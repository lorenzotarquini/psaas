namespace FinHorizon.MySync.Domain.Common;

/// <summary>Name, description and tag the user gives to an account or a debt (UC2-R2, R3; UC3-R2).</summary>
public sealed record Descriptor
{
    public const int MaxNameLength = 50;
    public const int MaxDescriptionLength = 250;
    public const int MaxTagLength = 30;

    public Descriptor(string name, string? description, string? tag)
    {
        Name = TextNormalizer.Clean(name) ?? throw new ArgumentException("A name is required.", nameof(name));
        Description = TextNormalizer.Clean(description);
        Tag = TextNormalizer.Clean(tag);
        if (Name.Length > MaxNameLength || Description?.Length > MaxDescriptionLength || Tag?.Length > MaxTagLength)
        {
            throw new ArgumentException("A descriptor field is too long.");
        }
    }

    public string Name { get; }

    public string? Description { get; }

    public string? Tag { get; }

    public string NormalizedName => TextNormalizer.Normalize(Name);
}
