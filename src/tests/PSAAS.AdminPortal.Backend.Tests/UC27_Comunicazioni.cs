using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC27-comunicazioni.md</summary>
public sealed class UC27_Comunicazioni(PostgresFixture database) : AcceptanceTest(database)
{
    private Task<HttpResponseMessage> SendAsync(object segment, params string[] channels) =>
        Admin.PostAsJsonAsync("/api/communications", new { subject = "Manutenzione sabato", body = "Sabato dalle 8 alle 12 la piattaforma sarà ferma.", channels, segment });

    private static readonly object Active = new { statuses = new[] { "Active" } };

    [Fact, Trait("Scenario", "UC27-S1")]
    public async Task S1_AnteprimaDestinatari()
    {
        var pro = await GivenPlanAsync("pro");
        await SubscribeAsync((await GivenTenantAsync("Alfa Srl")).Id, pro.Id);
        await SubscribeAsync((await GivenTenantAsync("Beta Snc")).Id, pro.Id);
        await SubscribeAsync((await GivenTenantAsync("Gamma Spa")).Id, (await GivenPlanAsync("base")).Id);
        var delta = await GivenTenantAsync("Delta Srl");
        await SubscribeAsync(delta.Id, pro.Id);
        await Admin.PostAsJsonAsync($"/api/tenants/{delta.Id}/status", new { status = "Cancelling", reason = "Disdetta" });
        await Admin.PostAsJsonAsync($"/api/tenants/{delta.Id}/status", new { status = "Ceased", reason = "Chiuso" });

        var preview = await ReadAsync<CommunicationPreviewView>(
            await Admin.PostAsJsonAsync("/api/communications/preview", new { planIds = new[] { pro.Id } }), HttpStatusCode.OK);

        Assert.Equal(2, preview.Count);
        Assert.Equal(["Alfa Srl", "Beta Snc"], preview.Sample);
    }

    [Fact, Trait("Scenario", "UC27-S2")]
    public async Task S2_InvioEmailEAnnuncio()
    {
        var alfa = await GivenTenantAsync("Alfa Srl", certifiedEmail: "alfa@pec.it");
        await GivenTenantAsync("Beta Snc", certifiedEmail: "beta@pec.it");

        var sent = await ReadAsync<CommunicationView>(await SendAsync(Active, "InApp", "Email"), HttpStatusCode.Created);

        Assert.Equal(["alfa@pec.it", "beta@pec.it"], Host.Mailbox.Sent.Select(m => m.To).Order());
        Assert.Equal((2, 2), (sent.RecipientCount, sent.EmailsSent));
        Assert.All(sent.Recipients, r => Assert.Equal("Sent", r.EmailStatus));
        Assert.Single((await GetOkAsync<PageView<AuditEventView>>($"/api/audit?tenantId={alfa.Id}&action=communication.sent")).Items);
    }

    [Fact, Trait("Scenario", "UC27-S3")]
    public async Task S3_SelezioneEsplicita()
    {
        var alfa = await GivenTenantAsync("Alfa Srl");
        await GivenTenantAsync("Beta Snc");
        var gamma = await GivenTenantAsync("Gamma Spa");

        var sent = await ReadAsync<CommunicationView>(
            await SendAsync(new { statuses = new[] { "Suspended" }, tenantIds = new[] { alfa.Id, gamma.Id } }, "InApp"), HttpStatusCode.Created);

        Assert.Equal(["Alfa Srl", "Gamma Spa"], sent.Recipients.Select(r => r.TenantName));
        Assert.Empty(Host.Mailbox.Sent);
    }

    [Fact, Trait("Scenario", "UC27-S4")]
    public async Task S4_EmailRifiutata()
    {
        await GivenTenantAsync("Alfa Srl", certifiedEmail: "alfa@pec.it");
        await GivenTenantAsync("Beta Snc", certifiedEmail: "beta@pec.it");
        Host.Mailbox.Rejecting.Add("beta@pec.it");

        var sent = await ReadAsync<CommunicationView>(await SendAsync(Active, "Email"), HttpStatusCode.Created);

        Assert.Equal("Sent", sent.Recipients.Single(r => r.TenantName == "Alfa Srl").EmailStatus);
        var beta = sent.Recipients.Single(r => r.TenantName == "Beta Snc");
        Assert.Equal("Failed", beta.EmailStatus);
        Assert.Contains("550", beta.Error);
    }

    [Fact, Trait("Scenario", "UC27-S5")]
    public async Task S5_DatiNonValidi()
    {
        var errors = await ErrorsAsync(await Admin.PostAsJsonAsync("/api/communications", new
        {
            subject = " ", body = "Testo", channels = Array.Empty<string>(), segment = new { planIds = new[] { Guid.NewGuid() } },
        }));

        Assert.Equal(("Required", "Required", "InvalidValue"), (errors["subject"], errors["channels"], errors["segment"]));
    }

    [Fact, Trait("Scenario", "UC27-S6")]
    public async Task S6_SollecitoViaEmail()
    {
        var (_, invoice) = await GivenInvoicedTenantAsync();

        await DaysPassAsync(31);

        var reminder = Assert.Single(Host.Mailbox.Sent);
        Assert.Contains(invoice.Number, reminder.Subject);
    }
}
