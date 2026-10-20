using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PSAAS.Application.Abstractions.Geography;
using PSAAS.Application.Abstractions.Tenants;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

public sealed class TenantCreateTests
{
    private static readonly ItalianCity Roma = new("Roma", "RM", ["00100", "00118"]);

    private static object ValidTenant(object? profiles = null, object? productiveUnits = null) => new
    {
        companyName = "Acme Srl",
        completeVatNumber = "it12345678901",
        certifiedEmail = "acme@pec.it",
        street = "Via Roma 1",
        city = "roma",
        postalCode = "00118",
        profiles,
        productiveUnits
    };

    [Fact]
    public async Task Create_WithoutStructure_Returns201WithDetail()
    {
        using var factory = NewFactory();

        var response = await factory.CreateAdministratorClient().PostAsJsonAsync("/api/tenants", ValidTenant());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal($"/api/tenants/{json.GetProperty("id").GetGuid()}", response.Headers.Location!.AbsolutePath);
        Assert.Equal("12345678901", json.GetProperty("vatNumber").GetString());
        Assert.Equal("IT12345678901", json.GetProperty("completeVatNumber").GetString());
        var address = json.GetProperty("address");
        Assert.Equal("Roma", address.GetProperty("city").GetString());
        Assert.Equal("IT", address.GetProperty("country").GetString());
    }

    [Fact]
    public async Task Create_WithStructure_LinksUnitsToProfilesByKey()
    {
        using var factory = NewFactory();
        var body = ValidTenant(
            profiles: new[] { new { key = "op", name = "Operatore" }, new { key = (string?)null, name = "Ospite" } },
            productiveUnits: new[] { new { name = "Stabilimento", profileKeys = new[] { "op" } } });

        var response = await factory.CreateAdministratorClient().PostAsJsonAsync("/api/tenants", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profiles = factory.TenantWrites.LastProfiles.ToArray();
        Assert.Equal(["Operatore", "Ospite"], profiles.Select(profile => profile.Name));
        Assert.NotEqual(profiles[0].TemporaryId, profiles[1].TemporaryId);
        var unit = Assert.Single(factory.TenantWrites.LastProductiveUnits);
        Assert.Equal([profiles[0].TemporaryId], unit.TemporaryProfileIds);
    }

    [Fact]
    public async Task Create_EmptyBody_ReportsEveryRequiredField()
    {
        using var factory = NewFactory();

        var errors = await PostInvalidAsync(factory, new { });

        foreach (var field in new[] { "companyName", "completeVatNumber", "certifiedEmail", "street", "city", "postalCode" })
        {
            Assert.Equal("Required", errors[field]);
        }
    }

    [Fact]
    public async Task Create_DuplicateTenantValues_ReportsNotUniquePerField()
    {
        using var factory = NewFactory();
        factory.Uniqueness.TakenValues.UnionWith(["acmesrl", "IT12345678901", "ACME@PEC.IT"]);

        var errors = await PostInvalidAsync(factory, ValidTenant());

        Assert.Equal("NotUnique", errors["companyName"]);
        Assert.Equal("NotUnique", errors["completeVatNumber"]);
        Assert.Equal("NotUnique", errors["certifiedEmail"]);
    }

    [Fact]
    public async Task Create_ConcurrentDuplicate_ReportsNotUniqueInsteadOfFailing()
    {
        using var factory = NewFactory();
        factory.TenantWrites.BeforeSave = () =>
        {
            factory.Uniqueness.TakenValues.Add("IT12345678901");
            throw new TenantUniquenessConflictException("race");
        };

        var errors = await PostInvalidAsync(factory, ValidTenant());

        Assert.Equal("NotUnique", Assert.Single(errors).Value);
    }

    [Theory]
    [InlineData("Milano", "00118", "city")]
    [InlineData("Roma", "20100", "postalCode")]
    public async Task Create_AddressNotInCityList_ReportsInvalidValue(string city, string postalCode, string field)
    {
        using var factory = NewFactory();

        var errors = await PostInvalidAsync(factory, new
        {
            companyName = "Acme Srl", completeVatNumber = "IT12345678901", certifiedEmail = "acme@pec.it",
            street = "Via Roma 1", city, postalCode
        });

        Assert.Equal("InvalidValue", Assert.Single(errors).Value);
        Assert.True(errors.ContainsKey(field));
    }

    [Fact]
    public async Task Create_CityListUnreachable_AcceptsAddressAsTyped()
    {
        using var factory = NewFactory();
        factory.Cities.Result = null;

        var response = await factory.CreateAdministratorClient().PostAsJsonAsync("/api/tenants", ValidTenant());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidStructure_ReportsIndexedErrors()
    {
        using var factory = NewFactory();
        var body = ValidTenant(
            profiles: new[] { new { key = "a", name = "Operatore" }, new { key = "a", name = "operatore " } },
            productiveUnits: new[] { new { name = " ", profileKeys = new[] { "missing" } } });

        var errors = await PostInvalidAsync(factory, body);

        Assert.Equal("NotUnique", errors["profiles[1].name"]);
        Assert.Equal("NotUnique", errors["profiles[1].key"]);
        Assert.Equal("Required", errors["productiveUnits[0].name"]);
        Assert.Equal("InvalidValue", errors["productiveUnits[0].profileKeys[0]"]);
    }

    private static BackendFactory NewFactory()
    {
        var factory = new BackendFactory();
        factory.Cities.Result = [Roma];
        return factory;
    }

    private static async Task<Dictionary<string, string>> PostInvalidAsync(BackendFactory factory, object body)
    {
        var response = await factory.CreateAdministratorClient().PostAsJsonAsync("/api/tenants", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        return errors.EnumerateObject().ToDictionary(error => error.Name, error => error.Value[0].GetString()!);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
}
