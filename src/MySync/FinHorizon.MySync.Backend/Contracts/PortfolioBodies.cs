namespace FinHorizon.MySync.Backend.Contracts;

/// <summary>The body of PUT /api/accounts/{id}: the id travels in the route.</summary>
public sealed record AccountBody(string? Kind, string? Name, string? Description, string? Tag, decimal? InitialBalance, string? Currency = null);

/// <summary>The body of PUT /api/debts/{id}: <see cref="Residual"/> is the initial residual.</summary>
public sealed record DebtBody(string? Kind, string? Name, string? Description, string? Tag, Guid? AccountId, decimal? Residual, string? Currency = null, decimal? MonthlyPayment = null);

/// <summary>The body of PUT /api/categories/{id}/fixed (UC15).</summary>
public sealed record CategoryFixedBody(bool Fixed);
