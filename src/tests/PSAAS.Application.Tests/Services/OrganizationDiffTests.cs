using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Services.Tenants;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC12-S3: what an organization save changed, by name.</summary>
public sealed class OrganizationDiffTests
{
    private static readonly Guid Plant = Guid.NewGuid();
    private static readonly Guid Store = Guid.NewGuid();

    private static readonly TenantStructureReadModel Before = new(
        [new("op", "Operatore", null), new("rev", "Revisore", null)],
        [new(Plant, "Stabilimento", null, ["op"]), new(Store, "Magazzino", null, [])]);

    [Fact]
    public void UC12_S3_NamesAddedRemovedRenamedAndCountsAccessChanges()
    {
        var diff = OrganizationDiff.Between(Before, new TenantOrganizationRequest(
            [new("op", "op", "Addetto", null), new(null, "new", "Ispettore", null)],
            [new(Plant, "Fabbrica", null, []), new(null, "Uffici", null, ["new"])]));

        Assert.Equal(["Ispettore"], diff.ProfilesAdded);
        Assert.Equal(["Revisore"], diff.ProfilesRemoved);
        Assert.Equal([new AuditRename("Operatore", "Addetto")], diff.ProfilesRenamed);
        Assert.Equal(["Uffici"], diff.UnitsAdded);
        Assert.Equal(["Magazzino"], diff.UnitsRemoved);
        Assert.Equal([new AuditRename("Stabilimento", "Fabbrica")], diff.UnitsRenamed);
        Assert.Equal(1, diff.AccessChanges);
    }

    [Fact]
    public void UC12_S3_SameOrganization_IsEmpty()
    {
        var diff = OrganizationDiff.Between(Before, new TenantOrganizationRequest(
            [new("op", "op", "Operatore", null), new("rev", "rev", "Revisore", null)],
            [new(Plant, "Stabilimento", null, ["op"]), new(Store, "Magazzino", null, [])]));

        Assert.True(diff.IsEmpty);
    }
}
