using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Categories;
using FinHorizon.MySync.Application.Features.Projection;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC15-spese-fisse-e-variabili.md</summary>
public sealed class UC15_SpeseFisse(PostgresFixture database) : AcceptanceTest(database)
{
    private Task<HttpResponseMessage> SetFixedAsync(Guid categoryId, bool isFixed, HttpClient? user = null) =>
        (user ?? Mario).PutAsJsonAsync($"/api/categories/{categoryId}/fixed", new { @fixed = isFixed });

    [Fact, Trait("Scenario", "UC15-S1")]
    public async Task S1_UnaCategoriaDiventaFissa()
    {
        TodayIs(new DateOnly(2026, 1, 10));
        var intesa = await GivenAccountAsync("Conto Intesa", 10_000m);
        var affitto = await GivenCategoryAsync("Affitto");
        var spesa = await GivenCategoryAsync("Spesa alimentare");
        var svago = await GivenCategoryAsync("Svago");
        TodayIs(new DateOnly(2026, 11, 17));
        for (var month = 5; month <= 10; month++)
        {
            var day = new DateOnly(2026, month, 10);
            await GivenExpenseAsync(700m, intesa, affitto, date: day);
            await GivenExpenseAsync(400m, intesa, spesa, date: day);
            await GivenExpenseAsync(200m, intesa, svago, date: day);
        }

        var before = (await GetOkAsync<ProjectionView>("/api/projection")).Suggested;
        Assert.Equal((0m, 1300m), (before.FixedExpenses, before.VariableExpenses));
        var balance = (await AccountAsync(intesa.Id)).Balance;

        var category = await ReadAsync<CategoryView>(await SetFixedAsync(affitto.Id, true), HttpStatusCode.OK);

        Assert.True(category.Fixed);
        var after = (await GetOkAsync<ProjectionView>("/api/projection")).Suggested;
        Assert.Equal((700m, 600m), (after.FixedExpenses, after.VariableExpenses));
        Assert.Equal(balance, (await AccountAsync(intesa.Id)).Balance);
    }

    [Fact, Trait("Scenario", "UC15-S2")]
    public async Task S2_UnaNuovaCategoriaEVariabile() =>
        Assert.False((await GivenCategoryAsync("Palestra")).Fixed);

    [Fact, Trait("Scenario", "UC15-S3")]
    public async Task S3_UnRicavoNonEFisso()
    {
        var stipendio = await GivenCategoryAsync("Stipendio", "Income");

        var errors = await ErrorsAsync(await SetFixedAsync(stipendio.Id, true));

        Assert.Equal("InvalidValue", errors["fixed"]);
    }

    [Fact, Trait("Scenario", "UC15-S5")]
    public async Task S5_LeSpeseDegliAbbonamentiNonContano()
    {
        TodayIs(new DateOnly(2026, 1, 10));
        var intesa = await GivenAccountAsync("Conto Intesa", 10_000m);
        var affitto = await GivenCategoryAsync("Affitto");
        await SetFixedAsync(affitto.Id, true);
        var spesa = await GivenCategoryAsync("Spesa alimentare");
        var svago = await GivenCategoryAsync("Svago");
        var netflix = await ReadAsync<Application.Features.Subscriptions.SubscriptionView>(
            await Mario.PostAsJsonAsync("/api/subscriptions", new { name = "Netflix", accountId = intesa.Id, categoryId = svago.Id, amount = 20m, firstChargeOn = new DateOnly(2026, 5, 5) }),
            HttpStatusCode.Created);
        TodayIs(new DateOnly(2026, 11, 17));
        for (var month = 5; month <= 10; month++)
        {
            var day = new DateOnly(2026, month, 10);
            await GivenExpenseAsync(700m, intesa, affitto, date: day);
            await GivenExpenseAsync(400m, intesa, spesa, date: day);
            await GivenExpenseAsync(200m, intesa, svago, date: day);
            await GivenExpenseAsync(30m, intesa, svago, date: day);
            await ReadAsync<object>(
                await RecordAsync(new { kind = "Expense", date = new DateOnly(2026, month, 5), amount = 20m, accountId = intesa.Id, categoryId = svago.Id, subscriptionId = netflix.Id }),
                HttpStatusCode.Created);
        }

        var before = (await GetOkAsync<ProjectionView>("/api/projection")).Suggested;
        Assert.Equal((700m, 630m), (before.FixedExpenses, before.VariableExpenses));

        await ReadAsync<CategoryView>(await SetFixedAsync(svago.Id, true), HttpStatusCode.OK);

        var after = (await GetOkAsync<ProjectionView>("/api/projection")).Suggested;
        Assert.Equal((930m, 400m), (after.FixedExpenses, after.VariableExpenses));
        Assert.Equal(new ProjectionSubscriptionParameterView(netflix.Id, 20m, true), Assert.Single(after.Subscriptions));
    }

    [Fact, Trait("Scenario", "UC15-S4")]
    public async Task S4_CategoriaDiUnAltroUtente()
    {
        var anna = UserClient("Anna");
        var annas = await GivenCategoryAsync("Affitto", user: anna);

        Assert.Equal(HttpStatusCode.NotFound, (await SetFixedAsync(annas.Id, true)).StatusCode);
        var stillVariable = await GetOkAsync<List<CategoryView>>("/api/categories?kind=Expense&term=Affitto", anna);
        Assert.False(Assert.Single(stillVariable).Fixed);
    }
}
