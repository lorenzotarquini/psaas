using Xunit;

namespace PSAAS.Domain.Tests;

/// <summary>Derived from UC3-S1 (a new tenant gets its own identity) and UC2 (a stored tenant keeps it).</summary>
public sealed class TenantTests
{
    private static readonly Address Rome = new("Via Roma 1", "Roma", "00118", "IT");

    [Fact]
    public void Create_AssignsANewIdentity()
    {
        var first = Tenant.Create("Alfa Srl", "12345678901", "IT12345678901", "alfa@pec.it", Rome);
        var second = Tenant.Create("Alfa Srl", "12345678901", "IT12345678901", "alfa@pec.it", Rome);

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Rehydrate_KeepsTheStoredIdentityAndData()
    {
        var id = Guid.NewGuid();

        var tenant = Tenant.Rehydrate(id, "Alfa Srl", "12345678901", "IT12345678901", "alfa@pec.it", Rome);

        Assert.Equal(id, tenant.Id);
        Assert.Equal(("Alfa Srl", "12345678901", "IT12345678901", "alfa@pec.it", Rome),
            (tenant.CompanyName, tenant.VatNumber, tenant.CompleteVatNumber, tenant.CertifiedEmail, tenant.Address));
    }

    [Fact]
    public void Rehydrate_RejectsAnEmptyIdentity()
    {
        Assert.Throws<ArgumentException>(() =>
            Tenant.Rehydrate(Guid.Empty, "Alfa Srl", "12345678901", "IT12345678901", "alfa@pec.it", Rome));
    }

    [Theory]
    [InlineData("12345678901", true)]
    [InlineData("1234567890", false)]
    [InlineData("1234567890A", false)]
    [InlineData("", false)]
    public void IsValidVat_RequiresElevenDigits(string vatNumber, bool expected)
    {
        var tenant = Tenant.Create("Alfa Srl", vatNumber, "IT12345678901", "alfa@pec.it", Rome);

        Assert.Equal(expected, tenant.IsValidVat());
    }

    [Theory]
    [InlineData("IT12345678901", true)]
    [InlineData("IT1234567890", false)]
    [InlineData("", false)]
    public void IsValidCompleteVat_RequiresThirteenCharacters(string completeVatNumber, bool expected)
    {
        var tenant = Tenant.Create("Alfa Srl", "12345678901", completeVatNumber, "alfa@pec.it", Rome);

        Assert.Equal(expected, tenant.IsValidCompleteVat());
    }
}
