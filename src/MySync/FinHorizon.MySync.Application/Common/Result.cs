using FinHorizon.MySync.Domain.Common;

namespace FinHorizon.MySync.Application.Common;

public enum ResultStatus
{
    Ok = 0,
    Created = 1,
    NotFound = 2,
    Invalid = 3
}

/// <summary>
/// The outcome of a use case. Expected failures (a wrong field, a record of another user) are values, not exceptions;
/// the API layer maps the status to HTTP.
/// </summary>
public sealed class Result<T>
{
    private Result(ResultStatus status, T? value, IReadOnlyList<FieldError> errors)
    {
        Status = status;
        Value = value;
        Errors = errors;
    }

    public ResultStatus Status { get; }

    public T? Value { get; }

    public IReadOnlyList<FieldError> Errors { get; }

    public bool Succeeded => Status is ResultStatus.Ok or ResultStatus.Created;

    public static Result<T> Ok(T value) => new(ResultStatus.Ok, value, []);

    public static Result<T> Created(T value) => new(ResultStatus.Created, value, []);

    public static Result<T> NotFound() => new(ResultStatus.NotFound, default, []);

    public static Result<T> Invalid(IReadOnlyList<FieldError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("An invalid result needs at least one error.", nameof(errors));
        }

        return new(ResultStatus.Invalid, default, errors);
    }

    public static Result<T> Invalid(string field, string code) => Invalid([new FieldError(field, code)]);
}
