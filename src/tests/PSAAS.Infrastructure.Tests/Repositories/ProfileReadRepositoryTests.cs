using PSAAS.Application.Contracts.Users;
using PSAAS.Infrastructure.Repositories.Profiles;
using Xunit;

namespace PSAAS.Infrastructure.Tests.Repositories;

/// <summary>Derived from UC9 (panoramica dei profili), UC6-S6 (a chosen profile must exist) and UC7 (tenant of each profile).</summary>
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
            [("Amministrativo", beta.Id, "Beta Snc"), ("Operatore", alfa.Id, "Alfa Srl")],
            profiles.Select(profile => (profile.Name, profile.TenantId, profile.TenantCompanyName)));
    }

    [Fact]
    public async Task UC9_S2_NoProfiles_IsEmpty()
    {
        Assert.Empty(await Repository.GetAllAsync());
    }

    [Fact]
    public async Task UC6_S6_GetWithTenant_ReturnsOnlyProfilesThatExist()
    {
        var alfa = _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        var operatore = _db.AddProfile(alfa, "Operatore");

        var existing = await Repository.GetWithTenantAsync([$" {operatore.Id} ", "inesistente", "", operatore.Id]);

        Assert.Equal([operatore.Id], existing.Select(profile => profile.Id));
    }

    [Fact]
    public async Task UC7_S1_S3_GetWithTenant_ResolvesTheTenantOfEachProfile()
    {
        var alfa = _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        var beta = _db.AddTenant("Beta Snc", "IT22222222222", "beta@pec.it");
        var operatore = _db.AddProfile(alfa, "Operatore");
        var revisore = _db.AddProfile(beta, "Revisore");

        var profiles = await Repository.GetWithTenantAsync([operatore.Id, revisore.Id]);

        UserProfileReadModel[] expected =
        [
            new(operatore.Id, "Operatore", alfa.Id, "Alfa Srl"),
            new(revisore.Id, "Revisore", beta.Id, "Beta Snc")
        ];
        Assert.Equal(expected, profiles.OrderBy(profile => profile.Name));
    }
}
