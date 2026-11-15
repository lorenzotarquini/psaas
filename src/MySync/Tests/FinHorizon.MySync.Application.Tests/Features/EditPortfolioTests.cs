using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Accounts;
using FinHorizon.MySync.Application.Features.Debts;
using FinHorizon.MySync.Domain.Common;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC10 at the application level.</summary>
public sealed class EditPortfolioTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    [Fact]
    public async Task Account_KeepsItsOwnNameAndRecomputesTheBalance()
    {
        var account = _app.GivenAccount("Credem", 1000m);
        _app.GivenExpense(account, _app.GivenCategory(), 100m);

        var result = await _app.SendAsync<UpdateAccountCommand, AccountView>(new(account.Id, "CurrentAccount", " credem ", null, "Banca", 2000m));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal((2000m, 1900m), (result.Value!.InitialBalance, result.Value.Balance));
    }

    [Fact]
    public async Task Account_CannotTakeTheNameOfAnother()
    {
        var credem = _app.GivenAccount("Credem");
        _app.GivenAccount("Revolut");

        var result = await _app.SendAsync<UpdateAccountCommand, AccountView>(new(credem.Id, "CurrentAccount", "Revolut", null, null, 0m));

        Assert.Equal([new FieldError("name", ErrorCodes.NotUnique)], result.Errors);
    }

    [Fact]
    public async Task Account_OfAnotherUserIsNotFound()
    {
        var annas = _app.GivenAccount("Conto Anna", owner: FakeStore.Anna);

        var result = await _app.SendAsync<UpdateAccountCommand, AccountView>(new(annas.Id, "Cash", "Mio", null, null, 0m));

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData("99.99", false)]
    [InlineData("100", true)]
    public async Task Debt_ResidualCannotGoBelowThePaidInstallments(string residual, bool accepted)
    {
        var account = _app.GivenAccount();
        var debt = _app.GivenDebt(account, residual: 300m);
        _app.GivenExpense(account, _app.GivenCategory(), 100m, debt);

        var result = await _app.SendAsync<UpdateDebtCommand, DebtView>(
            new(debt.Id, "Loan", "Auto", null, null, account.Id, decimal.Parse(residual, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(accepted, result.Succeeded);
        if (accepted)
        {
            Assert.True(result.Value!.PaidOff);
        }
        else
        {
            Assert.Equal([new FieldError("residual", ErrorCodes.InvalidValue)], result.Errors);
        }
    }

    [Fact]
    public async Task Debt_NeedsAnAccountOfTheUser()
    {
        var debt = _app.GivenDebt(_app.GivenAccount());
        var annas = _app.GivenAccount("Conto Anna", owner: FakeStore.Anna);

        var result = await _app.SendAsync<UpdateDebtCommand, DebtView>(new(debt.Id, "Loan", "Auto", null, null, annas.Id, 300m));

        Assert.Equal([new FieldError("accountId", ErrorCodes.InvalidValue)], result.Errors);
    }

    public void Dispose() => _app.Dispose();
}
