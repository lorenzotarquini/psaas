using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC22-viste-salvate.md</summary>
public sealed class UC22_VisteSalvate(PostgresFixture database) : AcceptanceTest(database)
{
    private Task<HttpResponseMessage> SaveAsync(HttpClient client, string name, string query = "planId=p1&province=MI") =>
        client.PostAsJsonAsync("/api/saved-views", new { area = "tenants", name, query });

    private HttpClient OtherAdmin() =>
        ClientWith(KeycloakTokens.Create(["Administrator"], subject: "kc-other-admin", userName: "altro.admin"));

    [Fact, Trait("Scenario", "UC22-S1")]
    public async Task S1_SalvareERitrovare()
    {
        await SaveAsync(Admin, "Pro a Milano");

        var view = Assert.Single(await GetOkAsync<List<SavedViewView>>("/api/saved-views?area=tenants"));

        Assert.Equal(("Pro a Milano", "planId=p1&province=MI"), (view.Name, view.Query));
    }

    [Fact, Trait("Scenario", "UC22-S2")]
    public async Task S2_NomeGiaUsato()
    {
        await SaveAsync(Admin, "Pro a Milano");

        var errors = await ErrorsAsync(await SaveAsync(Admin, "pro a milano"));

        Assert.Equal("NotUnique", errors["name"]);
    }

    [Fact, Trait("Scenario", "UC22-S3")]
    public async Task S3_VistePersonali()
    {
        var theirs = await ReadAsync<SavedViewView>(await SaveAsync(OtherAdmin(), "Sue"), HttpStatusCode.Created);

        var mine = await GetOkAsync<List<SavedViewView>>("/api/saved-views?area=tenants");
        var delete = await Admin.DeleteAsync($"/api/saved-views/{theirs.Id}");

        Assert.Empty(mine);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact, Trait("Scenario", "UC22-S4")]
    public async Task S4_Eliminare()
    {
        var view = await ReadAsync<SavedViewView>(await SaveAsync(Admin, "Pro a Milano"), HttpStatusCode.Created);

        var delete = await Admin.DeleteAsync($"/api/saved-views/{view.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(await GetOkAsync<List<SavedViewView>>("/api/saved-views?area=tenants"));
    }
}
