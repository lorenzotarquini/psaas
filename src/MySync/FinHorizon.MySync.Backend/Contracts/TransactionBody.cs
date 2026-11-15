namespace FinHorizon.MySync.Backend.Contracts;

/// <summary>The body of PUT /api/transactions/{id}: the id travels in the route.</summary>
public sealed record TransactionBody(
    string? Kind,
    DateOnly? Date,
    decimal? Amount,
    Guid? AccountId,
    Guid? CategoryId,
    Guid? DebtId,
    string? Note);
