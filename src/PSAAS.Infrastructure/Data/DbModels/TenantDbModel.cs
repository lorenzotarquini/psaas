using PSAAS.Domain;

namespace PSAAS.Infrastructure.Data.DbModels;

public sealed class TenantDbModel
{
    public string Id { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string NormalizedCompanyName { get; set; } = string.Empty;

    public string VatNumber { get; set; } = string.Empty;

    public string CompleteVatNumber { get; set; } = string.Empty;

    public string CertifiedEmail { get; set; } = string.Empty;

    public string NormalizedCertifiedEmail { get; set; } = string.Empty;

    public Address Address { get; set; } = new(string.Empty, string.Empty, string.Empty, string.Empty);

    /// <summary>TenantStatus as text, so the column reads well in SQL and survives enum reordering.</summary>
    public string Status { get; set; } = nameof(TenantStatus.Active);

    public string? StatusReason { get; set; }

    public DateTimeOffset StatusChangedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? SdiCode { get; set; }

    public string? AccountManager { get; set; }
}
