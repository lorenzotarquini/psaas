using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Categories;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC6-creare-categoria.md (S5 is a frontend scenario).</summary>
public sealed class UC6_CreareCategoria(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact]
    public async Task S1_CategoriaCreata()
    {
        var response = await Mario.PostAsJsonAsync("/api/categories", new { kind = "Expense", name = "Palestra" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(["Palestra"], (await SearchAsync("Expense", "pal")).Select(c => c.Name));
        Assert.Empty(await SearchAsync("Income", "pal"));
    }

    [Fact]
    public async Task S2_CategoriaGiaEsistente()
    {
        var existing = await GivenCategoryAsync("Palestra");

        var response = await Mario.PostAsJsonAsync("/api/categories", new { kind = "Expense", name = " palestra " });

        Assert.Equal(existing.Id, (await ReadAsync<CategoryView>(response, HttpStatusCode.OK)).Id);
        Assert.Single(await SearchAsync("Expense", null));
    }

    [Fact]
    public async Task S3_StessoNomeTipiDiversi()
    {
        await GivenCategoryAsync("Regali");

        var response = await Mario.PostAsJsonAsync("/api/categories", new { kind = "Income", name = "Regali" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Required")]
    [InlineData("41", "MaxLengthExceeded")]
    public async Task S4_NomeNonValido(string name, string code)
    {
        var value = name.Length == 0 ? name : new string('x', int.Parse(name, System.Globalization.CultureInfo.InvariantCulture));

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/categories", new { kind = "Expense", name = value }));

        Assert.Equal(code, errors["name"]);
    }

    [Fact]
    public async Task S6_CreazioniConcorrentiUnaSolaCategoria()
    {
        var attempts = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Mario.PostAsJsonAsync("/api/categories", new { kind = "Expense", name = "Palestra" })));

        Assert.All(attempts, response => Assert.True(response.IsSuccessStatusCode, response.StatusCode.ToString()));
        Assert.Single(await SearchAsync("Expense", "palestra"));
    }

    private Task<List<CategoryView>> SearchAsync(string kind, string? term) =>
        GetOkAsync<List<CategoryView>>($"/api/categories?kind={kind}&term={term}");
}
