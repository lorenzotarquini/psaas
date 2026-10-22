using Microsoft.AspNetCore.Mvc;
using NLog;
using PSAAS.Application.Abstractions.Catalog;
using PSAAS.Application.Contracts.Catalog;
using PSAAS.Application.Services.Catalog;

namespace PSAAS.AdminPortal.Backend.Controllers;

// UC15 (EXP-01): module catalog.
[ApiController]
[Route("api/modules")]
public sealed class ModulesController(ICatalogRepository catalog, CatalogService service) : ControllerBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [HttpGet]
    public async Task<IReadOnlyList<ModuleReadModel>> GetAll(CancellationToken cancellationToken)
    {
        var modules = await catalog.GetModulesAsync(cancellationToken);
        Logger.Info("Listed {ModuleCount} modules", modules.Count);
        return modules;
    }

    /// <summary>Registers a module from its manifest, or updates it (new version, status, hooks).</summary>
    [HttpPost]
    public async Task<ActionResult<ModuleReadModel>> Register(ModuleManifest manifest, CancellationToken cancellationToken)
    {
        var result = await service.RegisterModuleAsync(manifest, cancellationToken);
        if (!result.Succeeded) return Problem(result);
        return Ok(await catalog.GetModuleAsync(result.Id!, cancellationToken));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ModuleReadModel>> SetStatus(string id, ModuleStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SetModuleStatusAsync(id, request.Status, cancellationToken);
        if (result is null) return NotFound();
        if (!result.Succeeded) return Problem(result);
        return Ok(await catalog.GetModuleAsync(id, cancellationToken));
    }

    private ActionResult Problem(CatalogSaveResult result) =>
        ValidationProblem(new ValidationProblemDetails(result.Errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
}

public sealed record ModuleStatusRequest(string? Status);

// UC16 (EXP-02): plans.
[ApiController]
[Route("api/plans")]
public sealed class PlansController(ICatalogRepository catalog, CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PlanReadModel>> GetAll(CancellationToken cancellationToken) => catalog.GetPlansAsync(cancellationToken);

    [HttpPost]
    public Task<ActionResult<PlanReadModel>> Create(PlanRequest request, CancellationToken cancellationToken) => SaveAsync(null, request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<ActionResult<PlanReadModel>> Update(Guid id, PlanRequest request, CancellationToken cancellationToken) => SaveAsync(id, request, cancellationToken);

    private async Task<ActionResult<PlanReadModel>> SaveAsync(Guid? id, PlanRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SavePlanAsync(id, request, cancellationToken);
        if (result is null) return NotFound();
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
        }

        var plan = await catalog.GetPlanAsync(Guid.Parse(result.Id!), cancellationToken);
        return id is null ? StatusCode(StatusCodes.Status201Created, plan) : Ok(plan);
    }
}

// UC17 (EXP-03): every subscription, and the one of a tenant with its modules (UC18 retry).
[ApiController]
public sealed class SubscriptionsController(ISubscriptionRepository subscriptions, SubscriptionService service) : ControllerBase
{
    [HttpGet("api/subscriptions")]
    public Task<IReadOnlyList<SubscriptionReadModel>> GetAll(CancellationToken cancellationToken) => subscriptions.GetAllAsync(cancellationToken);

    [HttpGet("api/tenants/{tenantId:guid}/subscription")]
    public async Task<ActionResult<SubscriptionReadModel>> Get(Guid tenantId, CancellationToken cancellationToken) =>
        await subscriptions.GetAsync(tenantId, cancellationToken) is { } subscription ? subscription : NoContent();

    [HttpPut("api/tenants/{tenantId:guid}/subscription")]
    public async Task<ActionResult<SubscriptionReadModel>> Subscribe(Guid tenantId, SubscriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SubscribeAsync(tenantId, request, cancellationToken);
        if (!result.Found) return NotFound();
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
        }

        return (await subscriptions.GetAsync(tenantId, cancellationToken))!;
    }

    [HttpPost("api/tenants/{tenantId:guid}/modules/{moduleId}/retry")]
    public async Task<ActionResult<SubscriptionReadModel>> Retry(Guid tenantId, string moduleId, CancellationToken cancellationToken)
    {
        var result = await service.RetryAsync(tenantId, moduleId, cancellationToken);
        if (!result.Found) return NotFound();
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors.ToDictionary(e => e.Key, e => new[] { e.Value })));
        }

        return (await subscriptions.GetAsync(tenantId, cancellationToken))!;
    }
}
