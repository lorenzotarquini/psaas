using FinHorizon.MySync.Application.Abstractions.Currencies;
using FinHorizon.MySync.Application.Features.Dashboard;
using FinHorizon.MySync.Application.Features.Preferences;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC13 in the use cases: conversion into the display currency and its choice.</summary>
public sealed class CurrencyTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Dashboard_SumsTheConvertedBalances()
    {
        _app.GivenAccount("Credem", 1000m);
        _app.GivenAccount("Chase", 1100m, currency: "USD");
        _app.GivenAccount("Ledger", 0.5m, currency: "BTC", kind: AccountKind.VirtualCurrency);

        var dashboard = (await _app.SendAsync<GetDashboardQuery, DashboardView>(new GetDashboardQuery())).Value!;

        Assert.Equal(("EUR", 27_000m), (dashboard.Currency, dashboard.NetWorth));
        Assert.Equal(1000m, dashboard.Accounts.Single(account => account.Name == "Chase").DisplayBalance);
    }

    [Fact]
    public async Task DisplayCurrency_IsEuroUntilChosen_AndOnlyTraditional()
    {
        Assert.Equal(new PreferencesView("EUR", false), (await _app.SendAsync<GetPreferencesQuery, PreferencesView>(new GetPreferencesQuery())).Value);

        var crypto = await _app.SendAsync<UpdatePreferencesCommand, PreferencesView>(new UpdatePreferencesCommand("BTC"));
        Assert.Equal([new FieldError("displayCurrency", ErrorCodes.InvalidValue)], crypto.Errors);

        await _app.SendAsync<UpdatePreferencesCommand, PreferencesView>(new UpdatePreferencesCommand("USD"));
        Assert.Equal(new PreferencesView("USD", true), (await _app.SendAsync<GetPreferencesQuery, PreferencesView>(new GetPreferencesQuery())).Value);
    }

    [Fact]
    public async Task TransactionTotals_AreConvertedDayByDay()
    {
        var euro = _app.GivenAccount("Credem");
        var dollars = _app.GivenAccount("Chase", 0m, currency: "USD");
        var category = _app.GivenCategory();
        _app.GivenExpense(euro, category, 10m);
        _app.GivenExpense(dollars, category, 11m);

        var page = (await _app.SendAsync<ListTransactionsQuery, TransactionPage>(new ListTransactionsQuery())).Value!;

        Assert.Equal(("EUR", 20m), (page.Currency, page.ExpenseTotal));
    }

    [Fact]
    public async Task WithoutTheRateService_OnlyConversionsFail()
    {
        _app.GivenAccount("Credem");
        _app.Currencies.Unavailable = true;

        Assert.Equal(1000m, (await _app.SendAsync<GetDashboardQuery, DashboardView>(new GetDashboardQuery())).Value!.NetWorth);

        _app.GivenAccount("Chase", 10m, currency: "USD");
        await Assert.ThrowsAsync<CurrencyServiceUnavailableException>(() => _app.SendAsync<GetDashboardQuery, DashboardView>(new GetDashboardQuery()));
    }

    [Fact]
    public async Task TransferBetweenCurrencies_MovesBothAmounts()
    {
        var euro = _app.GivenAccount("Credem");
        var dollars = _app.GivenAccount("Chase", 0m, currency: "USD");

        var result = await _app.SendAsync<RecordTransactionCommand, TransactionView>(
            new RecordTransactionCommand("Transfer", _app.Store.Today, 100m, euro.Id, null, null, null, dollars.Id, 108m));

        Assert.Equal((108m, "USD"), (result.Value!.CounterAmount, result.Value.CounterCurrency));
        Assert.Equal([-100m, 108m], _app.Store.Transactions.Single().Postings.Select(posting => posting.Amount.Amount));
        Assert.Equal(TransactionKind.Transfer, result.Value.Kind);
    }
}
