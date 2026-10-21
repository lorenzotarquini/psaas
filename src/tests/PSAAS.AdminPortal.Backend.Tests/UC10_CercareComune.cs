using System.Net;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using PSAAS.Application.Abstractions.Geography;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>
/// docs/use-cases/UC10-cercare-comune.md. Ordering and the 20-town limit belong to the real city
/// provider and are covered by the Infrastructure unit tests.
/// </summary>
public sealed class UC10_CercareComune(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact, Trait("Scenario", "UC10-S1")]
    public async Task S1_ComuniConProvinciaECap()
    {
        CityList.Cities.Clear();
        CityList.Cities.Add(new ItalianCity("Roma", "RM", ["00118"]));

        var city = Assert.Single(await GetOkAsync<List<CityView>>("/api/geography/cities?term=ro"));

        Assert.Equal(("Roma", "RM"), (city.Name, city.ProvinceCode));
        Assert.Equal(["00118"], city.PostalCodes);
    }

    [Fact, Trait("Scenario", "UC10-S2")]
    public async Task S2_TestoTroppoCorto()
    {
        var response = await Admin.GetAsync("/api/geography/cities?term=r");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact, Trait("Scenario", "UC10-S3")]
    public async Task S3_ListaComuniNonRaggiungibile()
    {
        CityList.Unavailable = true;

        var response = await Admin.GetAsync("/api/geography/cities?term=ro");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
