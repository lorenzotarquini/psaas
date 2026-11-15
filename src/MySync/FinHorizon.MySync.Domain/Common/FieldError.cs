namespace FinHorizon.MySync.Domain.Common;

/// <summary>A broken rule on one field of a request, with one of the <see cref="ErrorCodes"/>.</summary>
public sealed record FieldError(string Field, string Code);
