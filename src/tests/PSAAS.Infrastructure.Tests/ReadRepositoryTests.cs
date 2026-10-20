using Microsoft.EntityFrameworkCore;
using PSAAS.Domain;
using PSAAS.Infrastructure.Converters;
using PSAAS.Infrastructure.Data.DbContexts;
using PSAAS.Infrastructure.Data.DbModels;
using PSAAS.Infrastructure.Repositories.Tenants;
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
}
