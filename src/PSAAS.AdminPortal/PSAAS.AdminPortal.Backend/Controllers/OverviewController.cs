using Microsoft.AspNetCore.Mvc;
using PSAAS.Application.Contracts.Overview;
using PSAAS.Application.Services.Overview;
using PSAAS.Application.Services.Users;

namespace PSAAS.AdminPortal.Backend.Controllers;

// UC29 (mock 1b) and UC30 (⌘K).
[ApiController]
public sealed class OverviewController : ControllerBase
{
    [HttpGet("api/overview")]
    public Task<OverviewKpis> GetKpis([FromServices] OverviewService overview, CancellationToken cancellationToken) =>
        overview.GetKpisAsync(cancellationToken);

    [HttpGet("api/search")]
    public async Task<ActionResult<SearchResult>> Search([FromQuery] string? q, [FromServices] GlobalSearchService search, CancellationToken cancellationToken) =>
        await search.SearchAsync(q, cancellationToken) is { } result
            ? result
            : ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["q"] = ["InvalidValue"] }));

    /// <summary>UC29 "Reinvia inviti": every invitation open for more than 5 days.</summary>
    [HttpPost("api/users/invites/resend")]
    public async Task<object> ResendInvites([FromServices] UserManagementService users, CancellationToken cancellationToken) =>
        new { resent = await users.ResendStaleInvitesAsync(cancellationToken) };
}

// UC31 (EXP-12).
[ApiController]
public sealed class ImpersonationController(ImpersonationService impersonation) : ControllerBase
{
    /// <summary>Whether the "Impersona" button can work (UC31 1a).</summary>
    [HttpGet("api/impersonation")]
    public object Status() => new { configured = impersonation.IsConfigured };

    [HttpPost("api/users/{id}/impersonate")]
    public async Task<ActionResult<ImpersonationTicket>> Start(string id, ImpersonationRequest request, CancellationToken cancellationToken)
    {
        var result = await impersonation.StartAsync(id, request, cancellationToken);
        if (result.Errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
        }

        return !result.Found ? NotFound() : result.Conflict ? Conflict() : result.Ticket!;
    }
}
