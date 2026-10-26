using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NLog;
using PSAAS.Application.Abstractions.Billing;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Billing;
using PSAAS.Infrastructure.Data.DbContexts;

namespace PSAAS.Infrastructure.Billing;

public sealed class InvoiceDbModel
{
    public Guid Id { get; set; }

    /// <summary>UC23-R1: Year + Sequence are unique; the number is "Year/Sequence".</summary>
    public int Year { get; set; }

    public int Sequence { get; set; }

    public string Kind { get; set; } = InvoiceKinds.Invoice;
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantVatNumber { get; set; } = string.Empty;
    public string TenantCertifiedEmail { get; set; } = string.Empty;
    public string? TenantSdiCode { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public DateTimeOffset? PeriodFrom { get; set; }
    public DateTimeOffset? PeriodTo { get; set; }

    /// <summary>The lines as JSON: they never change after the invoice is issued.</summary>
    public string Lines { get; set; } = "[]";

    public decimal Net { get; set; }
    public decimal VatRate { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = InvoiceStatuses.Issued;
    public DateTimeOffset? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
    public int RemindersSent { get; set; }
    public DateTimeOffset? LastReminderAt { get; set; }
    public Guid? RelatedInvoiceId { get; set; }
    public string? CancelReason { get; set; }
}

public sealed class InvoiceRepository(PsaasDbContext dbContext) : IInvoiceRepository
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var year = invoice.IssuedAt.Year;
        // shortcut: next number read then written; the unique index rejects a concurrent duplicate, add a sequence if issuing gets concurrent.
        var sequence = await dbContext.Invoices.Where(i => i.Year == year).MaxAsync(i => (int?)i.Sequence, cancellationToken).ConfigureAwait(false) ?? 0;
        var model = ToDbModel(invoice);
        model.Year = year;
        model.Sequence = sequence + 1;
        dbContext.Invoices.Add(model);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(model.Id, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Invoice not saved");
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var model = await dbContext.Invoices.SingleAsync(i => i.Id == invoice.Id, cancellationToken).ConfigureAwait(false);
        // Only what can change after issue (UC23-R3).
        model.Status = invoice.Status;
        model.PaidAt = invoice.PaidAt;
        model.PaymentReference = invoice.PaymentReference;
        model.RemindersSent = invoice.RemindersSent;
        model.LastReminderAt = invoice.LastReminderAt;
        model.RelatedInvoiceId = invoice.RelatedInvoiceId;
        model.CancelReason = invoice.CancelReason;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Invoice?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Invoices.AsNoTracking().SingleOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);
        return model is null ? null : (await WithRelatedAsync([model], cancellationToken).ConfigureAwait(false))[0];
    }

    public async Task<PagedResult<Invoice>> GetPageAsync(InvoiceQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var invoices = dbContext.Invoices.AsNoTracking();
        if (query.TenantId is { } tenantId)
        {
            var id = tenantId.ToString();
            invoices = invoices.Where(i => i.TenantId == id);
        }

        if (!string.IsNullOrWhiteSpace(query.Status)) invoices = invoices.Where(i => i.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            invoices = invoices.Where(i => i.TenantName.ToLower().Contains(search)
                || (i.Year.ToString() + "/" + i.Sequence.ToString()).Contains(search.TrimStart('0')));
        }

        var total = await invoices.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = await invoices
            .OrderByDescending(i => i.IssuedAt).ThenByDescending(i => i.Sequence)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedResult<Invoice>(await WithRelatedAsync(page, cancellationToken).ConfigureAwait(false), total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<Invoice>> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        var open = await dbContext.Invoices.AsNoTracking()
            .Where(i => i.Kind == InvoiceKinds.Invoice && (i.Status == InvoiceStatuses.Issued || i.Status == InvoiceStatuses.Overdue))
            .OrderBy(i => i.DueAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return await WithRelatedAsync(open, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ExistsForPeriodAsync(Guid tenantId, DateTimeOffset periodFrom, CancellationToken cancellationToken = default)
    {
        var id = tenantId.ToString();
        return dbContext.Invoices.AnyAsync(
            i => i.TenantId == id && i.Kind == InvoiceKinds.Invoice && i.PeriodFrom == periodFrom && i.Status != InvoiceStatuses.Cancelled,
            cancellationToken);
    }

    public async Task<BillingSummary> GetSummaryAsync(DateTimeOffset monthStart, CancellationToken cancellationToken = default)
    {
        var invoices = dbContext.Invoices.AsNoTracking().Where(i => i.Kind == InvoiceKinds.Invoice);
        var toCollect = await invoices.Where(i => i.Status == InvoiceStatuses.Issued || i.Status == InvoiceStatuses.Overdue)
            .SumAsync(i => (decimal?)i.Total, cancellationToken).ConfigureAwait(false) ?? 0;
        var overdue = invoices.Where(i => i.Status == InvoiceStatuses.Overdue);
        var overdueTotal = await overdue.SumAsync(i => (decimal?)i.Total, cancellationToken).ConfigureAwait(false) ?? 0;
        var overdueCount = await overdue.CountAsync(cancellationToken).ConfigureAwait(false);
        var paid = await invoices.Where(i => i.Status == InvoiceStatuses.Paid && i.PaidAt >= monthStart)
            .SumAsync(i => (decimal?)i.Total, cancellationToken).ConfigureAwait(false) ?? 0;
        // Issued this month: invoices minus credit notes, cancelled ones included (their credit note offsets them).
        var issuedThisMonth = await dbContext.Invoices.AsNoTracking().Where(i => i.IssuedAt >= monthStart)
            .SumAsync(i => (decimal?)(i.Kind == InvoiceKinds.CreditNote ? -i.Total : i.Total), cancellationToken).ConfigureAwait(false) ?? 0;
        return new BillingSummary(toCollect, overdueTotal, overdueCount, paid, issuedThisMonth);
    }

    private async Task<Invoice[]> WithRelatedAsync(IReadOnlyList<InvoiceDbModel> models, CancellationToken cancellationToken)
    {
        var relatedIds = models.Where(m => m.RelatedInvoiceId is not null).Select(m => m.RelatedInvoiceId!.Value).ToArray();
        var numbers = await dbContext.Invoices.AsNoTracking()
            .Where(i => relatedIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => Number(i.Year, i.Sequence), cancellationToken).ConfigureAwait(false);
        Logger.Debug("Read {InvoiceCount} invoices", models.Count);
        return models.Select(m => ToInvoice(m, m.RelatedInvoiceId is { } r ? numbers.GetValueOrDefault(r) : null)).ToArray();
    }

    private static string Number(int year, int sequence) => $"{year}/{sequence.ToString("0000", CultureInfo.InvariantCulture)}";

    private static Invoice ToInvoice(InvoiceDbModel m, string? relatedNumber) => new(
        m.Id, Number(m.Year, m.Sequence), m.Kind, Guid.Parse(m.TenantId), m.TenantName, m.TenantVatNumber, m.TenantCertifiedEmail, m.TenantSdiCode,
        m.IssuedAt, m.DueAt, m.PeriodFrom, m.PeriodTo, JsonSerializer.Deserialize<InvoiceLine[]>(m.Lines) ?? [], m.Net, m.VatRate, m.Vat, m.Total,
        m.Status, m.PaidAt, m.PaymentReference, m.RemindersSent, m.LastReminderAt, m.RelatedInvoiceId, relatedNumber, m.CancelReason);

    private static InvoiceDbModel ToDbModel(Invoice i) => new()
    {
        Id = i.Id,
        Kind = i.Kind,
        TenantId = i.TenantId.ToString(),
        TenantName = i.TenantName,
        TenantVatNumber = i.TenantVatNumber,
        TenantCertifiedEmail = i.TenantCertifiedEmail,
        TenantSdiCode = i.TenantSdiCode,
        IssuedAt = i.IssuedAt,
        DueAt = i.DueAt,
        PeriodFrom = i.PeriodFrom,
        PeriodTo = i.PeriodTo,
        Lines = JsonSerializer.Serialize(i.Lines),
        Net = i.Net,
        VatRate = i.VatRate,
        Vat = i.Vat,
        Total = i.Total,
        Status = i.Status,
        PaidAt = i.PaidAt,
        PaymentReference = i.PaymentReference,
        RemindersSent = i.RemindersSent,
        LastReminderAt = i.LastReminderAt,
        RelatedInvoiceId = i.RelatedInvoiceId,
        CancelReason = i.CancelReason,
    };
}

/// <summary>EXP-08 decision: no SDI yet, invoices stay in PSAAS. Replace with an SDI or payment provider adapter.</summary>
public sealed class NoOpInvoiceTransmitter : IInvoiceTransmitter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public Task TransmitAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        Logger.Info("Invoice {Number} kept internally: no transmission channel configured", invoice.Number);
        return Task.CompletedTask;
    }
}
