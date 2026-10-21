namespace PSAAS.AdminPortal.Backend.Contracts;

/// <summary>Outcome of checking one unique tenant field: null when valid, otherwise the code saving would return.</summary>
public sealed record TenantFieldValidationDto(string? ErrorCode);
