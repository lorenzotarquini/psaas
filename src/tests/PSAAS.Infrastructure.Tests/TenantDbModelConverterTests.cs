using PSAAS.Domain;
using PSAAS.Infrastructure.Converters;
using PSAAS.Infrastructure.Data.DbModels;
using Xunit;

namespace PSAAS.Infrastructure.Tests;

public sealed class TenantDbModelConverterTests
{
    [Fact]
    public void ToDomain_PreservesTenantId()
    {
        var id = Guid.NewGuid();
        var dbModel = new TenantDbModel
        {
            Id = id.ToString(),
            CompanyName = "Acme Srl",
            VatNumber = "12345678901",
            CompleteVatNumber = "IT12345678901",
            CertifiedEmail = "acme@example.test",
            Address = new Address("Via Roma 1", "Roma", "00100", "IT")
        };

        var tenant = TenantDbModelConverter.ToDomain(dbModel);

        Assert.Equal(id, tenant.Id);
        Assert.Equal(dbModel.CompanyName, tenant.CompanyName);
        Assert.Equal(dbModel.VatNumber, tenant.VatNumber);
        Assert.Equal(dbModel.CompleteVatNumber, tenant.CompleteVatNumber);
        Assert.Equal(dbModel.CertifiedEmail, tenant.CertifiedEmail);
        Assert.Equal(dbModel.Address, tenant.Address);
    }
}
