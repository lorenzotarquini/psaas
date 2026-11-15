using FinHorizon.MySync.Application.Abstractions.Currencies;
using Microsoft.AspNetCore.Diagnostics;

namespace FinHorizon.MySync.Backend.Infrastructure;

/// <summary>UC13-5a: without rates the request cannot be answered now, which is a 503 and not a server error.</summary>
internal sealed class CurrencyServiceExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public const string ProblemType = "currency-service-unavailable";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (exception is not CurrencyServiceUnavailableException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = StatusCodes.Status503ServiceUnavailable, Type = ProblemType, Title = "Exchange rates unavailable" },
        }).ConfigureAwait(false);
    }
}