using Microsoft.EntityFrameworkCore;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Domain;
using PSAAS.Infrastructure.Repositories.Tenants;
using Xunit;

namespace PSAAS.Infrastructure.Tests.Repositories;

/// <summary>
/// Derived from UC3-S1 and UC3-S2 (Attivare un nuovo cliente). Unique-violation handling (UC3 flow 3b)
/// needs Postgres errors and is not covered here: see <see cref="InMemoryDatabase"/>.
/// </summary>
public sealed class TenantWriteRepositoryTests
{
    private readonly Data.DbContexts.PsaasDbContext _db = InMemoryDatabase.Create();

    private static Tenant Alfa() => Tenant.Create(
        "Alfa Srl", "11111111111", "IT11111111111", "alfa@pec.it", new Address("Via Roma 1", "Roma", "00118", "IT"));

    [Fact]
    public async Task UC3_S1_SavesTheTenantWithItsNormalizedColumns()
    {
        var alfa = Alfa();

        await new TenantWriteRepository(_db).AddWithProfilesAndProductiveUnitsAsync(alfa, [], []);

        var stored = await _db.Tenants.AsNoTracking().SingleAsync();
        Assert.Equal(alfa.Id.ToString(), stored.Id);
        Assert.Equal(("alfasrl", "ALFA@PEC.IT"), (stored.NormalizedCompanyName, stored.NormalizedCertifiedEmail));
        Assert.Equal(alfa.Address, stored.Address);
        Assert.Empty(await _db.Profiles.ToListAsync());
    }

    [Fact]
    public async Task UC3_S2_SavesProfilesUnitsAndLinksResolvingTemporaryIds()
    {
        var alfa = Alfa();

        await new TenantWriteRepository(_db).AddWithProfilesAndProductiveUnitsAsync(
            alfa,
            [new TenantProfileCreateModel("op", "Operatore", "Di linea"), new TenantProfileCreateModel("osp", "Ospite", null)],
            [
                new TenantProductiveUnitCreateModel("Stabilimento", null, [], ["op"]),
                new TenantProductiveUnitCreateModel("Magazzino", "Deposito", [], [])
            ]);

        var profiles = await _db.Profiles.AsNoTracking().ToDictionaryAsync(profile => profile.Name);
        var units = await _db.ProductiveUnits.AsNoTracking().ToDictionaryAsync(unit => unit.Name);
        Assert.All(profiles.Values, profile => Assert.Equal(alfa.Id.ToString(), profile.TenantId));
        Assert.All(units.Values, unit => Assert.Equal(alfa.Id.ToString(), unit.TenantId));
        Assert.DoesNotContain(profiles.Values, profile => profile.Id is "op" or "osp");
        Assert.Equal("Deposito", units["Magazzino"].Description);
        var link = Assert.Single(await _db.ProductiveUnitProfiles.AsNoTracking().ToListAsync());
        Assert.Equal((units["Stabilimento"].Id, profiles["Operatore"].Id), (link.ProductiveUnitId, link.ProfileId));
    }

    [Fact]
    public async Task UC3_S2_UnknownTemporaryProfileId_IsRejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new TenantWriteRepository(_db).AddWithProfilesAndProductiveUnitsAsync(
                Alfa(), [], [new TenantProductiveUnitCreateModel("Stabilimento", null, [], ["inesistente"])]));
    }
}
