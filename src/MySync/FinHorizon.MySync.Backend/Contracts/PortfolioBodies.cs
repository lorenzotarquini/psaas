namespace FinHorizon.MySync.Backend.Contracts;

/// <summary>The body of PUT /api/accounts/{id}: the id travels in the route.</summary>
public sealed record AccountBody(string? Kind, string? Name, string? Description, string? Tag, decimal? InitialBalance, string? Currency = null);

/// <summary>The body of PUT /api/debts/{id}: <see cref="Residual"/> is the initial residual.</summary>
public sealed record DebtBody(string? Kind, string? Name, string? Description, string? Tag, Guid? AccountId, decimal? Residual, string? Currency = null, decimal? MonthlyPayment = null);

/// <summary>The body of PUT /api/categories/{id}/fixed (UC15).</summary>
public sealed record CategoryFixedBody(bool Fixed);

/// <summary>The body of PUT /api/subscriptions/{id} (UC16 flow M).</summary>
public sealed record SubscriptionBody(string? Name, string? Description, Guid? AccountId, Guid? CategoryId, decimal? Amount, DateOnly? FirstChargeOn);

/// <summary>The body of POST /api/subscriptions/{id}/cancel (UC16-R13).</summary>
public sealed record CancelSubscriptionBody(DateOnly? EndsOn);

/// <summary>The body of POST /api/subscriptions/{id}/skip (UC16-R9).</summary>
public sealed record SkipChargeBody(DateOnly? DueOn);
