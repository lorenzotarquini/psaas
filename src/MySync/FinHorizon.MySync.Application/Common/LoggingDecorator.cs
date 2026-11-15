using System.Diagnostics;
using NLog;

namespace FinHorizon.MySync.Application.Common;

/// <summary>
/// Logs every use case with its outcome and duration. Never the request: amounts and notes are personal data (RNF7).
/// </summary>
public sealed class LoggingDecorator<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> inner)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        var result = await inner.HandleAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.Status == ResultStatus.Invalid)
        {
            Logger.Info(
                "Use case {UseCase} rejected in {ElapsedMs} ms on fields {Fields}",
                typeof(TRequest).Name,
                watch.ElapsedMilliseconds,
                result.Errors.Select(error => $"{error.Field}:{error.Code}"));
        }
        else
        {
            Logger.Info("Use case {UseCase} ended {Status} in {ElapsedMs} ms", typeof(TRequest).Name, result.Status, watch.ElapsedMilliseconds);
        }

        return result;
    }
}
