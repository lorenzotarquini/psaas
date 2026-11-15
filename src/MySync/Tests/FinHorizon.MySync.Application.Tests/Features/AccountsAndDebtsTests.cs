using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Application.Features.Portfolio;
using FinHorizon.MySync.Domain.Common;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC2 and UC3 at the application level.</summary>
public sealed class AccountsAndDebtsTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    [Fact]
    public async Task CreateAccount_OpensTodayAndSaves()
    {
        var result = await _app.SendAsync<CreateAccountCommand, AccountView>(new("CurrentAccount", " Conto Intesa ", "Stipendio", "Banca", 2500m));

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal("Conto Intesa", result.Value!.Name);
        Assert.Equal(_app.Store.Today, result.Value.OpenedOn);
        Assert.Equal(2500m, result.Value.Balance);
        Assert.Equal(1, _app.Store.Saves);
    }

    [Fact]
    public async Task CreateAccount_InvalidInputNeverReachesTheStore()
    {
        var result = await _app.SendAsync<CreateAccountCommand, AccountView>(new(null, null, null, null, null));

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(["kind", "name", "initialBalance"], result.Errors.Select(error => error.Field));
        Assert.All(result.Errors, error => Assert.Equal(ErrorCodes.Required, error.Code));
        Assert.Empty(_app.Store.Accounts);
    }

    [Fact]
    public async Task CreateAccount_StopsAtTheLimit()
    {
        _app.Store.Limits = _app.Store.Limits with { MaxAccounts = 1 };
        _app.GivenAccount();

        var result = await _app.SendAsync<CreateAccountCommand, AccountView>(new("Cash", "Contanti", null, null, 0m));

        Assert.Equal([new FieldError("limit", ErrorCodes.LimitReached)], result.Errors);
    }

    [Fact]
    public async Task CreateAccount_NameIsUniquePerUserOnly()
    {
        _app.GivenAccount("Contanti", owner: FakeStore.Anna);
        _app.GivenAccount("Conto Intesa");

        var duplicate = await _app.SendAsync<CreateAccountCommand, AccountView>(new("Cash", " conto INTESA ", null, null, 0m));
        var sameAsAnna = await _app.SendAsync<CreateAccountCommand, AccountView>(new("Cash", "Contanti", null, null, 0m));

        Assert.Equal([new FieldError("name", ErrorCodes.NotUnique)], duplicate.Errors);
        Assert.Equal(ResultStatus.Created, sameAsAnna.Status);
    }

    [Fact]
    public async Task CreateAccount_LosingTheRaceIsNotUnique()
    {
        _app.Store.FailNextSaveWithDuplicate = true;

        var result = await _app.SendAsync<CreateAccountCommand, AccountView>(new("Cash", "Contanti", null, null, 0m));

        Assert.Equal([new FieldError("name", ErrorCodes.NotUnique)], result.Errors);
    }

    [Fact]
    public async Task CreateDebt_NeedsAnAccountOfTheUser()
    {
        var annasAccount = _app.GivenAccount(owner: FakeStore.Anna);

        var result = await _app.SendAsync<CreateDebtCommand, DebtView>(new("Loan", "Auto", null, null, annasAccount.Id, 300m));

        Assert.Equal([new FieldError("accountId", ErrorCodes.InvalidValue)], result.Errors);
    }

    [Fact]
    public async Task CreateDebt_ZeroLimitTurnsDebtsOff()
    {
        _app.Store.Limits = _app.Store.Limits with { MaxDebts = 0 };
        var account = _app.GivenAccount();

        var result = await _app.SendAsync<CreateDebtCommand, DebtView>(new("Loan", "Auto", null, null, account.Id, 300m));

        Assert.Equal([new FieldError("limit", ErrorCodes.LimitReached)], result.Errors);
    }

    [Fact]
    public async Task CreateDebt_IsCreatedOpen()
    {
        var account = _app.GivenAccount();

        var result = await _app.SendAsync<CreateDebtCommand, DebtView>(new("Mortgage", "Mutuo casa", "Tasso fisso", "Casa", account.Id, 120_000m));

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal(120_000m, result.Value!.Residual);
        Assert.False(result.Value.PaidOff);
    }

    [Fact]
    public async Task Lists_ShowBalancesResidualsAndLimits()
    {
        _app.Store.Limits = new(5, 3);
        var account = _app.GivenAccount(balance: 1000m);
        var debt = _app.GivenDebt(account, residual: 300m);
        _app.GivenExpense(account, _app.GivenCategory(), 100m, debt);

        var accounts = (await _app.SendAsync<ListAccountsQuery, AccountList>(new())).Value!;
        var debts = (await _app.SendAsync<ListDebtsQuery, DebtList>(new())).Value!;

        Assert.Equal(900m, Assert.Single(accounts.Items).Balance);
        Assert.Equal(5, accounts.Max);
        Assert.Equal(200m, Assert.Single(debts.Items).Residual);
        Assert.Equal(3, debts.Max);
    }

    [Fact]
    public async Task Tags_AreDistinctAndSorted()
    {
        await _app.SendAsync<CreateAccountCommand, AccountView>(new("Cash", "A", null, "Casa", 0m));
        await _app.SendAsync<CreateAccountCommand, AccountView>(new("Cash", "B", null, "banca", 0m));
        _app.Store.Limits = new(5, 5);
        await _app.SendAsync<CreateAccountCommand, AccountView>(new("Cash", "C", null, "casa", 0m));

        var tags = (await _app.SendAsync<ListTagsQuery, IReadOnlyList<string>>(new())).Value!;

        Assert.Equal(["banca", "Casa"], tags);
    }

    public void Dispose() => _app.Dispose();
}
