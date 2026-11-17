using FinHorizon.MySync.Application.Abstractions;
using FinHorizon.MySync.Application.Features.Memberships;

namespace FinHorizon.MySync.Backend.Infrastructure;

/// <summary>
/// UC17-R4: with the subscription expired MySync is read-only. Every change under /api answers 402, except the
/// subscription itself (the user must be able to pay); reads are untouched.
/// </summary>
internal sealed class MembershipWriteGate(RequestDelegate next)
{
    public const string ProblemType = "membership-expired";

    public async Task InvokeAsync(HttpContext context, ICurrentUser user, MembershipSync memberships, IProblemDetailsService problemDetails)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method)
            || !request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || request.Path.StartsWithSegments("/api/membership", StringComparison.OrdinalIgnoreCase)
            || context.User.Identity?.IsAuthenticated != true)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (await memberships.CanWriteAsync(user.Id, context.RequestAborted).ConfigureAwait(false))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = { Status = StatusCodes.Status402PaymentRequired, Type = ProblemType, Title = "MySync subscription expired" },
        }).ConfigureAwait(false);
    }
}

/// <summary>UC17: Stripe unreachable or not configured is a 503, not a server error.</summary>
internal sealed class PaymentProviderExceptionHandler(IProblemDetailsService problemDetails) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public const string ProblemType = "payment-provider-unavailable";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (exception is not Application.Abstractions.Payments.PaymentProviderUnavailableException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = StatusCodes.Status503ServiceUnavailable, Type = ProblemType, Title = "Payments unavailable" },
        }).ConfigureAwait(false);
    }
}
