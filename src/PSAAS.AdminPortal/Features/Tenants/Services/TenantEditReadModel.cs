namespace PSAAS.AdminPortal.Features.Tenants.Services;

public sealed record TenantEditReadModel(
    Guid Id,
    string CompanyName,
    string CompleteVatNumber,
    string CertifiedEmail,
    string Street,
    string City,
    string PostalCode,
    string Country);
