using System.Diagnostics;
using NLog;

namespace FinHorizon.MySync.Application.Common;

/// <summary>
/// Logs every use case with its outcome and duration. Never the request: amounts and notes are personal data (RNF7).
/// The user is on the line already, as the pseudonymous id the request logging puts in scope.
/// </summary>
public sealed class LoggingDecorator<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> inner)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>The same as <c>LogEvents.UseCase</c> of FinHorizon.Observability: the dashboards select these lines by it.</summary>
    public const string UseCaseEvent = "use_case";

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger().WithProperty("Event", UseCaseEvent);

    public async Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        var result = await inner.HandleAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.Status == ResultStatus.Invalid)
        {
            // Fields as one string ("name:Required,amount:InvalidValue"): Loki reads strings, not arrays.
            Logger.Info(
                "Use case {UseCase} ended {Status} in {ElapsedMs} ms on fields {Fields}",
                typeof(TRequest).Name,
                result.Status,
                watch.ElapsedMilliseconds,
                string.Join(',', result.Errors.Select(error => $"{error.Field}:{error.Code}")));
        }
        else
        {
            Logger.Info("Use case {UseCase} ended {Status} in {ElapsedMs} ms", typeof(TRequest).Name, result.Status, watch.ElapsedMilliseconds);
        }

        return result;
    }
}
