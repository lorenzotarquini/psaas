using PSAAS.Infrastructure.Repositories.Tenants;
using Xunit;

namespace PSAAS.Infrastructure.Tests.Repositories;

/// <summary>Derived from UC1 (Cercare i clienti) and UC2 (Consultare la scheda di un cliente).</summary>
public sealed class TenantReadRepositoryTests
{
    private readonly Data.DbContexts.PsaasDbContext _db = InMemoryDatabase.Create();

    private TenantReadRepository Repository => new(_db);

    [Fact]
    public async Task UC1_S1_PagesSortedByCompanyName()
    {
        _db.AddTenant("Gamma Spa", "IT33333333333", "gamma@pec.it");
        _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        _db.AddTenant("Beta Snc", "IT22222222222", "beta@pec.it");

        var page1 = await Repository.GetPageAsync(null, page: 1, pageSize: 2);
        var page2 = await Repository.GetPageAsync(null, page: 2, pageSize: 2);

        Assert.Equal(["Alfa Srl", "Beta Snc"], page1.Items.Select(tenant => tenant.CompanyName));
        Assert.Equal((3, 1, 2), (page1.TotalCount, page1.Page, page1.PageSize));
        Assert.Equal(["Gamma Spa"], page2.Items.Select(tenant => tenant.CompanyName));
    }

    [Theory]
    [InlineData("ALFA s.r.l.")]
    [InlineData("  alfa  ")]
    public async Task UC1_S2_SearchByCompanyName_IgnoresCaseSpacesAndPunctuation(string search)
    {
        _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        _db.AddTenant("Beta Snc", "IT22222222222", "beta@pec.it");

        var result = await Repository.GetPageAsync(search, 1, 20);

        Assert.Equal(["Alfa Srl"], result.Items.Select(tenant => tenant.CompanyName));
    }

    [Theory]
    [InlineData("it2222")]
    [InlineData("22222222222")]
    public async Task UC1_S3_SearchByVatNumberOrCompleteVatNumber(string search)
    {
        _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        _db.AddTenant("Beta Snc", "IT22222222222", "beta@pec.it");

        var result = await Repository.GetPageAsync(search, 1, 20);

        Assert.Equal(["Beta Snc"], result.Items.Select(tenant => tenant.CompanyName));
    }

    [Fact]
    public async Task UC1_S4_SearchByCertifiedEmail_IgnoresCase()
    {
        _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        _db.AddTenant("Beta Snc", "IT22222222222", "beta@pec.it");

        var result = await Repository.GetPageAsync("BETA@PEC", 1, 20);

        Assert.Equal(["Beta Snc"], result.Items.Select(tenant => tenant.CompanyName));
    }

    [Theory]
    [InlineData("zeta")]
    [InlineData("-")]
    public async Task UC1_S5_NoMatch_IsAnEmptyPage(string search)
    {
        _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");

        var result = await Repository.GetPageAsync(search, 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task UC2_S1_GetById_RehydratesTheTenant()
    {
        var stored = _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it", city: "Roma");

        var tenant = await Repository.GetByIdAsync(Guid.Parse(stored.Id));

        Assert.Equal(("Alfa Srl", "11111111111", "Roma"), (tenant!.CompanyName, tenant.VatNumber, tenant.Address.city));
    }

    [Fact]
    public async Task UC2_S3_GetById_UnknownTenantIsNull()
    {
        Assert.Null(await Repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UC2_S1_Structure_ListsProfilesAndUnitsWithTheirLinks()
    {
        var alfa = _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");
        var operatore = _db.AddProfile(alfa, "Operatore");
        _db.AddUnit(alfa, "Stabilimento", operatore);
        _db.AddUnit(alfa, "Magazzino");
        var beta = _db.AddTenant("Beta Snc", "IT22222222222", "beta@pec.it");
        _db.AddProfile(beta, "Revisore");

        var structure = await Repository.GetProfilesAndProductiveUnitsAsync(alfa.Id);

        Assert.Equal(["Operatore"], structure.Profiles.Select(profile => profile.Name));
        Assert.Equal(["Magazzino", "Stabilimento"], structure.ProductiveUnits.Select(unit => unit.Name));
        Assert.Empty(structure.ProductiveUnits[0].ProfileIds);
        Assert.Equal([operatore.Id], structure.ProductiveUnits[1].ProfileIds);
    }

    [Fact]
    public async Task UC2_S2_Structure_OfATenantWithoutOrganizationIsEmpty()
    {
        var alfa = _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");

        var structure = await Repository.GetProfilesAndProductiveUnitsAsync(alfa.Id);

        Assert.Empty(structure.Profiles);
        Assert.Empty(structure.ProductiveUnits);
    }

    [Fact]
    public async Task UC11_S6_FindByUniqueValue_NamesTheTenantUsingIt_ExceptTheExcludedOne()
    {
        var alfa = _db.AddTenant("Alfa Srl", "IT11111111111", "alfa@pec.it");

        var found = await Repository.FindByUniqueValueAsync("completeVatNumber", "IT11111111111", null);
        var excluded = await Repository.FindByUniqueValueAsync("companyName", "alfasrl", Guid.Parse(alfa.Id));

        Assert.Equal(new PSAAS.Application.Contracts.Tenants.TenantReference(Guid.Parse(alfa.Id), "Alfa Srl"), found);
        Assert.Null(excluded);
    }
}
