using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.Application.Abstractions.Audit;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Audit;

namespace PSAAS.AdminPortal.Backend.Controllers;

// UC12 (EXP-10): the audit log of the platform and, with tenantId, the history of one tenant.
[ApiController]
[Route("api/audit")]
public sealed class AuditController(IAuditReadRepository audit) : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpGet]
    public async Task<PagedResult<AuditEventReadModel>> GetPage(
        [FromQuery] Guid? tenantId,
        [FromQuery] string? action,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 200)] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await audit.GetPageAsync(
            new AuditQuery(tenantId?.ToString(), action, search, from, to, page, pageSize),
            cancellationToken);
        Logger.Info(
            "Listed {EventCount} of {TotalCount} audit events (tenant {TenantId}, action {Action})",
            result.Items.Count,
            result.TotalCount,
            tenantId,
            action);
        return result;
    }
}
