using System.Globalization;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Subscriptions;
using FinHorizon.MySync.Domain.Transactions;
using FinHorizon.MySync.Domain.Transactions.Rules;
using Xunit;
using static FinHorizon.MySync.Domain.Tests.Builders;

namespace FinHorizon.MySync.Domain.Tests.Subscriptions;

/// <summary>The rules of UC16: charges (R6), what is due (R7, R9), the end (R13) and the linked expenses (R10, R11).</summary>
public sealed class SubscriptionTests
{
    private static readonly TransactionPolicy Policy = new(
        typeof(ITransactionRule).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(ITransactionRule).IsAssignableFrom(type))
            .Select(type => (ITransactionRule)Activator.CreateInstance(type)!));

    private static DateOnly Date(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Subscription Netflix(string firstChargeOn = "2026-10-05", string createdOn = "2026-10-01", decimal amount = 17.99m, string currency = "EUR") =>
        Subscription.Start(Mario, new Descriptor("Netflix", null, null), Guid.NewGuid(), currency, Guid.NewGuid(), Euro(amount), Date(firstChargeOn), Date(createdOn));

    [Theory, Trait("Scenario", "UC16-S6")]
    [InlineData(2026, 2, "2026-02-28")]
    [InlineData(2026, 3, "2026-03-31")]
    [InlineData(2026, 4, "2026-04-30")]
    [InlineData(2028, 2, "2028-02-29")]
    public void Charge_FallsOnTheDayOfTheFirstOrOnTheLastOfTheMonth(int year, int month, string expected) =>
        Assert.Equal(Date(expected), Netflix("2026-01-31", "2026-01-31").ChargeIn(year, month));

    [Theory]
    [InlineData("2026-10-04", null)]
    [InlineData("2026-10-05", "2026-10-05")]
    [InlineData("2026-11-17", "2026-11-05")]
    [InlineData("2026-12-04", "2026-11-05")]
    public void LastCharge_IsTheMostRecentUpToTheDay(string day, string? expected) =>
        Assert.Equal(expected is null ? null : Date(expected), Netflix().LastChargeOnOrBefore(Date(day)));

    [Theory]
    [InlineData("2026-09-30", "2026-10-05")]
    [InlineData("2026-11-05", "2026-12-05")]
    [InlineData("2026-11-17", "2026-12-05")]
    public void NextCharge_IsTheFirstAfterTheDay(string day, string expected) =>
        Assert.Equal(Date(expected), Netflix().NextChargeAfter(Date(day)));

    [Fact]
    public void End_StopsTheCharges()
    {
        var netflix = Netflix();
        netflix.Cancel(Date("2026-12-10"));

        Assert.True(netflix.IsCancelled);
        Assert.Equal(Date("2026-12-05"), netflix.NextChargeAfter(Date("2026-11-17")));
        Assert.Null(netflix.NextChargeAfter(Date("2026-12-05")));
        Assert.Equal(Date("2026-12-05"), netflix.LastChargeOnOrBefore(Date("2027-03-01")));
        Assert.True(netflix.IsActiveOn(Date("2026-12-10")));
        Assert.False(netflix.IsActiveOn(Date("2026-12-11")));
    }

    [Fact]
    public void End_CannotPrecedeTheFirstCharge() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Netflix().Cancel(Date("2026-10-04")));

    [Fact]
    public void Resume_RemovesTheEnd()
    {
        var netflix = Netflix();
        netflix.Cancel(Date("2026-10-31"));

        netflix.Resume(Date("2026-11-17"));

        Assert.False(netflix.IsCancelled);
        Assert.Equal(Date("2026-12-05"), netflix.NextChargeAfter(Date("2026-11-17")));
        Assert.Null(netflix.DueCharge(Date("2026-11-17"), [Date("2026-10-05")]));
        Assert.Equal(Date("2026-12-05"), netflix.DueCharge(Date("2026-12-05"), [Date("2026-10-05")]));
    }

    [Fact]
    public void Due_IsTheLatestChargeWithoutAnExpenseInItsMonth()
    {
        var netflix = Netflix();
        var today = Date("2026-11-17");

        Assert.Equal(Date("2026-11-05"), netflix.DueCharge(today, [Date("2026-10-05")]));
        Assert.Null(netflix.DueCharge(today, [Date("2026-11-09")]));
        Assert.Null(netflix.DueCharge(Date("2026-10-04"), []));
    }

    [Fact, Trait("Scenario", "UC16-S7")]
    public void Due_IgnoresTheChargesBeforeTheCreation() =>
        Assert.Null(Netflix("2026-09-03", "2026-11-17").DueCharge(Date("2026-11-17"), []));

    [Fact, Trait("Scenario", "UC16-S8")]
    public void Skip_RemovesOnlyThatCharge()
    {
        var netflix = Netflix();
        netflix.Skip(Date("2026-11-05"));

        Assert.Null(netflix.DueCharge(Date("2026-11-17"), []));
        Assert.Equal(Date("2026-12-05"), netflix.DueCharge(Date("2026-12-05"), []));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.001)]
    public void Amount_IsPositiveInCents(decimal amount) =>
        Assert.Throws<ArgumentException>(() => Netflix(amount: amount));

    [Fact]
    public void Tag_IsNotPartOfASubscription() =>
        Assert.Throws<ArgumentException>(() => Subscription.Start(
            Mario, new Descriptor("Netflix", null, "Casa"), Guid.NewGuid(), "EUR", Guid.NewGuid(), Euro(10m), Today, Today));

    [Fact]
    public void Payment_ValidExpense_HasNoErrors()
    {
        var (account, category, netflix) = Records();

        Assert.Empty(Policy.Check(Context(Payment(account, category, netflix, "2026-11-05"), account, category, netflix)));
    }

    [Fact]
    public void Payment_OnAnIncome_IsInvalidValueOnTheSubscription()
    {
        var (account, _, netflix) = Records();
        var salary = Category(TransactionKind.Income, "Stipendio");
        var data = new TransactionData(TransactionKind.Income, Date("2026-11-05"), Euro(10m), account.Id, salary.Id, null, null, SubscriptionId: netflix.Id);

        Assert.Contains(new FieldError("subscriptionId", ErrorCodes.InvalidValue), Policy.Check(Context(data, account, salary, netflix)));
    }

    [Fact]
    public void Payment_ThatIsAlsoAnInstallment_IsInvalidValueOnTheSubscription()
    {
        var (account, category, netflix) = Records();
        var debt = Debt(account);
        var data = Payment(account, category, netflix, "2026-11-05") with { DebtId = debt.Id };

        Assert.Contains(
            new FieldError("subscriptionId", ErrorCodes.InvalidValue),
            Policy.Check(new TransactionContext(data, account, category, debt, Euro(500m), Date("2026-11-17"), Subscription: netflix)));
    }

    [Fact]
    public void Payment_OfAnotherUsersSubscription_IsInvalidValueOnTheSubscription()
    {
        var (account, category, netflix) = Records();

        Assert.Contains(new FieldError("subscriptionId", ErrorCodes.InvalidValue), Policy.Check(Context(Payment(account, category, netflix, "2026-11-05"), account, category, null)));
    }

    [Fact]
    public void Payment_FromAnAccountInAnotherCurrency_IsInvalidValueOnTheAccount()
    {
        var (account, category, _) = Records();
        var dollars = Netflix(currency: "USD");

        Assert.Contains(new FieldError("accountId", ErrorCodes.InvalidValue), Policy.Check(Context(Payment(account, category, dollars, "2026-11-05"), account, category, dollars)));
    }

    [Theory]
    [InlineData("2026-09-30")]
    [InlineData("2026-11-12")]
    public void Payment_OutsideTheSubscription_IsInvalidValueOnTheDate(string date)
    {
        var (account, category, netflix) = Records();
        netflix.Cancel(Date("2026-11-10"));

        Assert.Contains(new FieldError("date", ErrorCodes.InvalidValue), Policy.Check(Context(Payment(account, category, netflix, date), account, category, netflix)));
    }

    [Fact]
    public void Transaction_RefusesASubscriptionOnAnIncome()
    {
        var (account, _, netflix) = Records();
        var salary = Category(TransactionKind.Income, "Stipendio");

        Assert.Throws<ArgumentException>(() => Transaction.Record(
            Mario,
            new TransactionData(TransactionKind.Income, Today, Euro(10m), account.Id, salary.Id, null, null, SubscriptionId: netflix.Id),
            DateTimeOffset.UnixEpoch));
    }

    private static (Accounts.Account Account, Categories.Category Category, Subscription Netflix) Records() =>
        (Account(openedOn: Date("2026-01-10")), Category(name: "Abbonamenti"), Netflix());

    private static TransactionData Payment(Accounts.Account account, Categories.Category category, Subscription subscription, string date) =>
        new(TransactionKind.Expense, Date(date), Euro(17.99m), account.Id, category.Id, null, null, SubscriptionId: subscription.Id);

    private static TransactionContext Context(TransactionData data, Accounts.Account account, Categories.Category category, Subscription? subscription) =>
        new(data, account, category, null, Money.Zero, Date("2026-11-17"), Subscription: subscription);
}
