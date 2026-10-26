namespace PSAAS.Application.Contracts.Billing;

public static class InvoiceKinds
{
    public const string Invoice = "Invoice";
    public const string CreditNote = "CreditNote";
}

/// <summary>UC24-R1.</summary>
public static class InvoiceStatuses
{
    public const string Issued = "Issued";
    public const string Overdue = "Overdue";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Issued, Overdue, Paid, Cancelled };

    public static bool IsOpen(string status) => status is Issued or Overdue;
}

public sealed record InvoiceLine(string Description, decimal Quantity, decimal UnitPrice, decimal Amount);

/// <summary>An invoice or credit note (UC23-R3): the tenant data are those at the time of issue and never change.</summary>
/// <param name="Number">"2026/0001" (UC23-R1).</param>
/// <param name="RelatedInvoiceId">The credit note of a cancelled invoice, or the invoice a credit note cancels.</param>
public sealed record Invoice(
    Guid Id,
    string Number,
    string Kind,
    Guid TenantId,
    string TenantName,
    string TenantVatNumber,
    string TenantCertifiedEmail,
    string? TenantSdiCode,
    DateTimeOffset IssuedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? PeriodFrom,
    DateTimeOffset? PeriodTo,
    IReadOnlyList<InvoiceLine> Lines,
    decimal Net,
    decimal VatRate,
    decimal Vat,
    decimal Total,
    string Status,
    DateTimeOffset? PaidAt = null,
    string? PaymentReference = null,
    int RemindersSent = 0,
    DateTimeOffset? LastReminderAt = null,
    Guid? RelatedInvoiceId = null,
    string? RelatedNumber = null,
    string? CancelReason = null);

public sealed record InvoiceQuery(Guid? TenantId = null, string? Status = null, string? Search = null, int Page = 1, int PageSize = 50);

/// <summary>KPIs of the billing page (UC24 flow 1); amounts are totals with VAT.</summary>
public sealed record BillingSummary(decimal ToCollect, decimal Overdue, int OverdueCount, decimal PaidThisMonth, decimal IssuedThisMonth);

/// <summary>What one run of the daily billing did (UC23-R6).</summary>
public sealed record BillingRunResult(int Issued, int Expired, int Reminders, int Suspended);

/// <param name="PaidAt">"YYYY-MM-DD", not in the future (UC24-R2).</param>
public sealed record PaymentRequest(string? PaidAt, string? Reference);

public sealed record CancelInvoiceRequest(string? Reason);

/// <param name="Conflict">Not allowed in the current status (UC24 3a, 5a).</param>
public sealed record InvoiceActionResult(Invoice? Invoice, bool Found, bool Conflict, IReadOnlyDictionary<string, string> Errors)
{
    public static readonly InvoiceActionResult NotFound = new(null, false, false, new Dictionary<string, string>());

    public static readonly InvoiceActionResult NotAllowed = new(null, true, true, new Dictionary<string, string>());
}
