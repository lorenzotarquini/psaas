namespace FinHorizon.MySync.Application.Common;

/// <summary>The value of a use case that returns nothing (a deletion).</summary>
public readonly record struct Unit
{
    public static readonly Unit Value;
}
