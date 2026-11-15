using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC5-registrare-movimento.md</summary>
public sealed class UC5_RegistrareMovimento(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact]
    public async Task S1_Spesa()
    {
        var account = await GivenAccountAsync("Conto Intesa", 2500m);
        var category = await GivenCategoryAsync("Spesa alimentare");

        var movement = await GivenExpenseAsync(85.40m, account, category);

        Assert.Equal(2414.60m, (await AccountAsync(account.Id)).Balance);
        Assert.Equal(movement.Id, (await DashboardAsync()).RecentTransactions[0].Id);
        Assert.Equal(("Conto Intesa", "Spesa alimentare"), (movement.AccountName, movement.CategoryName));
    }

    [Fact]
    public async Task S2_Ricavo()
    {
        var account = await GivenAccountAsync("Conto Intesa", 2500m);

        await GivenIncomeAsync(1800m, account, await GivenCategoryAsync("Stipendio", "Income"));

        Assert.Equal(4300m, (await AccountAsync(account.Id)).Balance);
    }

    [Fact]
    public async Task S3_RataDiUnDebito()
    {
        var account = await GivenAccountAsync("Conto Intesa", 2500m);
        var mortgage = await GivenDebtAsync("Mutuo casa", account, 120_000m, "Mortgage");
        await GivenExpenseAsync(650m, account, await GivenCategoryAsync("Casa"), mortgage);

        var dashboard = await DashboardAsync();
        Assert.Equal(1850m, dashboard.Accounts.Single().Balance);
        Assert.Equal(119_350m, dashboard.Debts.Single().Residual);
        Assert.Equal(1850m, dashboard.NetWorth);
    }

    [Fact]
    public async Task S4_UltimaRataEstingueIlDebito()
    {
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        var car = await GivenDebtAsync("Auto", account, 300m);

        await GivenExpenseAsync(300m, account, await GivenCategoryAsync("Rate"), car);

        var debt = await DebtAsync(car.Id);
        Assert.Equal(0m, debt.Residual);
        Assert.True(debt.PaidOff);
    }

    [Fact]
    public async Task S5_RataOltreIlResiduo()
    {
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        var car = await GivenDebtAsync("Auto", account, 300m);

        var response = await RecordAsync(MovementBody("Expense", 300.01m, account, await GivenCategoryAsync("Rate"), Today, car));

        Assert.Equal("InvalidValue", (await ErrorsAsync(response))["amount"]);
        Assert.Equal(300m, (await DebtAsync(car.Id)).Residual);
    }

    [Fact]
    public async Task S6_DatiObbligatoriMancanti()
    {
        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/transactions", new { }));

        Assert.Equal(
            new Dictionary<string, string>
            {
                ["kind"] = "Required",
                ["date"] = "Required",
                ["amount"] = "Required",
                ["accountId"] = "Required",
                ["categoryId"] = "Required"
            },
            errors);
    }

    [Theory]
    [InlineData("amount", "0", "InvalidValue")]
    [InlineData("amount", "-10.00", "InvalidValue")]
    [InlineData("amount", "10.005", "InvalidFormat")]
    [InlineData("date", "tomorrow", "InvalidValue")]
    [InlineData("date", "2026-10-31", "InvalidValue")]
    [InlineData("categoryId", "income", "InvalidValue")]
    [InlineData("accountId", "anna", "InvalidValue")]
    public async Task S7_ValoriNonValidi(string field, string value, string code)
    {
        TodayIs(new DateOnly(2026, 11, 1));
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        TodayIs(new DateOnly(2026, 11, 15));
        var expense = await GivenCategoryAsync("Spesa");
        var body = new Dictionary<string, object?>
        {
            ["kind"] = "Expense",
            ["date"] = Today,
            ["amount"] = 10m,
            ["accountId"] = account.Id,
            ["categoryId"] = expense.Id
        };
        body[field] = (field, value) switch
        {
            ("amount", _) => decimal.Parse(value, CultureInfo.InvariantCulture),
            ("date", "tomorrow") => Today.AddDays(1),
            ("date", _) => DateOnly.Parse(value, CultureInfo.InvariantCulture),
            ("categoryId", _) => (await GivenCategoryAsync("Stipendio", "Income")).Id,
            _ => (await GivenAccountAsync("Conto Anna", 0m, user: UserClient("Anna"))).Id
        };

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/transactions", body));

        Assert.Equal(code, errors[field]);
    }

    [Fact]
    public async Task S8_DebitoSuUnRicavo()
    {
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        var mortgage = await GivenDebtAsync("Mutuo casa", account, 1000m, "Mortgage");

        var response = await RecordAsync(MovementBody("Income", 10m, account, await GivenCategoryAsync("Rimborso", "Income"), Today, mortgage));

        Assert.Equal("InvalidValue", (await ErrorsAsync(response))["debtId"]);
    }

    [Fact]
    public async Task S9_SaldoNegativo()
    {
        var cash = await GivenAccountAsync("Contanti", 20m, "Cash");

        await GivenExpenseAsync(35m, cash, await GivenCategoryAsync("Bar"));

        Assert.Equal(-15m, (await AccountAsync(cash.Id)).Balance);
    }

    [Fact]
    public async Task S10_RataPrimaDelDebito()
    {
        TodayIs(new DateOnly(2026, 11, 1));
        var account = await GivenAccountAsync("Conto Intesa", 1000m);
        TodayIs(new DateOnly(2026, 11, 10));
        var car = await GivenDebtAsync("Auto", account, 300m);

        var response = await RecordAsync(MovementBody("Expense", 10m, account, await GivenCategoryAsync("Rate"), new DateOnly(2026, 11, 9), car));

        Assert.Equal("InvalidValue", (await ErrorsAsync(response))["date"]);
    }

    [Fact]
    public async Task S11_NotaSalvata()
    {
        var account = await GivenAccountAsync("Conto Intesa", 1000m);

        var response = await RecordAsync(MovementBody("Expense", 10m, account, await GivenCategoryAsync("Bar"), Today, note: "  caffè "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("caffè", (await DashboardAsync()).RecentTransactions[0].Note);
    }
}
