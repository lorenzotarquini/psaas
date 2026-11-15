namespace FinHorizon.MySync.Domain.Common;

/// <summary>The user who owns a piece of data: the Keycloak subject (RNF2).</summary>
public readonly record struct OwnerId
{
    public OwnerId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
