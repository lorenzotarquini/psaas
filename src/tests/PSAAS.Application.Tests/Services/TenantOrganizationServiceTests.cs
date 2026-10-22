using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Users;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Tenants;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC5 (Modificare l'organizzazione di un cliente).</summary>
public sealed class TenantOrganizationServiceTests
{
    private static readonly Tenant Alfa = Tenant.Create(
        "Alfa Srl", "11111111111", "IT11111111111", "alfa@pec.it", new Address("Via Roma 1", "Roma", "00118", "IT"));

    private static readonly Guid PlantId = Guid.NewGuid();

    private readonly FakeTenantReadRepository _reader = new()
    {
        Tenant = Alfa,
        Structure = new(
            [new TenantProfileReadModel("op", "Operatore", null), new TenantProfileReadModel("rev", "Revisore", null)],
            [new TenantProductiveUnitReadModel(PlantId, "Stabilimento", null, ["op"])]),
    };

    private readonly FakeTenantWriteRepository _writer = new();
    private readonly FakeUserDirectory _users = new();

    private TenantOrganizationService Service => new(_reader, _writer, _users);

    private static TenantOrganizationRequest Request(
        TenantOrganizationProfileRequest[] profiles,
        TenantOrganizationUnitRequest[] units) => new(profiles, units);

    [Fact]
    public async Task UC5_S1_S2_KeepsIdentitiesAndLinksUnitsByKey()
    {
        var result = await Service.SaveAsync(Alfa.Id, Request(
            [new("op", "p", " Addetto ", null), new(null, "new", "Nuovo", " ")],
            [new(PlantId, "Fabbrica", null, ["p", "new"])]));

        Assert.True(result.Succeeded);
        var profiles = _writer.EditedProfiles.ToArray();
        Assert.Equal([("op", "Addetto"), (null, "Nuovo")], profiles.Select(p => (p.Id, p.Name)));
        Assert.Null(profiles[1].Description);
        var unit = Assert.Single(_writer.EditedProductiveUnits);
        Assert.Equal(PlantId, unit.Id);
        Assert.Equal(profiles.Select(p => p.ClientId), unit.ProfileClientIds);
    }

    [Fact]
    public async Task UC5_S4_RemovedProfilesAreTakenOffTheUsers()
    {
        await Service.SaveAsync(Alfa.Id, Request([new("op", "op", "Operatore", null)], []));

        Assert.Equal(["rev"], _users.RemovedProfiles);
    }

    [Fact]
    public async Task UC5_S4_KeycloakDown_TheOrganizationIsSavedAnyway()
    {
        _users.Unavailable = true;

        var result = await Service.SaveAsync(Alfa.Id, Request([], []));

        Assert.True(result.Succeeded);
        Assert.Empty(_writer.EditedProfiles);
    }

    [Fact]
    public async Task UC5_S5_DuplicateNamesAndUnknownKeys_AreReportedByPosition()
    {
        var result = await Service.SaveAsync(Alfa.Id, Request(
            [new(null, "a", "Operatore", null), new(null, "b", "OPERATORE", null)],
            [new(null, "Magazzino", null, ["zzz"])]));

        Assert.Equal("NotUnique", result.Errors["profiles[1].name"]);
        Assert.Equal("InvalidValue", result.Errors["productiveUnits[0].profileKeys[0]"]);
        Assert.Null(_writer.Saved);
    }

    [Fact]
    public async Task UC5_S6_IdsOfAnotherTenant_AreInvalid()
    {
        var result = await Service.SaveAsync(Alfa.Id, Request(
            [new("someone-else", "x", "Rubato", null)],
            [new(Guid.NewGuid(), "Rubata", null)]));

        Assert.Equal("InvalidValue", result.Errors["profiles[0].id"]);
        Assert.Equal("InvalidValue", result.Errors["productiveUnits[0].id"]);
    }

    [Fact]
    public async Task UC5_S8_MissingTenant_IsNotFound()
    {
        Assert.False((await Service.SaveAsync(Guid.NewGuid(), Request([], []))).Found);
    }
}
