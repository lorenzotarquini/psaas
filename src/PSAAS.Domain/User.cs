namespace PSAAS.Domain;

public class User
{
    private User()
    {
        Id = Guid.Empty;
        KeycloakId = string.Empty;
        UserName = string.Empty;
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Roles = Array.Empty<string>();
        Claims = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    }

    private User(string keycloakId, string userName, string email, string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        KeycloakId = keycloakId;
        UserName = userName;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Roles = Array.Empty<string>();
        Claims = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    }

    public Guid Id { get; }
    public string KeycloakId { get; }
    public string UserName { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public Guid TenantId { get; private set; }
    public Guid ProfileId { get; private set; }
    public string[] Roles { get; private set; }
    public Dictionary<string, string[]> Claims { get; private set; }

    public static User CreateEmpty()
    {
        return new User();
    }

    public static User Create(string keycloakId, string userName, string email, string firstName, string lastName)
    {
        return new User(keycloakId, userName, email, firstName, lastName);
    }

    public bool Equals(User other)
    {
        if (other == null) return false;
        return Id.Equals(other.Id) && KeycloakId.Equals(other.KeycloakId) && UserName.Equals(other.UserName) &&
               Email.Equals(other.Email) && FirstName.Equals(other.FirstName) && LastName.Equals(other.LastName) &&
               TenantId.Equals(other.TenantId) && ProfileId.Equals(other.ProfileId) && Roles.SequenceEqual(other.Roles) &&
               ClaimsAreEqual(other.Claims);
    }

    public void AddToTenant(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public void AddProfile(Guid profileId)
    {
        ProfileId = profileId;
    }

    public void AddRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || HasRole(role)) return;

        var roles = Roles.ToList();
        roles.Add(role);
        Roles = roles.ToArray();
    }

    public bool HasRole(string role)
    {
        return Roles.Any(existingRole => existingRole.Equals(role, StringComparison.OrdinalIgnoreCase));
    }

    public void AddClaim(string type, string value)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value)) return;

        if (!Claims.TryGetValue(type, out var values))
        {
            Claims[type] = [value];
            return;
        }

        if (values.Any(existingValue => existingValue.Equals(value, StringComparison.OrdinalIgnoreCase))) return;

        var valuesList = values.ToList();
        valuesList.Add(value);
        Claims[type] = valuesList.ToArray();
    }

    public bool HasClaim(string type, string value)
    {
        return Claims.TryGetValue(type, out var values) &&
               values.Any(existingValue => existingValue.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    private bool ClaimsAreEqual(Dictionary<string, string[]> otherClaims)
    {
        if (Claims.Count != otherClaims.Count) return false;

        foreach (var claim in Claims)
        {
            if (!otherClaims.TryGetValue(claim.Key, out var otherValues)) return false;
            if (!claim.Value.SequenceEqual(otherValues)) return false;
        }

        return true;
    }
}
