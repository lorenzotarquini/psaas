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
            tenant.Address);
    }
}
