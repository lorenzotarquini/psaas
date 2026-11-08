namespace FinHorizon.Modules.Sdk;

/// <summary>The life of a module in the catalog, as its manifest declares it.</summary>
public static class ModuleStatuses
{
    public const string Beta = "Beta";
    public const string Available = "Available";
    public const string Deprecated = "Deprecated";

    public static readonly string[] All = [Beta, Available, Deprecated];
}

/// <summary>What a module declares about itself when it is registered in the catalog (UC15).</summary>
/// <param name="Id">Stable key, lowercase letters, digits and dashes (e.g. "warehouse").</param>
/// <param name="Version">Semantic version, e.g. "1.4.0".</param>
/// <param name="Requires">Modules it needs (core is always there).</param>
/// <param name="ProvisionUrl">Called when the module is activated for a tenant; none means nothing to prepare.</param>
public sealed record ModuleManifest(
    string? Id,
    string? Name,
    string? Description,
    string? Version,
    string? Icon,
    string? Status,
    IReadOnlyList<string>? Requires,
    IReadOnlyList<string>? Permissions,
    string? ProvisionUrl,
    string? DeprovisionUrl);
