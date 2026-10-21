using PSAAS.Infrastructure.Repositories.Profiles;
using Xunit;

namespace PSAAS.Infrastructure.Tests.Repositories;

/// <summary>Derived from UC9 (panoramica dei profili) and UC6-S6 (a chosen profile must exist).</summary>
public sealed class ProfileReadRepositoryTests
{
    private readonly Data.DbContexts.PsaasDbContext _db = InMemoryDatabase.Create();

    private ProfileReadRepository Repository => new(_db);

    [Fact]
    public async Task UC9_S1_AllProfilesSortedByNameWithTheirTenant()
    {
        var alfa = _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        var beta = _db.AddTenant("Beta Snc", "IT22222222222", "beta@pec.it");
        _db.AddProfile(alfa, "Operatore");
        _db.AddProfile(beta, "Amministrativo");

        var profiles = await Repository.GetAllAsync();

        Assert.Equal(
            [("Amministrativo", beta.Id), ("Operatore", alfa.Id)],
            profiles.Select(profile => (profile.Name, profile.TenantId)));
    }

    [Fact]
    public async Task UC9_S2_NoProfiles_IsEmpty()
    {
        Assert.Empty(await Repository.GetAllAsync());
    }

    [Fact]
    public async Task UC6_S6_GetExistingIds_ReturnsOnlyProfilesThatExist()
    {
        var alfa = _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        var operatore = _db.AddProfile(alfa, "Operatore");

        var existing = await Repository.GetExistingIdsAsync([$" {operatore.Id} ", "inesistente", "", operatore.Id]);

        Assert.Equal([operatore.Id], existing);
    }
}
