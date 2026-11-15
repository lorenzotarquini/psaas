using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using FinHorizon.MySync.Domain.Transactions;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC9-trasferire-denaro.md (S8 is a frontend scenario).</summary>
public sealed class UC9_TrasferireDenaro(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact]
    public async Task S1_TrasferimentoTraDueConti()
    {
        var (credem, revolut) = await GivenTwoAccountsAsync();

        var transfer = await ReadAsync<TransactionView>(await TransferAsync(500m, credem, revolut), HttpStatusCode.Created);

        Assert.Equal((TransactionKind.Transfer, "Credem", "Revolut"), (transfer.Kind, transfer.AccountName, transfer.CounterAccountName));
        Assert.Null(transfer.CategoryId);
        var dashboard = await DashboardAsync();
        Assert.Equal((7100m, 523m), ((await AccountAsync(credem.Id)).Balance, (await AccountAsync(revolut.Id)).Balance));
        Assert.Equal(7623m, dashboard.NetWorth);
    }

    [Fact]
    public async Task S2_StessoConto()
    {
        var (credem, _) = await GivenTwoAccountsAsync();

        var errors = await ErrorsAsync(await TransferAsync(10m, credem, credem));

        Assert.Equal("InvalidValue", errors["counterAccountId"]);
    }

    [Fact]
    public async Task S3_ContoDiDestinazioneDiUnAltroUtente()
    {
        var (credem, _) = await GivenTwoAccountsAsync();
        var annas = await GivenAccountAsync("Conto Anna", 0m, user: UserClient("Anna"));

        var errors = await ErrorsAsync(await TransferAsync(10m, credem, annas));

        Assert.Equal("InvalidValue", errors["counterAccountId"]);
        Assert.Equal(7600m, (await AccountAsync(credem.Id)).Balance);
    }

    [Fact]
    public async Task S4_DatiObbligatori()
    {
        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/transactions", new { kind = "Transfer" }));

        Assert.Equal(
            new Dictionary<string, string>
            {
                ["date"] = "Required",
                ["amount"] = "Required",
                ["accountId"] = "Required",
                ["counterAccountId"] = "Required"
            },
            errors);
    }

    [Fact]
    public async Task S5_NeSpesaNeRicavo()
    {
        var (credem, revolut) = await GivenTwoAccountsAsync();
        await TransferAsync(300m, credem, revolut);

        var page = await TransactionsAsync($"?accountId={revolut.Id}");
        var transfers = await TransactionsAsync("?kind=Transfer");

        Assert.Equal((1, 0m, 0m), (page.TotalCount, page.ExpenseTotal, page.IncomeTotal));
        Assert.Single(transfers.Items);
        Assert.All((await NetWorthAsync("30d")).Points, point => Assert.Equal(7623m, point.NetWorth));
    }

    [Fact]
    public async Task S6_ModificaEdEliminazione()
    {
        var (credem, revolut) = await GivenTwoAccountsAsync();
        var transfer = await ReadAsync<TransactionView>(await TransferAsync(500m, credem, revolut), HttpStatusCode.Created);

        // Direction swapped and amount changed: the postings are replaced.
        (await Mario.PutAsJsonAsync($"/api/transactions/{transfer.Id}", Body(20m, revolut, credem))).EnsureSuccessStatusCode();
        Assert.Equal((7620m, 3m), ((await AccountAsync(credem.Id)).Balance, (await AccountAsync(revolut.Id)).Balance));

        (await Mario.DeleteAsync($"/api/transactions/{transfer.Id}")).EnsureSuccessStatusCode();
        Assert.Equal((7600m, 23m), ((await AccountAsync(credem.Id)).Balance, (await AccountAsync(revolut.Id)).Balance));
    }

    [Fact]
    public async Task S7_CategoriaODebitoRifiutati()
    {
        var (credem, revolut) = await GivenTwoAccountsAsync();
        var category = await GivenCategoryAsync("Spesa");

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/transactions", new
        {
            kind = "Transfer",
            date = Today,
            amount = 10m,
            accountId = credem.Id,
            counterAccountId = revolut.Id,
            categoryId = category.Id
        }));

        Assert.Equal("InvalidValue", errors["categoryId"]);
    }

    private async Task<(AccountView Credem, AccountView Revolut)> GivenTwoAccountsAsync() =>
        (await GivenAccountAsync("Credem", 7600m), await GivenAccountAsync("Revolut", 23m, "VirtualBank"));

    private object Body(decimal amount, AccountView from, AccountView to) =>
        new { kind = "Transfer", date = Today, amount, accountId = from.Id, counterAccountId = to.Id };

    private Task<HttpResponseMessage> TransferAsync(decimal amount, AccountView from, AccountView to) =>
        Mario.PostAsJsonAsync("/api/transactions", Body(amount, from, to));
}
