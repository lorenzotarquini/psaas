namespace FinHorizon.MySync.Application.Common;

/// <summary>
/// One use case: a request in, a <see cref="Result{T}"/> out. Cross-cutting behaviour (validation, logging)
/// wraps every handler as a decorator, so handlers contain only the use case.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : notnull
{
    Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
