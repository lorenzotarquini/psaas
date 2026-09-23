using PSAAS.Domain;

namespace PSAAS.Infrastructure.Data.DbModels;

public sealed class TenantDbModel
{
    public string Id { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string VatNumber { get; set; } = string.Empty;

    public string CompleteVatNumber { get; set; } = string.Empty;

    public string CertifiedEmail { get; set; } = string.Empty;

    public Address Address { get; set; } = new(string.Empty, string.Empty, string.Empty, string.Empty);
}
