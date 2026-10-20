using System.Net;
using System.Text.Json;
using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Contracts.Profiles;
using PSAAS.Domain;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

// Each test builds its own factory so the in-memory fakes never leak between tests.
public sealed class EndpointTests
{
    private static readonly Tenant Acme = Tenant.Create(
        "Acme Srl", "12345678901", "IT12345678901", "acme@pec.test", new Address("Via Roma 1", "Roma", "00100", "IT"));

    [Fact]
    public async Task Tenants_ReturnsPageWithCityAndForwardsQuery()
    {
        using var factory = new BackendFactory();
        factory.Tenants.Tenants.Add(Acme);

        var json = await GetJsonAsync(factory, "/api/tenants?search=acme&page=2&pageSize=5");

        Assert.Equal(("acme", 2, 5), factory.Tenants.LastPageRequest);
        Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
        var item = json.GetProperty("items")[0];
        Assert.Equal("Acme Srl", item.GetProperty("companyName").GetString());
        Assert.Equal("Roma", item.GetProperty("city").GetString());
    }

    [Theory]
    [InlineData("/api/tenants?page=0")]
    [InlineData("/api/tenants?pageSize=101")]
    [InlineData("/api/geography/cities?term=r")]
    public async Task InvalidQuery_Returns400(string url)
    {
        using var factory = new BackendFactory();

        var response = await factory.CreateAdministratorClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Tenant_ReturnsAddressAndStructure()
    {
        using var factory = new BackendFactory();
        factory.Tenants.Tenants.Add(Acme);
        var profileId = Guid.NewGuid().ToString();
        factory.Tenants.Structures[Acme.Id.ToString()] = new TenantStructureReadModel(
            [new TenantProfileReadModel(profileId, "Operatore", null)],
            [new TenantProductiveUnitReadModel(Guid.NewGuid(), "Stabilimento", null, [profileId])]);

        var json = await GetJsonAsync(factory, $"/api/tenants/{Acme.Id}");

        Assert.Equal("Via Roma 1", json.GetProperty("address").GetProperty("street").GetString());
        Assert.Equal("Operatore", json.GetProperty("profiles")[0].GetProperty("name").GetString());
        Assert.Equal(profileId, json.GetProperty("productiveUnits")[0].GetProperty("profileIds")[0].GetString());
    }

    [Theory]
    [InlineData("/api/tenants/{0}")]
    public async Task UnknownIds_Return404(string url)
    {
        using var factory = new BackendFactory();

        var response = await factory.CreateAdministratorClient().GetAsync(string.Format(url, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Profiles_ReturnsAllProfilesWithTenant()
    {
        using var factory = new BackendFactory();
        factory.Profiles.Profiles.Add(new ProfileReadModel("p1", Acme.Id.ToString(), "Operatore", null));

        var json = await GetJsonAsync(factory, "/api/profiles");

        Assert.Equal(Acme.Id.ToString(), json[0].GetProperty("tenantId").GetString());
    }

    [Fact]
    public async Task Cities_ReturnsMatchesOr503WhenSourceIsDown()
    {
        using var factory = new BackendFactory();
        factory.Cities.Result = [new ItalianCity("Roma", "RM", ["00100"])];

        var json = await GetJsonAsync(factory, "/api/geography/cities?term=ro");
        factory.Cities.Result = null;
        var down = await factory.CreateAdministratorClient().GetAsync("/api/geography/cities?term=ro");

        Assert.Equal("RM", json[0].GetProperty("provinceCode").GetString());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, down.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsIdentityFromToken()
    {
        using var factory = new BackendFactory();

        var json = await GetJsonAsync(factory, "/api/me");

        Assert.Equal(BackendFactory.Subject, json.GetProperty("keycloakId").GetString());
        Assert.Equal("test.user", json.GetProperty("name").GetString());
        Assert.Equal("Administrator", json.GetProperty("roles")[0].GetString());
    }

    private static async Task<JsonElement> GetJsonAsync(BackendFactory factory, string url)
    {
        var response = await factory.CreateAdministratorClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }
}
