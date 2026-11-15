using System.ComponentModel.DataAnnotations;

namespace FinHorizon.MySync.Application.Abstractions.Limits;

/// <summary>Section "MySync" of the configuration (env vars MySync__MaxAccounts, MySync__MaxDebts).</summary>
public sealed class LimitsOptions
{
    public const string SectionName = "MySync";

    /// <summary>UC2-R6 (D2).</summary>
    [Range(1, 1000)]
    public int MaxAccounts { get; set; } = 2;

    /// <summary>UC3-R5 (D2): 0 turns debts off.</summary>
    [Range(0, 1000)]
    public int MaxDebts { get; set; } = 1;
}
