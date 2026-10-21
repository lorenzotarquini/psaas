using PSAAS.Infrastructure.Repositories.Tenants;
using Xunit;

namespace PSAAS.Infrastructure.Tests.Repositories;

/// <summary>Derived from UC3-S5 (values used by another tenant) and UC4-S2/S3 (a tenant may keep its own values).</summary>
public sealed class TenantUniquenessCheckerTests
{
    private readonly Data.DbContexts.PsaasDbContext _db = InMemoryDatabase.Create();

    private TenantUniquenessChecker Checker => new(_db);

    [Fact]
    public async Task UC3_S5_ValuesOfAnExistingTenant_AreTaken()
    {
        _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");

        Assert.False(await Checker.IsCompanyNameUniqueAsync("alfasrl"));
        Assert.False(await Checker.IsCompleteVatNumberUniqueAsync("IT11111111111"));
        Assert.False(await Checker.IsCertifiedEmailUniqueAsync("ALFA@PEC.IT"));
    }

    [Fact]
    public async Task UC3_S1_NewValues_AreFree()
    {
        _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");

        Assert.True(await Checker.IsCompanyNameUniqueAsync("betasnc"));
        Assert.True(await Checker.IsCompleteVatNumberUniqueAsync("IT22222222222"));
        Assert.True(await Checker.IsCertifiedEmailUniqueAsync("BETA@PEC.IT"));
    }

    [Fact]
    public async Task UC4_S2_ATenantDoesNotCollideWithItself()
    {
        var alfa = Guid.Parse(_db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it").Id);

        Assert.True(await Checker.IsCompanyNameUniqueAsync("alfasrl", alfa));
        Assert.True(await Checker.IsCompleteVatNumberUniqueAsync("IT11111111111", alfa));
        Assert.True(await Checker.IsCertifiedEmailUniqueAsync("ALFA@PEC.IT", alfa));
    }

    [Fact]
    public async Task UC4_S3_ValuesOfAnotherTenant_StayTakenWhenExcludingOneself()
    {
        _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        var beta = Guid.Parse(_db.AddTenant("Beta Snc", "IT22222222222", "beta@pec.it").Id);

        Assert.False(await Checker.IsCompanyNameUniqueAsync("alfasrl", beta));
        Assert.False(await Checker.IsCompleteVatNumberUniqueAsync("IT11111111111", beta));
        Assert.False(await Checker.IsCertifiedEmailUniqueAsync("ALFA@PEC.IT", beta));
    }
}
