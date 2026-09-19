namespace PSAAS.Domain;

public class Profile
{
    public Profile()
    {
        Id = Guid.Empty;
        Name = string.Empty;
        Description = string.Empty;
        ProductiveUnitIds = Array.Empty<Guid>();
    }

    public Profile(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        ProductiveUnitIds = Array.Empty<Guid>();
    }

    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public Guid[] ProductiveUnitIds { get; private set; }

    public static Profile CreateEmpty()
    {
        return new Profile();
    }

    public static Profile Create(string name, string description)
    {
        return new Profile(name, description);
    }

    public bool Equals(Profile other)
    {
        if (other == null) return false;
        return Id.Equals(other.Id) && Name.Equals(other.Name) && Description.Equals(other.Description);
    }

    public void AddProductiveUnit(Guid productiveUnitId)
    {
        var productiveUnitIdsList = ProductiveUnitIds.ToList();
        productiveUnitIdsList.Add(productiveUnitId);
        ProductiveUnitIds = productiveUnitIdsList.ToArray();
    }
}