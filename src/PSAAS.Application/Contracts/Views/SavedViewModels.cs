namespace PSAAS.Application.Contracts.Views;

/// <param name="Query">The list parameters as in the page address, e.g. "planId=…&amp;province=MI" (UC22-R2).</param>
public sealed record SavedView(Guid Id, string Area, string Name, string Query, DateTimeOffset CreatedAt);

public sealed record SavedViewRequest(string? Area, string? Name, string? Query);

public sealed record SavedViewResult(SavedView? View, IReadOnlyDictionary<string, string> Errors);
