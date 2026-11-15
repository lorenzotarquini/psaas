using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC8-gestire-movimenti.md (S8 is a frontend scenario).</summary>
public sealed class UC8_GestireMovimenti(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact]
    public async Task S1_FiltroPerContoEPeriodo()
    {
        TodayIs(new DateOnly(2026, 11, 1));
        var intesa = await GivenAccountAsync("Conto Intesa", 0m);
        var cash = await GivenAccountAsync("Contanti", 0m, "Cash");
        TodayIs(new DateOnly(2026, 11, 15));
        await GivenIncomeAsync(1800m, intesa, await GivenCategoryAsync("Stipendio", "Income"), new DateOnly(2026, 11, 2));
        var food = await GivenExpenseAsync(85.40m, intesa, await GivenCategoryAsync("Spesa alimentare"), date: new DateOnly(2026, 11, 5));
        await GivenExpenseAsync(20m, cash, await GivenCategoryAsync("Bar"), date: new DateOnly(2026, 11, 6));
        var bills = await GivenExpenseAsync(150m, intesa, await GivenCategoryAsync("Bollette"), date: new DateOnly(2026, 11, 10));

        var page = await TransactionsAsync($"?accountId={intesa.Id}&from=2026-11-03&to=2026-11-15");

        Assert.Equal([bills.Id, food.Id], page.Items.Select(t => t.Id));
        Assert.Equal((235.40m, 0m, 2), (page.ExpenseTotal, page.IncomeTotal, page.TotalCount));
    }

    [Fact]
    public async Task S2_ModificaImporto()
    {
        var account = await GivenAccountAsync("Conto Intesa", 2500m);
        var category = await GivenCategoryAsync("Spesa alimentare");
        var expense = await GivenExpenseAsync(85.40m, account, category);

        var response = await AmendAsync(expense.Id, MovementBody("Expense", 58.40m, account, category, Today));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2441.60m, (await AccountAsync(account.Id)).Balance);
        Assert.Equal(expense.RecordedAt, (await GetOkAsync<TransactionView>($"/api/transactions/{expense.Id}")).RecordedAt);
    }

    [Fact]
    public async Task S3_CambioConto()
    {
        var intesa = await GivenAccountAsync("Conto Intesa", 1000m);
        var cash = await GivenAccountAsync("Contanti", 100m, "Cash");
        var category = await GivenCategoryAsync("Bar");
        var expense = await GivenExpenseAsync(20m, intesa, category);

        (await AmendAsync(expense.Id, MovementBody("Expense", 20m, cash, category, Today))).EnsureSuccessStatusCode();

        Assert.Equal((1000m, 80m), ((await AccountAsync(intesa.Id)).Balance, (await AccountAsync(cash.Id)).Balance));
    }

    [Fact]
    public async Task S4_EliminaRataRiattivaIlDebito()
    {
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        var car = await GivenDebtAsync("Auto", account, 300m);
        var installment = await GivenExpenseAsync(300m, account, await GivenCategoryAsync("Rate"), car);

        var response = await Mario.DeleteAsync($"/api/transactions/{installment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var debt = await DebtAsync(car.Id);
        Assert.Equal((300m, false), (debt.Residual, debt.PaidOff));
        Assert.Equal(1000m, (await AccountAsync(account.Id)).Balance);
        Assert.Equal(0, (await TransactionsAsync()).TotalCount);
    }

    [Theory]
    [InlineData("250.00", true)]
    [InlineData("300.01", false)]
    public async Task S5_RataModificataEResiduo(string amount, bool accepted)
    {
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        var car = await GivenDebtAsync("Auto", account, 300m);
        var category = await GivenCategoryAsync("Rate");
        var installment = await GivenExpenseAsync(300m, account, category, car);

        var response = await AmendAsync(installment.Id, MovementBody("Expense", decimal.Parse(amount, CultureInfo.InvariantCulture), account, category, Today, car));

        if (accepted)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var debt = await DebtAsync(car.Id);
            Assert.Equal((50m, false), (debt.Residual, debt.PaidOff));
        }
        else
        {
            Assert.Equal("InvalidValue", (await ErrorsAsync(response))["amount"]);
            Assert.True((await DebtAsync(car.Id)).PaidOff);
        }
    }

    [Theory]
    [InlineData("amount", "0")]
    [InlineData("date", "tomorrow")]
    [InlineData("date", "2026-10-31")]
    [InlineData("categoryId", "income")]
    public async Task S6_ModificaNonValida(string field, string value)
    {
        TodayIs(new DateOnly(2026, 11, 1));
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        TodayIs(new DateOnly(2026, 11, 15));
        var category = await GivenCategoryAsync("Spesa");
        var expense = await GivenExpenseAsync(10m, account, category);
        var body = new Dictionary<string, object?>
        {
            ["kind"] = "Expense",
            ["date"] = Today,
            ["amount"] = 10m,
            ["accountId"] = account.Id,
            ["categoryId"] = category.Id
        };
        body[field] = (field, value) switch
        {
            ("amount", _) => decimal.Parse(value, CultureInfo.InvariantCulture),
            ("date", "tomorrow") => Today.AddDays(1),
            ("date", _) => DateOnly.Parse(value, CultureInfo.InvariantCulture),
            _ => (await GivenCategoryAsync("Stipendio", "Income")).Id
        };

        var errors = await ErrorsAsync(await Mario.PutAsJsonAsync($"/api/transactions/{expense.Id}", body));

        Assert.Equal("InvalidValue", errors[field]);
        Assert.Equal(10m, (await GetOkAsync<TransactionView>($"/api/transactions/{expense.Id}")).Amount);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task S7_MovimentoDiUnAltroUtente(string method)
    {
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        var category = await GivenCategoryAsync("Spesa");
        var expense = await GivenExpenseAsync(10m, account, category);
        var request = new HttpRequestMessage(new HttpMethod(method), $"/api/transactions/{expense.Id}")
        {
            Content = method == "PUT" ? JsonContent.Create(MovementBody("Expense", 1m, account, category, Today)) : null
        };

        var response = await UserClient("Anna").SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(10m, (await GetOkAsync<TransactionView>($"/api/transactions/{expense.Id}")).Amount);
    }

    [Fact]
    public async Task S9_PeriodoAlRovescio()
    {
        var errors = await ErrorsAsync(await Mario.GetAsync("/api/transactions?from=2026-11-10&to=2026-11-01"));

        Assert.Equal("InvalidValue", errors["period"]);
    }

    [Fact]
    public async Task S10_Paginazione()
    {
        TodayIs(new DateOnly(2026, 11, 1));
        var account = await GivenAccountAsync("Conto Intesa", 0m);
        var category = await GivenCategoryAsync("Spesa");
        TodayIs(new DateOnly(2026, 11, 15));
        for (var day = 1; day <= 5; day++)
        {
            await GivenExpenseAsync(day, account, category, date: new DateOnly(2026, 11, day));
        }

        var second = await TransactionsAsync("?page=2&pageSize=2");

        Assert.Equal([3m, 2m], second.Items.Select(t => t.Amount));
        Assert.Equal((5, 15m), (second.TotalCount, second.ExpenseTotal));
    }

    private Task<HttpResponseMessage> AmendAsync(Guid id, object body) => Mario.PutAsJsonAsync($"/api/transactions/{id}", body);
}
