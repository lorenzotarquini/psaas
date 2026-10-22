namespace PSAAS.Application.Contracts.Tenants;

/// <summary>Enough to name a tenant and link to it (e.g. the one already using a value, UC11).</summary>
public sealed record TenantReference(Guid Id, string CompanyName);
