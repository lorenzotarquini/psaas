using System.Net;
using System.Net.Http.Json;
using PSAAS.AdminPortal.Backend.Tests.Infrastruttura;
using Xunit;

namespace PSAAS.AdminPortal.Backend.Tests;

/// <summary>docs/use-cases/UC24-incassi-solleciti.md</summary>
public sealed class UC24_IncassiSolleciti(PostgresFixture database) : AcceptanceTest(database)
{
    private string Today => Host.Clock.GetUtcNow().ToString("yyyy-MM-dd");

    private Task<HttpResponseMessage> PayAsync(Guid invoiceId, string? paidAt, string? reference = null) =>
        Admin.PostAsJsonAsync($"/api/invoices/{invoiceId}/payments", new { paidAt, reference });

    [Fact, Trait("Scenario", "UC24-S1")]
    public async Task S1_RegistrarePagamento()
    {
        var (_, invoice) = await GivenInvoicedTenantAsync();

        var paid = await ReadAsync<InvoiceView>(await PayAsync(invoice.Id, Today, "CRO 123"), HttpStatusCode.OK);
        var again = await PayAsync(invoice.Id, Today);

        Assert.Equal(("Paid", "CRO 123"), (paid.Status, paid.PaymentReference));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact, Trait("Scenario", "UC24-S2")]
    public async Task S2_ScadutaESollecito()
    {
        var (_, invoice) = await GivenInvoicedTenantAsync();

        await DaysPassAsync(31);
        var first = await GetOkAsync<InvoiceView>($"/api/invoices/{invoice.Id}");
        await DaysPassAsync(7);
        var second = await GetOkAsync<InvoiceView>($"/api/invoices/{invoice.Id}");

        Assert.Equal(("Overdue", 1), (first.Status, first.RemindersSent));
        Assert.Equal(2, second.RemindersSent);
    }

    [Fact, Trait("Scenario", "UC24-S3")]
    public async Task S3_SospensionePerMorosita()
    {
        var (alfa, invoice) = await GivenInvoicedTenantAsync();

        await DaysPassAsync(61);

        var lifecycle = (await TenantAsync(alfa.Id)).Lifecycle!;
        Assert.Equal(("Suspended", $"Morosità: fattura {invoice.Number}"), (lifecycle.Status, lifecycle.Reason));
    }

    [Fact, Trait("Scenario", "UC24-S4")]
    public async Task S4_AnnullareConNotaDiCredito()
    {
        var (alfa, invoice) = await GivenInvoicedTenantAsync();

        var cancelled = await ReadAsync<InvoiceView>(
            await Admin.PostAsJsonAsync($"/api/invoices/{invoice.Id}/cancel", new { reason = "Errore di listino" }), HttpStatusCode.OK);

        var credit = (await InvoicesOfAsync(alfa.Id)).Single(i => i.Kind == "CreditNote");
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal((122m, invoice.Id), (credit.Total, credit.RelatedInvoiceId));
        Assert.Equal(credit.Id, cancelled.RelatedInvoiceId);
    }

    [Fact, Trait("Scenario", "UC24-S5")]
    public async Task S5_RiepilogoEFiltri()
    {
        var (_, betaInvoice) = await GivenInvoicedTenantAsync("Beta Snc");
        await DaysPassAsync(31);
        var (_, alfaInvoice) = await GivenInvoicedTenantAsync("Alfa Srl");
        await PayAsync(alfaInvoice.Id, Today);

        var summary = await GetOkAsync<BillingSummaryView>("/api/invoices/summary");
        var overdue = await GetOkAsync<PageView<InvoiceView>>("/api/invoices?status=Overdue");

        Assert.Equal((122m, 1, 122m), (summary.Overdue, summary.OverdueCount, summary.PaidThisMonth));
        Assert.Equal(betaInvoice.Id, Assert.Single(overdue.Items).Id);
    }

    [Theory, Trait("Scenario", "UC24-S6")]
    [InlineData(null, "Required")]
    [InlineData("2999-01-01", "InvalidValue")]
    [InlineData("ieri", "InvalidValue")]
    public async Task S6_PagamentoNonValido(string? paidAt, string code)
    {
        var (_, invoice) = await GivenInvoicedTenantAsync();

        var errors = await ErrorsAsync(await PayAsync(invoice.Id, paidAt));

        Assert.Equal(code, errors["paidAt"]);
    }
}
