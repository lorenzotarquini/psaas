using FinHorizon.Modules.Sdk;
using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Features.Memberships;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinHorizon.MySync.Backend.Infrastructure;

/// <summary>
/// Contract C5 (docs/architettura-moduli.md): the action belongs to a feature of a tier. Without a paid tier the
/// read-only rule (C4) still lets the user read; with a tier that lacks the feature the answer is 403
/// "feature-not-in-tier" with the feature, so the frontend can offer a plan that has it.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal sealed class RequiresFeatureAttribute(string feature) : Attribute, IAsyncActionFilter
{
    public string Feature { get; } = feature;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var services = context.HttpContext.RequestServices;
        var entitlement = await services.GetRequiredService<MembershipSync>()
            .EntitlementAsync(services.GetRequiredService<ICurrentUser>().Id, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);
        // C4 before C5: an expired user reads what was there (the write gate answers 402 on changes).
        if (entitlement.Status == EntitlementStatus.None || entitlement.Has(Feature))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var problem = new ProblemDetails { Status = StatusCodes.Status403Forbidden, Type = ModuleEntitlement.FeatureProblemType, Title = "Feature not in the paid tier" };
        problem.Extensions["feature"] = Feature;
        context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status403Forbidden };
    }
}
