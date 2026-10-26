using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Abstractions.Billing;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Billing;
using PSAAS.Application.Services.Billing;

namespace PSAAS.AdminPortal.Backend.Controllers;

// UC24 (EXP-08): invoices, payments, cancellations.
[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(IInvoiceRepository invoices, BillingService billing, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<Invoice>>> GetPage(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1) errors["page"] = ["InvalidValue"];
        if (pageSize is < 1 or > 200) errors["pageSize"] = ["InvalidValue"];
        if (status is not null && !InvoiceStatuses.All.Contains(status)) errors["status"] = ["InvalidValue"];
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        return await invoices.GetPageAsync(new InvoiceQuery(tenantId, status, search, page, pageSize), cancellationToken);
    }

    /// <summary>KPIs of the billing page; the month is the current UTC month.</summary>
    [HttpGet("summary")]
    public Task<BillingSummary> GetSummary(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return invoices.GetSummaryAsync(new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero), cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Invoice>> Get(Guid id, CancellationToken cancellationToken) =>
        await invoices.GetAsync(id, cancellationToken) is { } invoice ? invoice : NotFound();

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<Invoice>> RegisterPayment(Guid id, PaymentRequest request, CancellationToken cancellationToken) =>
        ToResponse(await billing.RegisterPaymentAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<Invoice>> Cancel(Guid id, CancelInvoiceRequest request, CancellationToken cancellationToken) =>
        ToResponse(await billing.CancelAsync(id, request, cancellationToken));

    private ActionResult<Invoice> ToResponse(InvoiceActionResult result) =>
        !result.Found ? NotFound()
        : result.Conflict ? Conflict()
        : result.Invoice is null ? ValidationProblem(new ValidationProblemDetails(result.Errors.ToDictionary(e => e.Key, e => new[] { e.Value })))
        : result.Invoice;
}

// UC23-R6: the daily billing, also on demand.
[ApiController]
[Route("api/billing")]
public sealed class BillingController(BillingService billing) : ControllerBase
{
    [HttpPost("run")]
    public Task<BillingRunResult> Run(CancellationToken cancellationToken) => billing.RunAsync(cancellationToken);
}
