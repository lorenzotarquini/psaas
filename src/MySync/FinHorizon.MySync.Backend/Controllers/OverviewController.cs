using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Dashboard;
using FinHorizon.MySync.Application.Features.NetWorth;
using FinHorizon.MySync.Application.Features.Portfolio;
using FinHorizon.MySync.Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace FinHorizon.MySync.Backend.Controllers;

/// <summary>UC4 and UC7: the read-only views over the whole portfolio.</summary>
[ApiController]
[Route("api")]
public sealed class OverviewController : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardView>> Dashboard(
        [FromServices] IRequestHandler<GetDashboardQuery, DashboardView> handler,
        CancellationToken cancellationToken,
        [FromQuery] bool includeStable = true,
        [FromQuery] bool includeInvestments = true) =>
        (await handler.HandleAsync(new GetDashboardQuery(includeStable, includeInvestments), cancellationToken)).ToActionResult(this);

    [HttpGet("net-worth")]
    public async Task<ActionResult<NetWorthTrendView>> NetWorth(
        [FromQuery] string? period,
        [FromServices] IRequestHandler<GetNetWorthTrendQuery, NetWorthTrendView> handler,
        CancellationToken cancellationToken,
        [FromQuery] bool includeStable = true,
        [FromQuery] bool includeInvestments = true) =>
        (await handler.HandleAsync(new GetNetWorthTrendQuery(period, includeStable, includeInvestments), cancellationToken)).ToActionResult(this);

    /// <summary>Tags already used, for the suggestions of UC2 and UC3.</summary>
    [HttpGet("tags")]
    public async Task<ActionResult<IReadOnlyList<string>>> Tags(
        [FromServices] IRequestHandler<ListTagsQuery, IReadOnlyList<string>> handler,
        CancellationToken cancellationToken) =>
        (await handler.HandleAsync(new ListTagsQuery(), cancellationToken)).ToActionResult(this);
}
