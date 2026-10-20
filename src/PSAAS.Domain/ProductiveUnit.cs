namespace PSAAS.Domain;

public class ProductiveUnit
{
    private ProductiveUnit()
    {
        Id = Guid.Empty;
        Name = string.Empty;
        Description = string.Empty;
    }

    private ProductiveUnit(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
    }

    public Guid Id { get; }
    public string Name { get; }

    public string Description { get; }
    public Guid TenantId { get; private set; }

    public static ProductiveUnit CreateEmpty()
    {
        return new ProductiveUnit();
    }

    public static ProductiveUnit Create(string name, string description)
    {
        return new ProductiveUnit(name, description);
    }

    public bool Equals(ProductiveUnit other)
    {
        if (other == null) return false;
        return Id.Equals(other.Id) && Name.Equals(other.Name) && Description.Equals(other.Description) &&
               TenantId.Equals(other.TenantId);
    }

    public void AddToTenant(Guid tenantId)
    {
        TenantId = tenantId;
    }
}