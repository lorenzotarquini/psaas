using Microsoft.EntityFrameworkCore;
using PSAAS.Domain;
using PSAAS.Infrastructure.Converters;
using PSAAS.Infrastructure.Data.DbContexts;
using PSAAS.Infrastructure.Data.DbModels;
using PSAAS.Infrastructure.Repositories.Tenants;
using PSAAS.Infrastructure.Repositories.Users;
using Xunit;

namespace PSAAS.Infrastructure.Tests;

public sealed class ReadRepositoryTests
{
    [Fact]
    public async Task GetPageAsync_SortsByCompanyNameAndPages()
    {
        await using var db = await SeedAsync("Gamma Spa", "Alfa Srl", "Beta Snc");

        var page = await new TenantReadRepository(db).GetPageAsync(null, page: 2, pageSize: 2);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal("Gamma Spa", Assert.Single(page.Items).CompanyName);
    }

    [Theory]
    [InlineData("alfa s.r.l.", "Alfa Srl")]
    [InlineData("01", "Alfa Srl")]
    [InlineData("it02", "Beta Snc")]
    public async Task GetPageAsync_SearchesNormalizedNameAndVat(string search, string expected)
    {
        await using var db = await SeedAsync("Alfa Srl", "Beta Snc");

        var page = await new TenantReadRepository(db).GetPageAsync(search, 1, 20);

        Assert.Equal(expected, Assert.Single(page.Items).CompanyName);
    }

    [Fact]
    public async Task GetPageAsync_SearchWithoutLettersOrDigitsMatchesNothingByName()
    {
        await using var db = await SeedAsync("Alfa Srl");

        var page = await new TenantReadRepository(db).GetPageAsync("-", 1, 20);

        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task UserReadRepository_FiltersByTenantAndResolvesProfileName()
    {
        await using var db = await SeedAsync("Alfa Srl");
        var tenantId = (await db.Tenants.SingleAsync()).Id;
        db.Profiles.Add(new ProfileDbModel { Id = "p1", TenantId = tenantId, Name = "Operatore" });
        var user = NewUser(tenantId, "kc-1", "p1");
        db.Users.AddRange(user, NewUser(null, "kc-2", null));
        await db.SaveChangesAsync();
        var repository = new UserReadRepository(db);

        var users = await repository.GetByTenantAsync(tenantId);
        var otherTenant = await repository.GetByIdAsync(Guid.NewGuid().ToString(), user.Id);
        var byKeycloak = await repository.GetByKeycloakIdAsync("kc-2");

        Assert.Equal("Operatore", Assert.Single(users).ProfileName);
        Assert.Null(otherTenant);
        Assert.Null(byKeycloak!.TenantId);
    }

    private static async Task<PsaasDbContext> SeedAsync(params string[] companyNames)
    {
        var db = new PsaasDbContext(new DbContextOptionsBuilder<PsaasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        for (var i = 0; i < companyNames.Length; i++)
        {
            db.Tenants.Add(TenantDbModelConverter.ToDbModel(Tenant.Create(
                companyNames[i],
                $"0{i + 1}".PadLeft(11, '9'),
                $"IT0{i + 1}".PadRight(13, '9'),
                $"pec{i}@test.it",
                new Address("Via Roma 1", "Roma", "00100", "IT"))));
        }

        await db.SaveChangesAsync();
        return db;
    }

    private static UserDbModel NewUser(string? tenantId, string keycloakId, string? profileId) => new()
    {
        Id = Guid.NewGuid(),
        KeycloakId = keycloakId,
        UserName = keycloakId,
        Email = $"{keycloakId}@test.it",
        FirstName = "Mario",
        LastName = "Rossi",
        TenantId = tenantId,
        ProfileId = profileId
    };
}
