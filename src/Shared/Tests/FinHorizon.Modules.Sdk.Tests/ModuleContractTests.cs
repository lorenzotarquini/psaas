using Xunit;

namespace FinHorizon.Modules.Sdk.Tests;

/// <summary>The module contract (docs/architettura-moduli.md, C1–C6).</summary>
public sealed class ModuleContractTests
{
    private static readonly ModuleTier Base = new(1, "Base", [], new Dictionary<string, int> { ["accounts"] = 2 });
    private static readonly ModuleTier Plus = new(2, "Plus", ["projection"], new Dictionary<string, int> { ["accounts"] = 10 });

    [Fact]
    public void C1_C2_ValidTiers_HaveNoErrors() => Assert.Empty(TierLevels.Validate([Base, Plus]));

    [Theory]
    [InlineData(0, "Base", "tiers[0].level")]
    [InlineData(4, "Base", "tiers[0].level")]
    [InlineData(1, " ", "tiers[0].name")]
    public void C1_C2_LevelAndName(int level, string name, string field) =>
        Assert.Contains(field, TierLevels.Validate([new ModuleTier(level, name)]).Keys);

    [Fact]
    public void C1_SameLevelTwice_IsInvalid() =>
        Assert.Equal("InvalidValue", TierLevels.Validate([Base, Base with { Name = "Altro" }])["tiers[1].level"]);

    [Fact]
    public void C2_KeysLowercaseAndLimitsNotNegative()
    {
        var errors = TierLevels.Validate([new ModuleTier(1, "Base", ["Projection"], new Dictionary<string, int> { ["accounts"] = -1 })]);

        Assert.Equal(("InvalidFormat", "InvalidValue"), (errors["tiers[0].features"], errors["tiers[0].limits"]));
    }

    [Fact]
    public void C1_AHigherLevelNeverGivesLess()
    {
        var losesFeature = TierLevels.Validate([Plus with { Level = 1 }, Base with { Level = 2 }]);
        var lowersLimit = TierLevels.Validate([Base, Plus with { Limits = new Dictionary<string, int> { ["accounts"] = 1 } }]);

        Assert.Contains("tiers[1]", losesFeature.Keys);
        Assert.Contains("tiers[1]", lowersLimit.Keys);
    }

    [Fact]
    public void C3_C4_NothingPaid_IsReadOnly()
    {
        var none = ModuleEntitlement.For(EntitlementStatus.None, 2, [Base, Plus]);

        Assert.Equal((false, false, 7), (none.CanWrite, none.Has("projection"), none.Limit("accounts", 7)));
    }

    [Fact]
    public void C3_C5_C6_PaidTier_GivesItsFeaturesAndLimits()
    {
        var plus = ModuleEntitlement.For(EntitlementStatus.PastDue, 2, [Base, Plus]);
        var basic = ModuleEntitlement.For(EntitlementStatus.Active, 1, [Base, Plus]);

        Assert.Equal((true, true, 10), (plus.CanWrite, plus.Has("projection"), plus.Limit("accounts", 0)));
        Assert.Equal((true, false, 2), (basic.CanWrite, basic.Has("projection"), basic.Limit("accounts", 0)));
    }

    [Fact]
    public void C3_ALevelTheModuleDoesNotDeclare_GivesNothing() =>
        Assert.False(ModuleEntitlement.For(EntitlementStatus.Active, 3, [Base, Plus]).CanWrite);
}
