using System.Text.Json;
using FinHorizon.MySync.Domain.Common;
using FluentValidation;

namespace FinHorizon.MySync.Application.Common;

/// <summary>
/// Runs the input validators of the request before the use case: format, required fields, lengths.
/// Rules that need stored data (uniqueness, limits, ownership) stay in the handlers.
/// </summary>
public sealed class ValidationDecorator<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> inner,
    IEnumerable<IValidator<TRequest>> validators) : IRequestHandler<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<FieldError>();
        foreach (var validator in validators)
        {
            var validation = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            errors.AddRange(validation.Errors.Select(failure =>
                new FieldError(FieldName(failure.PropertyName), failure.ErrorCode)));
        }

        return errors.Count == 0
            ? await inner.HandleAsync(request, cancellationToken).ConfigureAwait(false)
            : Result<TResponse>.Invalid(errors.DistinctBy(error => error.Field).ToList());
    }

    /// <summary>"Events[0].Month" becomes "events[0].month": every segment of a nested field in camel case.</summary>
    private static string FieldName(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
