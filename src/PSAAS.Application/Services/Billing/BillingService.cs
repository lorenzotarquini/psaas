using System.Globalization;
using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Abstractions.Billing;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Abstractions.Communications;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Audit;
using PSAAS.Application.Contracts.Billing;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Tenants;
using PSAAS.Domain;

namespace PSAAS.Application.Services.Billing;

/// <param name="SuspendAfterDays">UC24-R4: days past the due date before a tenant is suspended (0 = never).</param>
public sealed record BillingOptions(int SuspendAfterDays = 30);

/// <summary>
/// EXP-08, internal billing without SDI: invoices of subscription periods (UC23), payments, reminders and suspension
/// for unpaid invoices (UC24).
/// </summary>
public sealed class BillingService(
    IInvoiceRepository invoices,
    IInvoiceTransmitter transmitter,
    ISubscriptionRepository subscriptions,
    ITenantReadRepository tenants,
    TenantLifecycleService lifecycle,
    IEmailSender email,
    IAuditLog audit,
    BillingOptions options,
    TimeProvider clock)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const decimal VatRate = 0.22m;
    public const int PaymentDays = 30;
    public const int ReminderEveryDays = 7;

    public static TimeSpan PeriodOf(string billingPeriod) =>
        billingPeriod == BillingPeriods.Yearly ? TimeSpan.FromDays(365) : TimeSpan.FromDays(30);

    /// <summary>UC23 step 1: the first paid period of a subscription that was not paid before.</summary>
    public async Task InvoicePeriodAsync(SubscriptionReadModel subscription, DateTimeOffset from, CancellationToken cancellationToken = default)
    {
        // A free plan has nothing to invoice.
        if (subscription.Price == 0 || await invoices.ExistsForPeriodAsync(subscription.TenantId, from, cancellationToken)) return;
        var to = from.Add(PeriodOf(subscription.BillingPeriod));
        await IssueAsync(
            subscription.TenantId, InvoiceKinds.Invoice, from, to,
            [Line($"Piano {subscription.PlanName} · {Period(from, to)}", subscription.Price)],
            null, cancellationToken);
    }

    /// <summary>UC23 step 4 and R4: the price difference for the days left in the current period.</summary>
    public async Task InvoicePlanChangeAsync(SubscriptionReadModel before, SubscriptionReadModel after, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var daysLeft = Math.Max(0, (int)Math.Ceiling((after.RenewsAt - now).TotalDays));
        var difference = Daily(after) - Daily(before);
        var amount = Math.Round(difference * daysLeft, 2, MidpointRounding.AwayFromZero);
        if (amount == 0) return;
        var description = $"Adeguamento piano {before.PlanName} → {after.PlanName} · {daysLeft} giorni";
        await IssueAsync(
            after.TenantId, amount > 0 ? InvoiceKinds.Invoice : InvoiceKinds.CreditNote, now, after.RenewsAt,
            [Line(description, Math.Abs(amount))], null, cancellationToken);

        static decimal Daily(SubscriptionReadModel s) => s.Price / (decimal)PeriodOf(s.BillingPeriod).TotalDays;
    }

    /// <summary>The daily run (UC23 steps 2–3, UC24 step 4); safe to repeat (UC23-R6).</summary>
    public async Task<BillingRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        int issued = 0, expired = 0, reminders = 0, suspended = 0;
        foreach (var subscription in await subscriptions.GetAllAsync(cancellationToken))
        {
            var (i, e) = await RenewAsync(subscription, now, cancellationToken);
            issued += i;
            expired += e;
        }

        foreach (var invoice in await invoices.GetOpenAsync(cancellationToken))
        {
            if (invoice.DueAt >= now) continue;
            var overdue = invoice with { Status = InvoiceStatuses.Overdue };
            if (overdue.LastReminderAt is null || overdue.LastReminderAt <= now.AddDays(-ReminderEveryDays))
            {
                overdue = overdue with { RemindersSent = overdue.RemindersSent + 1, LastReminderAt = now };
                reminders++;
                await RecordAsync(AuditActions.InvoiceReminderSent, overdue, new { number = overdue.Number, reminder = overdue.RemindersSent, total = overdue.Total }, cancellationToken);
                await EmailReminderAsync(overdue, cancellationToken);
            }

            if (overdue != invoice) await invoices.UpdateAsync(overdue, cancellationToken);
            if (options.SuspendAfterDays > 0 && invoice.DueAt <= now.AddDays(-options.SuspendAfterDays) && await SuspendAsync(overdue, cancellationToken))
            {
                suspended++;
            }
        }

        Logger.Info("Billing run: {Issued} issued, {Expired} expired, {Reminders} reminders, {Suspended} suspended", issued, expired, reminders, suspended);
        return new BillingRunResult(issued, expired, reminders, suspended);
    }

    /// <exception cref="ArgumentNullException">request is null.</exception>
    public async Task<InvoiceActionResult> RegisterPaymentAsync(Guid id, PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invoice = await invoices.GetAsync(id, cancellationToken);
        if (invoice is null) return InvoiceActionResult.NotFound;
        var errors = new Dictionary<string, string>();
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (string.IsNullOrWhiteSpace(request.PaidAt)) errors["paidAt"] = TenantFieldValidationErrorCodes.Required;
        else if (!DateOnly.TryParseExact(request.PaidAt.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var paid) || paid > today)
            errors["paidAt"] = TenantFieldValidationErrorCodes.InvalidValue;
        var reference = request.Reference?.Trim();
        if (reference?.Length > 100) errors["reference"] = TenantFieldValidationErrorCodes.MaxLengthExceeded;
        if (errors.Count > 0) return new InvoiceActionResult(null, true, false, errors);
        if (!InvoiceStatuses.IsOpen(invoice.Status) || invoice.Kind != InvoiceKinds.Invoice) return InvoiceActionResult.NotAllowed;

        var paidAt = new DateTimeOffset(DateOnly.ParseExact(request.PaidAt!.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var updated = invoice with { Status = InvoiceStatuses.Paid, PaidAt = paidAt, PaymentReference = string.IsNullOrEmpty(reference) ? null : reference };
        await invoices.UpdateAsync(updated, cancellationToken);
        await RecordAsync(AuditActions.InvoicePaid, updated, new { number = updated.Number, total = updated.Total, reference = updated.PaymentReference }, cancellationToken);
        Logger.Info("Invoice {Number} paid", updated.Number);
        return new InvoiceActionResult(updated, true, false, errors);
    }

    /// <summary>UC24 step 5: cancels an unpaid invoice with a credit note of the same amount.</summary>
    public async Task<InvoiceActionResult> CancelAsync(Guid id, CancelInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invoice = await invoices.GetAsync(id, cancellationToken);
        if (invoice is null) return InvoiceActionResult.NotFound;
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length == 0) return new InvoiceActionResult(null, true, false, new Dictionary<string, string> { ["reason"] = TenantFieldValidationErrorCodes.Required });
        if (!InvoiceStatuses.IsOpen(invoice.Status) || invoice.Kind != InvoiceKinds.Invoice) return InvoiceActionResult.NotAllowed;

        var credit = await IssueAsync(
            invoice.TenantId, InvoiceKinds.CreditNote, invoice.PeriodFrom, invoice.PeriodTo,
            invoice.Lines, invoice, cancellationToken, $"Storno fattura {invoice.Number}: {reason}");
        var cancelled = invoice with { Status = InvoiceStatuses.Cancelled, CancelReason = reason, RelatedInvoiceId = credit.Id, RelatedNumber = credit.Number };
        await invoices.UpdateAsync(cancelled, cancellationToken);
        await RecordAsync(AuditActions.InvoiceCancelled, cancelled, new { number = cancelled.Number, reason, creditNote = credit.Number }, cancellationToken);
        return new InvoiceActionResult(cancelled, true, false, new Dictionary<string, string>());
    }

    // UC23 steps 2-3: catches up every period due, so a missed day is billed on the next run.
    private async Task<(int Issued, int Expired)> RenewAsync(SubscriptionReadModel subscription, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var moduleIds = subscription.Modules.Select(m => m.ModuleId).ToArray();
        if (subscription.Status == SubscriptionStatuses.Trial && subscription.TrialEndsAt is { } trialEnd && trialEnd <= now)
        {
            if (!subscription.AutoRenew)
            {
                await ExpireAsync(subscription, moduleIds, cancellationToken);
                return (0, 1);
            }

            var converted = subscription with { Status = SubscriptionStatuses.Active, TrialEndsAt = null, RenewsAt = trialEnd };
            await subscriptions.SaveAsync(converted, moduleIds, cancellationToken);
            var tenant = await tenants.GetByIdAsync(subscription.TenantId, cancellationToken);
            if (tenant?.Lifecycle.Status == TenantStatus.Trial)
            {
                await lifecycle.ChangeStatusAsync(subscription.TenantId, new TenantStatusChangeRequest(nameof(TenantStatus.Active), null), cancellationToken);
            }

            subscription = converted;
        }

        if (subscription.Status != SubscriptionStatuses.Active || subscription.RenewsAt > now) return (0, 0);
        if (!subscription.AutoRenew)
        {
            await ExpireAsync(subscription, moduleIds, cancellationToken);
            return (0, 1);
        }

        var issued = 0;
        var renewsAt = subscription.RenewsAt;
        while (renewsAt <= now)
        {
            await InvoicePeriodAsync(subscription, renewsAt, cancellationToken);
            renewsAt = renewsAt.Add(PeriodOf(subscription.BillingPeriod));
            issued++;
        }

        await subscriptions.SaveAsync(subscription with { RenewsAt = renewsAt }, moduleIds, cancellationToken);
        return (issued, 0);
    }

    private async Task ExpireAsync(SubscriptionReadModel subscription, string[] moduleIds, CancellationToken cancellationToken)
    {
        await subscriptions.SaveAsync(subscription with { Status = SubscriptionStatuses.Expired }, moduleIds, cancellationToken);
        await audit.RecordAsync(
            new AuditRecord(
                AuditActions.SubscriptionExpired, AuditTargets.Tenant, subscription.TenantId.ToString(), subscription.TenantName,
                subscription.TenantId.ToString(), new { plan = subscription.PlanName }),
            cancellationToken);
    }

    // UC24-R4: only tenants that are running; the reactivation stays with the administrator.
    private async Task<bool> SuspendAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var tenant = await tenants.GetByIdAsync(invoice.TenantId, cancellationToken);
        if (tenant?.Lifecycle.Status is not (TenantStatus.Active or TenantStatus.Trial)) return false;
        var result = await lifecycle.ChangeStatusAsync(
            invoice.TenantId, new TenantStatusChangeRequest(nameof(TenantStatus.Suspended), $"Morosità: fattura {invoice.Number}"), cancellationToken);
        Logger.Warn("Tenant {TenantId} suspended for the unpaid invoice {Number}", invoice.TenantId, invoice.Number);
        return result.Succeeded;
    }

    private async Task<Invoice> IssueAsync(
        Guid tenantId,
        string kind,
        DateTimeOffset? from,
        DateTimeOffset? to,
        IReadOnlyList<InvoiceLine> lines,
        Invoice? related,
        CancellationToken cancellationToken,
        string? note = null)
    {
        var tenant = await tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant {tenantId} to invoice does not exist");
        var now = clock.GetUtcNow();
        var net = lines.Sum(l => l.Amount);
        var vat = Math.Round(net * VatRate, 2, MidpointRounding.AwayFromZero);
        var invoice = await invoices.AddAsync(
            new Invoice(
                Guid.NewGuid(), string.Empty, kind, tenantId, tenant.CompanyName, tenant.CompleteVatNumber, tenant.CertifiedEmail,
                tenant.Commercial.SdiCode, now, now.AddDays(PaymentDays), from, to, lines, net, VatRate, vat, net + vat,
                // A credit note settles itself (UC24-R1).
                kind == InvoiceKinds.CreditNote ? InvoiceStatuses.Paid : InvoiceStatuses.Issued,
                PaidAt: kind == InvoiceKinds.CreditNote ? now : null,
                RelatedInvoiceId: related?.Id,
                RelatedNumber: related?.Number,
                CancelReason: note),
            cancellationToken);
        await transmitter.TransmitAsync(invoice, cancellationToken);
        await RecordAsync(AuditActions.InvoiceIssued, invoice, new { number = invoice.Number, kind, total = invoice.Total }, cancellationToken);
        Logger.Info("Issued {Kind} {Number} of {Total} to tenant {TenantId}", kind, invoice.Number, invoice.Total, tenantId);
        return invoice;
    }

    // UC27-R4 / UC24-R3: the reminder goes to the PEC of the tenant; a refused email is logged and retried with the next reminder.
    private async Task EmailReminderAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        if (!email.IsConfigured) return;
        var body =
            $"Gentile {invoice.TenantName},\n\nrisulta non ancora saldata la fattura {invoice.Number} del {invoice.IssuedAt.ToString("dd'/'MM'/'yyyy", CultureInfo.InvariantCulture)}, " +
            $"scaduta il {invoice.DueAt.ToString("dd'/'MM'/'yyyy", CultureInfo.InvariantCulture)}, di {invoice.Total.ToString("N2", CultureInfo.GetCultureInfo("it-IT"))} €.\n" +
            "Se ha già provveduto al pagamento, non tenga conto di questo messaggio.";
        try
        {
            await email.SendAsync(new EmailMessage(invoice.TenantCertifiedEmail.ToLowerInvariant(), $"Sollecito di pagamento fattura {invoice.Number}", body), cancellationToken);
        }
        catch (EmailDeliveryException exception)
        {
            Logger.Warn(exception, "Reminder email for invoice {Number} refused", invoice.Number);
        }
    }

    private Task RecordAsync(string action, Invoice invoice, object details, CancellationToken cancellationToken) =>
        audit.RecordAsync(
            new AuditRecord(action, AuditTargets.Invoice, invoice.Id.ToString(), invoice.Number, invoice.TenantId.ToString(), details),
            cancellationToken);

    private static InvoiceLine Line(string description, decimal amount) => new(description, 1, amount, amount);

    private static string Period(DateTimeOffset from, DateTimeOffset to) =>
        $"{from.ToString("dd'/'MM'/'yyyy", CultureInfo.InvariantCulture)} – {to.AddDays(-1).ToString("dd'/'MM'/'yyyy", CultureInfo.InvariantCulture)}";
}
