using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Billing;

namespace PSAAS.Application.Abstractions.Billing;

public interface IInvoiceRepository
{
    /// <summary>Saves a new invoice with the next number of the year of issue (UC23-R1) and returns it numbered.</summary>
    Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default);

    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);

    Task<Invoice?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Most recent first.</summary>
    Task<PagedResult<Invoice>> GetPageAsync(InvoiceQuery query, CancellationToken cancellationToken = default);

    /// <summary>Issued and overdue invoices, for the daily run.</summary>
    Task<IReadOnlyList<Invoice>> GetOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>UC23-R6: an invoice of the tenant for the period starting then already exists.</summary>
    Task<bool> ExistsForPeriodAsync(Guid tenantId, DateTimeOffset periodFrom, CancellationToken cancellationToken = default);

    Task<BillingSummary> GetSummaryAsync(DateTimeOffset monthStart, CancellationToken cancellationToken = default);
}

/// <summary>
/// Where issued invoices go (UC23 step 5). Today a no-op: invoices stay in PSAAS; the place to plug SDI or a payment
/// provider in later.
/// </summary>
public interface IInvoiceTransmitter
{
    Task TransmitAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
