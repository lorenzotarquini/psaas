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

    [Fact]
    public void ToDbModel_DerivesNormalizedColumnsBackingUniqueIndexes()
    {
        var tenant = Tenant.Create(
            "Acme S.r.l.",
            "12345678901",
            "IT12345678901",
            "Pec@Example.test",
            new Address("Via Roma 1", "Roma", "00100", "Italia"));

        var dbModel = TenantDbModelConverter.ToDbModel(tenant);

        Assert.Equal(tenant.Id.ToString(), dbModel.Id);
        Assert.Equal("Acme S.r.l.", dbModel.CompanyName);
        Assert.Equal("acmesrl", dbModel.NormalizedCompanyName);
        Assert.Equal("12345678901", dbModel.VatNumber);
        Assert.Equal("IT12345678901", dbModel.CompleteVatNumber);
        Assert.Equal("Pec@Example.test", dbModel.CertifiedEmail);
        Assert.Equal("PEC@EXAMPLE.TEST", dbModel.NormalizedCertifiedEmail);
        Assert.Equal(tenant.Address, dbModel.Address);
    }

    [Fact]
    public void ToDbModel_RoundtripsThroughToDomain()
    {
        var tenant = Tenant.Create(
            "Acme S.r.l.",
            "12345678901",
            "IT12345678901",
            "pec@example.test",
            new Address("Via Roma 1", "Roma", "00100", "Italia"));

        var roundtripped = TenantDbModelConverter.ToDomain(TenantDbModelConverter.ToDbModel(tenant));

        Assert.Equal(tenant.Id, roundtripped.Id);
        Assert.Equal(tenant.CompanyName, roundtripped.CompanyName);
        Assert.Equal(tenant.VatNumber, roundtripped.VatNumber);
        Assert.Equal(tenant.CompleteVatNumber, roundtripped.CompleteVatNumber);
        Assert.Equal(tenant.CertifiedEmail, roundtripped.CertifiedEmail);
        Assert.Equal(tenant.Address, roundtripped.Address);
    }
}
