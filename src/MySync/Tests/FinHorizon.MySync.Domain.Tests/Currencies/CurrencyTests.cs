using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Currencies;
using FinHorizon.MySync.Domain.NetWorth;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Rules;
using Xunit;
using static FinHorizon.MySync.Domain.Tests.Builders;

namespace FinHorizon.MySync.Domain.Tests.Currencies;

/// <summary>UC13: currencies of accounts, conversion with the rate of the day, transfers and installments across currencies.</summary>
public sealed class CurrencyTests
{
    private static readonly DateOnly Friday = new(2026, 11, 13);
    private static readonly DateOnly Sunday = new(2026, 11, 15);

    private static readonly AmountsMustFitCurrenciesRule Rule = new();

    private static Domain.Accounts.Account Wallet(decimal balance = 0.5m) =>
        Domain.Accounts.Account.Open(Mario, AccountKind.VirtualCurrency, new Descriptor("Ledger", null, null), "BTC", Euro(balance), Today.AddDays(-30));

    private static Domain.Accounts.Account Dollars() =>
        Domain.Accounts.Account.Open(Mario, AccountKind.CurrentAccount, new Descriptor("Chase", null, null), "USD", Euro(0m), Today.AddDays(-30));

    [Fact]
    public void RateTable_CarriesTheLastRateOverTheWeekend()
    {
        var rates = new RateTable("EUR", [new DailyRate("USD", Friday, 0.9m), new DailyRate("USD", Friday.AddDays(-1), 0.8m)]);

        Assert.Equal(90m, rates.ToDisplay("USD", 100m, Sunday));
        Assert.Equal(80m, rates.ToDisplay("USD", 100m, Friday.AddDays(-1)));
        // Before the first rate: the first one known.
        Assert.Equal(80m, rates.ToDisplay("USD", 100m, Friday.AddDays(-10)));
        Assert.Equal(100m, rates.ToDisplay("EUR", 100m, Sunday));
        Assert.Throws<InvalidOperationException>(() => rates.ToDisplay("BTC", 1m, Sunday));
    }

    [Fact]
    public void Trend_ConvertsEachDayWithItsRate()
    {
        var history = new LedgerHistory([new OpeningValue(Friday, Euro(1m), "BTC"), new OpeningValue(Friday, Euro(100m))], [], []);
        var rates = new RateTable("EUR", [new DailyRate("BTC", Friday, 40_000m), new DailyRate("BTC", Friday.AddDays(1), 50_000m)]);

        var trend = NetWorthCalculator.Trend(history, NetWorthPeriod.Days30, Sunday, rates);

        Assert.Equal(40_100m, trend.Points.Single(point => point.Date == Friday).NetWorth.Amount);
        Assert.Equal(50_100m, trend.Points.Single(point => point.Date == Sunday).NetWorth.Amount);
    }

    [Theory]
    [InlineData(AccountKind.VirtualCurrency, "0.12345678", true)]
    [InlineData(AccountKind.VirtualCurrency, "0.123456789", false)]
    [InlineData(AccountKind.CurrentAccount, "1.005", false)]
    public void InitialBalance_HasTheDecimalsOfTheCurrency(AccountKind kind, string balance, bool valid)
    {
        var amount = Euro(decimal.Parse(balance, System.Globalization.CultureInfo.InvariantCulture));
        var open = () => Domain.Accounts.Account.Open(Mario, kind, new Descriptor("x", null, null), kind == AccountKind.VirtualCurrency ? "BTC" : "EUR", amount, Today);

        if (valid)
        {
            Assert.Equal("BTC", open().Currency);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => open());
        }
    }

    [Fact]
    public void Currency_IsFixedOnceThereAreMovements()
    {
        var account = Account();

        Assert.Throws<InvalidOperationException>(() =>
            account.Update(AccountKind.CurrentAccount, new Descriptor("Conto Intesa", null, null), "USD", Euro(1000m), hasMovements: true));
        account.Update(AccountKind.CurrentAccount, new Descriptor("Conto Intesa", null, null), "USD", Euro(1000m), hasMovements: false);
        Assert.Equal("USD", account.Currency);
    }

    [Fact]
    public void TransferBetweenCurrencies_NeedsTheReceivedAmount()
    {
        var euro = Account();
        var dollars = Dollars();
        var without = new TransactionData(TransactionKind.Transfer, Today, Euro(100m), euro.Id, null, null, null, dollars.Id);
        var with = without with { CounterAmount = Euro(108m) };

        Assert.Contains(new FieldError(TransactionFields.CounterAmount, ErrorCodes.Required), Rule.Check(Context(without, euro, dollars)));
        Assert.Empty(Rule.Check(Context(with, euro, dollars)));
        Assert.Equal([-100m, 108m], Transaction.Record(Mario, with, DateTimeOffset.UnixEpoch).Postings.Select(posting => posting.Amount.Amount));
    }

    [Fact]
    public void TransferInTheSameCurrency_HasNoReceivedAmount()
    {
        var from = Account();
        var to = Account(name: "Revolut");
        var data = new TransactionData(TransactionKind.Transfer, Today, Euro(100m), from.Id, null, null, null, to.Id, Euro(100m));

        Assert.Contains(new FieldError(TransactionFields.CounterAmount, ErrorCodes.InvalidValue), Rule.Check(Context(data, from, to)));
    }

    [Fact]
    public void Installment_LeavesFromAnAccountInTheDebtCurrency()
    {
        var euro = Account();
        var dollars = Dollars();
        var debt = Debt(euro);
        var category = Category();
        var data = Expense(dollars, category, 10m, debtId: debt.Id);

        var context = new TransactionContext(data, dollars, category, debt, Euro(500m), Today);

        Assert.Contains(new FieldError(TransactionFields.AccountId, ErrorCodes.InvalidValue), Rule.Check(context));
    }

    [Fact]
    public void AmountInBitcoin_CanHaveEightDecimals()
    {
        var wallet = Wallet();
        var category = Category(TransactionKind.Income, "Mining");
        var data = new TransactionData(TransactionKind.Income, Today, Euro(0.00000001m), wallet.Id, category.Id, null, null);

        Assert.Empty(Rule.Check(new TransactionContext(data, wallet, category, null, Money.Zero, Today)));
        Assert.Contains(
            new FieldError(TransactionFields.Amount, ErrorCodes.InvalidFormat),
            Rule.Check(new TransactionContext(data, Account(), category, null, Money.Zero, Today)));
    }

    private static TransactionContext Context(TransactionData data, Domain.Accounts.Account account, Domain.Accounts.Account counter) =>
        new(data, account, null, null, Money.Zero, Today, counter);
}
