using PSAAS.Application.Validators.Tenants;
using PSAAS.Domain;
using PSAAS.Infrastructure.Data.DbModels;

namespace PSAAS.Infrastructure.Converters;

public static class TenantDbModelConverter
{
    public static Tenant ToDomain(TenantDbModel tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var id = Guid.Parse(tenant.Id);

        return Tenant.Rehydrate(
            id,
            tenant.CompanyName,
            tenant.VatNumber,
            tenant.CompleteVatNumber,
            tenant.CertifiedEmail,
            tenant.Address,
            new TenantLifecycle(Enum.Parse<TenantStatus>(tenant.Status), tenant.StatusReason, tenant.StatusChangedAt, tenant.CreatedAt),
            new TenantCommercialInfo(tenant.SdiCode, tenant.AccountManager));
    }

    public static TenantDbModel ToDbModel(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        // The normalized columns back the unique indexes, so they are derived
        // here, at the domain-to-persistence boundary, with the same
        // normalization used by the tenant uniqueness checks.
        return new TenantDbModel
        {
            Id = tenant.Id.ToString(),
            CompanyName = tenant.CompanyName,
            NormalizedCompanyName = TenantFieldNormalizer.NormalizeCompanyName(tenant.CompanyName),
            VatNumber = tenant.VatNumber,
            CompleteVatNumber = tenant.CompleteVatNumber,
            CertifiedEmail = tenant.CertifiedEmail,
            NormalizedCertifiedEmail = TenantFieldNormalizer.NormalizeCertifiedEmail(tenant.CertifiedEmail),
            Address = tenant.Address,
            Status = tenant.Lifecycle.Status.ToString(),
            StatusReason = tenant.Lifecycle.Reason,
            StatusChangedAt = tenant.Lifecycle.ChangedAt,
            CreatedAt = tenant.Lifecycle.CreatedAt,
            SdiCode = tenant.Commercial.SdiCode,
            AccountManager = tenant.Commercial.AccountManager,
        };
    }
}
