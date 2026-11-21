using FinHorizon.Modules.Sdk;

namespace FinHorizon.MySync.Application.Features.Memberships;

/// <summary>
/// MySync as an implementation of the module contract (docs/architettura-moduli.md): its manifest with the tiers it
/// sells. The Admin Portal registers the same manifest (seeded by its migration ModuleTiers, or pasted in UC15 from
/// GET /api/module/manifest) so that plans can be sold for these levels.
/// </summary>
public static class MySyncModule
{
    public const string Id = "mysync";

    /// <summary>UC14 and the dashboard forecast (RF19, RF22).</summary>
    public const string Projection = "projection";

    public const string AccountsLimit = "accounts";

    public const string DebtsLimit = "debts";

    public const string SubscriptionsLimit = "subscriptions";

    public static IReadOnlyList<ModuleTier> Tiers { get; } =
    [
        new(1, "Base", [], Limits(2, 1, 5)),
        new(2, "Plus", [Projection], Limits(10, 10, 20)),
        new(3, "Pro", [Projection], Limits(100, 50, 100)),
    ];

    public static ModuleManifest Manifest(string version) => new(
        Id,
        "MySync",
        "Finanze personali: conti, debiti, movimenti e patrimonio",
        version,
        "wallet",
        ModuleStatuses.Available,
        [],
        [],
        null,
        null,
        Tiers);

    private static Dictionary<string, int> Limits(int accounts, int debts, int subscriptions) => new()
    {
        [AccountsLimit] = accounts,
        [DebtsLimit] = debts,
        [SubscriptionsLimit] = subscriptions,
    };
}

/// <summary>The tiers MySync applies at runtime: its own manifest; the acceptance tests replace them.</summary>
public interface IModuleTiers
{
    IReadOnlyList<ModuleTier> Tiers { get; }
}

public sealed class DeclaredModuleTiers : IModuleTiers
{
    public IReadOnlyList<ModuleTier> Tiers => MySyncModule.Tiers;
}
