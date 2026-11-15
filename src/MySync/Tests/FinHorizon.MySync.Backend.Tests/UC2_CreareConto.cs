using System.Net;
using System.Net.Http.Json;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Backend.Tests.Infrastruttura;
using FinHorizon.MySync.Domain.Accounts;
using Xunit;

namespace FinHorizon.MySync.Backend.Tests;

/// <summary>docs/mysync/use-cases/UC2-creare-conto.md</summary>
public sealed class UC2_CreareConto(PostgresFixture database) : AcceptanceTest(database)
{
    [Fact]
    public async Task S1_ContoCreato()
    {
        var response = await Mario.PostAsJsonAsync("/api/accounts", AccountBody("Conto Intesa", 2500m, description: "Stipendio e bollette", tag: "Banca"));

        var account = await ReadAsync<AccountView>(response, HttpStatusCode.Created);
        Assert.Equal(AccountKind.CurrentAccount, account.Kind);
        Assert.Equal(("Conto Intesa", "Stipendio e bollette", "Banca"), (account.Name, account.Description, account.Tag));
        Assert.Equal(Today, account.OpenedOn);
        Assert.Equal(2500m, (await AccountAsync(account.Id)).Balance);
    }

    [Fact]
    public async Task S2_SoloCampiObbligatori()
    {
        var account = await ReadAsync<AccountView>(await Mario.PostAsJsonAsync("/api/accounts", AccountBody("Contanti", 0m, "Cash")), HttpStatusCode.Created);

        Assert.Null(account.Description);
        Assert.Null(account.Tag);
    }

    [Fact]
    public async Task S3_DatiObbligatoriMancanti()
    {
        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/accounts", new { }));

        Assert.Equal(
            new Dictionary<string, string> { ["kind"] = "Required", ["name"] = "Required", ["initialBalance"] = "Required" },
            errors);
        Assert.Empty((await DashboardAsync()).Accounts);
    }

    [Fact]
    public async Task S4_NomeGiaUsato()
    {
        await GivenAccountAsync("Conto Intesa", 0m);

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/accounts", AccountBody(" conto intesa ", 0m)));

        Assert.Equal("NotUnique", errors["name"]);
    }

    [Fact]
    public async Task S5_StessoNomePerUtentiDiversi()
    {
        await GivenAccountAsync("Contanti", 0m, "Cash");

        var response = await UserClient("Anna").PostAsJsonAsync("/api/accounts", AccountBody("Contanti", 0m, "Cash"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task S6_LimiteRaggiunto()
    {
        await GivenAccountAsync("Uno", 0m);
        await GivenAccountAsync("Due", 0m);

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/accounts", AccountBody("Tre", 0m)));

        Assert.Equal("LimitReached", errors["limit"]);
        Assert.Equal(2, (await DashboardAsync()).Accounts.Count);
    }

    [Theory]
    [InlineData("kind", "Criptovalute", "InvalidValue")]
    [InlineData("name", "51", "MaxLengthExceeded")]
    [InlineData("description", "251", "MaxLengthExceeded")]
    [InlineData("tag", "31", "MaxLengthExceeded")]
    [InlineData("initialBalance", "10.005", "InvalidFormat")]
    public async Task S7_ValoriNonValidi(string field, string value, string code)
    {
        var body = new Dictionary<string, object?> { ["kind"] = "CurrentAccount", ["name"] = "Conto", ["initialBalance"] = 0m };
        body[field] = field switch
        {
            "initialBalance" => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
            "kind" => value,
            _ => new string('x', int.Parse(value, System.Globalization.CultureInfo.InvariantCulture))
        };

        var errors = await ErrorsAsync(await Mario.PostAsJsonAsync("/api/accounts", body));

        Assert.Equal(code, errors[field]);
    }

    [Fact]
    public async Task S8_SaldoInizialeNegativo()
    {
        var account = await GivenAccountAsync("Conto BPM", -300m);

        Assert.Equal(-300m, (await AccountAsync(account.Id)).Balance);
    }
}
