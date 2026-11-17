using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Projection;
using FinHorizon.MySync.Application.Features.Subscriptions;
using FinHorizon.MySync.Application.Features.Transactions;
using FinHorizon.MySync.Domain.Accounts;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC16 at the application level; the store's today is 2026-11-15.</summary>
public sealed class SubscriptionsTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    private Task<Result<SubscriptionView>> CreateAsync(CreateSubscriptionCommand command) =>
        _app.SendAsync<CreateSubscriptionCommand, SubscriptionView>(command);

    private static CreateSubscriptionCommand Netflix(Account account, Domain.Categories.Category category, decimal? amount = 17.99m, string? name = "Netflix") =>
        new(name, null, account.Id, category.Id, amount, new DateOnly(2026, 12, 5));

    [Fact]
    public async Task Create_RecordsNoMovement()
    {
        var account = _app.GivenAccount();
        var category = _app.GivenCategory("Abbonamenti");

        var result = await CreateAsync(Netflix(account, category));

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal(("Netflix", "Conto Intesa", "Abbonamenti", "EUR"), (result.Value!.Name, result.Value.AccountName, result.Value.CategoryName, result.Value.Currency));
        Assert.Equal(new DateOnly(2026, 12, 5), result.Value.NextChargeOn);
        Assert.Null(result.Value.DueOn);
        Assert.Empty(_app.Store.Transactions);
    }

    [Fact]
    public async Task Create_ChecksTheFields()
    {
        var result = await CreateAsync(new CreateSubscriptionCommand(" ", null, null, null, 10.001m, null));

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(new FieldError("name", ErrorCodes.Required), result.Errors);
        Assert.Contains(new FieldError("accountId", ErrorCodes.Required), result.Errors);
        Assert.Contains(new FieldError("categoryId", ErrorCodes.Required), result.Errors);
        Assert.Contains(new FieldError("amount", ErrorCodes.InvalidFormat), result.Errors);
        Assert.Contains(new FieldError("firstChargeOn", ErrorCodes.Required), result.Errors);
    }

    [Fact]
    public async Task Create_RefusesACryptoAccountAndAnIncomeCategory()
    {
        var wallet = _app.GivenAccount("Wallet", currency: "BTC", kind: AccountKind.VirtualCurrency);
        var salary = _app.GivenCategory("Stipendio", TransactionKind.Income);

        var result = await CreateAsync(Netflix(wallet, salary));

        Assert.Contains(new FieldError("accountId", ErrorCodes.InvalidValue), result.Errors);
        Assert.Contains(new FieldError("categoryId", ErrorCodes.InvalidValue), result.Errors);
    }

    [Fact]
    public async Task Create_RefusesRecordsOfAnotherUser()
    {
        var annas = _app.GivenAccount("Conto Anna", owner: FakeStore.Anna);
        var annasCategory = _app.GivenCategory("Abbonamenti", owner: FakeStore.Anna);

        var result = await CreateAsync(Netflix(annas, annasCategory));

        Assert.Contains(new FieldError("accountId", ErrorCodes.InvalidValue), result.Errors);
        Assert.Contains(new FieldError("categoryId", ErrorCodes.InvalidValue), result.Errors);
    }

    [Fact]
    public async Task Create_NameIsUniqueAmongCancelledToo()
    {
        var account = _app.GivenAccount();
        var category = _app.GivenCategory();
        _app.GivenSubscription(account, category).Cancel(_app.Store.Today);

        var result = await CreateAsync(Netflix(account, category, name: "NETFLIX"));

        Assert.Equal(new FieldError("name", ErrorCodes.NotUnique), Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Create_LimitCountsOnlyOpenSubscriptions()
    {
        _app.Store.Limits = new(2, 1, 1);
        var account = _app.GivenAccount();
        var category = _app.GivenCategory();
        var netflix = _app.GivenSubscription(account, category);

        Assert.Equal(new FieldError("limit", ErrorCodes.LimitReached), Assert.Single((await CreateAsync(Netflix(account, category, name: "Spotify"))).Errors));

        netflix.Cancel(_app.Store.Today.AddDays(15));
        Assert.Equal(ResultStatus.Created, (await CreateAsync(Netflix(account, category, name: "Spotify"))).Status);

        var resume = await _app.SendAsync<ResumeSubscriptionCommand, SubscriptionView>(new(netflix.Id));
        Assert.Equal(new FieldError("limit", ErrorCodes.LimitReached), Assert.Single(resume.Errors));
    }

    [Fact]
    public async Task List_OrdersOpenByNameThenCancelledAndSumsTheActive()
    {
        var account = _app.GivenAccount();
        var category = _app.GivenCategory();
        _app.GivenSubscription(account, category, "Spotify", 10.99m);
        _app.GivenSubscription(account, category, "Cloud", 3m);
        _app.GivenSubscription(account, category, "Palestra", 45m, new DateOnly(2026, 1, 3)).Cancel(new DateOnly(2026, 10, 31));
        _app.GivenSubscription(account, category, "Netflix", 17.99m, new DateOnly(2026, 1, 3)).Cancel(new DateOnly(2026, 11, 30));

        var list = (await _app.SendAsync<ListSubscriptionsQuery, SubscriptionList>(new())).Value!;

        Assert.Equal(["Cloud", "Spotify", "Netflix", "Palestra"], list.Items.Select(item => item.Name));
        Assert.Equal(31.98m, list.MonthlyTotal);
        Assert.Equal(("EUR", 10), (list.Currency, list.Max));
        Assert.False(list.Items.Single(item => item.Name == "Palestra").Active);
    }

    [Fact]
    public async Task List_ConvertsIntoTheDisplayCurrency()
    {
        var dollars = _app.GivenAccount("Revolut USD", currency: "USD");
        var category = _app.GivenCategory();
        _app.GivenSubscription(dollars, category, "Netflix", 11m);

        var list = (await _app.SendAsync<ListSubscriptionsQuery, SubscriptionList>(new())).Value!;

        Assert.Equal(10m, Assert.Single(list.Items).DisplayAmount);
        Assert.Equal(10m, list.MonthlyTotal);
    }

    [Fact]
    public async Task Cancel_CannotPrecedeTheLastPayment()
    {
        var account = _app.GivenAccount(openedDaysAgo: 300);
        var category = _app.GivenCategory();
        var netflix = _app.GivenSubscription(account, category, firstChargeOn: new DateOnly(2026, 10, 5), createdOn: new DateOnly(2026, 10, 1));
        _app.GivenExpense(account, category, 17.99m, subscription: netflix, date: new DateOnly(2026, 11, 5));

        var early = await _app.SendAsync<CancelSubscriptionCommand, SubscriptionView>(new(netflix.Id, new DateOnly(2026, 11, 4)));
        var missing = await _app.SendAsync<CancelSubscriptionCommand, SubscriptionView>(new(netflix.Id, null));
        var fine = await _app.SendAsync<CancelSubscriptionCommand, SubscriptionView>(new(netflix.Id, new DateOnly(2026, 11, 5)));

        Assert.Equal(new FieldError("endsOn", ErrorCodes.InvalidValue), Assert.Single(early.Errors));
        Assert.Equal(new FieldError("endsOn", ErrorCodes.Required), Assert.Single(missing.Errors));
        Assert.Equal(new DateOnly(2026, 11, 5), fine.Value!.EndsOn);
    }

    [Fact]
    public async Task Skip_OnlyTheChargeDueNow()
    {
        var account = _app.GivenAccount(openedDaysAgo: 300);
        var category = _app.GivenCategory();
        var netflix = _app.GivenSubscription(account, category, firstChargeOn: new DateOnly(2026, 10, 5), createdOn: new DateOnly(2026, 10, 1));

        var wrong = await _app.SendAsync<SkipSubscriptionChargeCommand, SubscriptionView>(new(netflix.Id, new DateOnly(2026, 10, 5)));
        var right = await _app.SendAsync<SkipSubscriptionChargeCommand, SubscriptionView>(new(netflix.Id, new DateOnly(2026, 11, 5)));

        Assert.Equal(new FieldError("dueOn", ErrorCodes.InvalidValue), Assert.Single(wrong.Errors));
        Assert.Null(right.Value!.DueOn);
    }

    [Fact]
    public async Task Update_WithPayments_KeepsTheCurrencyAndTheFirstCharge()
    {
        var account = _app.GivenAccount(openedDaysAgo: 300);
        var dollars = _app.GivenAccount("Revolut USD", currency: "USD");
        var category = _app.GivenCategory();
        var netflix = _app.GivenSubscription(account, category, firstChargeOn: new DateOnly(2026, 10, 5), createdOn: new DateOnly(2026, 10, 1));
        _app.GivenExpense(account, category, 17.99m, subscription: netflix, date: new DateOnly(2026, 10, 5));

        var result = await _app.SendAsync<UpdateSubscriptionCommand, SubscriptionView>(
            new(netflix.Id, "Netflix", null, dollars.Id, category.Id, 19.99m, new DateOnly(2026, 10, 6)));

        Assert.Contains(new FieldError("accountId", ErrorCodes.InvalidValue), result.Errors);
        Assert.Contains(new FieldError("firstChargeOn", ErrorCodes.InvalidValue), result.Errors);
        Assert.Equal(17.99m, netflix.Amount.Amount);
    }

    [Fact]
    public async Task Delete_OnlyWithoutPayments()
    {
        var account = _app.GivenAccount();
        var category = _app.GivenCategory();
        var spotify = _app.GivenSubscription(account, category, "Spotify");
        var netflix = _app.GivenSubscription(account, category);
        _app.GivenExpense(account, category, 17.99m, subscription: netflix);

        var deleted = await _app.SendAsync<DeleteSubscriptionCommand, Unit>(new(spotify.Id));
        var refused = await _app.SendAsync<DeleteSubscriptionCommand, Unit>(new(netflix.Id));

        Assert.Equal(ResultStatus.Ok, deleted.Status);
        Assert.Equal(new FieldError("payments", ErrorCodes.InvalidValue), Assert.Single(refused.Errors));
        Assert.Equal([netflix], _app.Store.Subscriptions);
    }

    [Fact]
    public async Task EveryCommand_OnAnotherUsersSubscription_IsNotFound()
    {
        var annas = _app.GivenSubscription(_app.GivenAccount("Conto Anna", owner: FakeStore.Anna), _app.GivenCategory(owner: FakeStore.Anna));
        var account = _app.GivenAccount();
        var category = _app.GivenCategory();

        Assert.Equal(ResultStatus.NotFound, (await _app.SendAsync<UpdateSubscriptionCommand, SubscriptionView>(new(annas.Id, "X", null, account.Id, category.Id, 1m, _app.Store.Today))).Status);
        Assert.Equal(ResultStatus.NotFound, (await _app.SendAsync<CancelSubscriptionCommand, SubscriptionView>(new(annas.Id, _app.Store.Today))).Status);
        Assert.Equal(ResultStatus.NotFound, (await _app.SendAsync<ResumeSubscriptionCommand, SubscriptionView>(new(annas.Id))).Status);
        Assert.Equal(ResultStatus.NotFound, (await _app.SendAsync<SkipSubscriptionChargeCommand, SubscriptionView>(new(annas.Id, _app.Store.Today))).Status);
        Assert.Equal(ResultStatus.NotFound, (await _app.SendAsync<DeleteSubscriptionCommand, Unit>(new(annas.Id))).Status);
    }

    [Fact]
    public async Task RecordTransaction_LinksTheExpenseAndRefusesAnotherUsersSubscription()
    {
        var account = _app.GivenAccount();
        var category = _app.GivenCategory();
        var netflix = _app.GivenSubscription(account, category, firstChargeOn: _app.Store.Today.AddDays(-10));
        var annas = _app.GivenSubscription(_app.GivenAccount("Conto Anna", owner: FakeStore.Anna), _app.GivenCategory(owner: FakeStore.Anna));

        var linked = await _app.SendAsync<RecordTransactionCommand, TransactionView>(
            new("Expense", _app.Store.Today, 17.99m, account.Id, category.Id, null, null, SubscriptionId: netflix.Id));
        var refused = await _app.SendAsync<RecordTransactionCommand, TransactionView>(
            new("Expense", _app.Store.Today, 17.99m, account.Id, category.Id, null, null, SubscriptionId: annas.Id));

        Assert.Equal((netflix.Id, "Netflix"), (linked.Value!.SubscriptionId, linked.Value.SubscriptionName));
        Assert.Equal(new FieldError("subscriptionId", ErrorCodes.InvalidValue), Assert.Single(refused.Errors));
    }

    [Fact]
    public async Task Projection_ProposesTheSubscriptionsNotEndedBeforeToday()
    {
        var account = _app.GivenAccount(openedDaysAgo: 300);
        var category = _app.GivenCategory();
        var netflix = _app.GivenSubscription(account, category, "Netflix", 20m, new DateOnly(2026, 5, 5));
        _app.GivenSubscription(account, category, "Palestra", 45m, new DateOnly(2026, 1, 3)).Cancel(new DateOnly(2026, 10, 31));

        var view = (await _app.SendAsync<GetProjectionQuery, ProjectionView>(new())).Value!;

        Assert.Equal(new ProjectionSubscriptionView(netflix.Id, "Netflix", 20m, new DateOnly(2026, 5, 5), null), Assert.Single(view.Subscriptions));
        Assert.Equal(new ProjectionSubscriptionParameterView(netflix.Id, 20m, true), Assert.Single(view.Suggested.Subscriptions));
    }

    [Fact]
    public async Task Projection_SavesTheSubscriptionsAndRefusesThoseOfAnotherUser()
    {
        var account = _app.GivenAccount(openedDaysAgo: 300);
        var category = _app.GivenCategory();
        var netflix = _app.GivenSubscription(account, category);
        var annas = _app.GivenSubscription(_app.GivenAccount("Conto Anna", owner: FakeStore.Anna), _app.GivenCategory(owner: FakeStore.Anna));

        var refused = await _app.SendAsync<SaveProjectionCommand, SavedProjectionView>(Save(new ProjectionSubscriptionInput(annas.Id, 10m, true)));
        var saved = await _app.SendAsync<SaveProjectionCommand, SavedProjectionView>(Save(new ProjectionSubscriptionInput(netflix.Id, 19.99m, false)));
        var twice = await _app.SendAsync<SaveProjectionCommand, SavedProjectionView>(Save(new ProjectionSubscriptionInput(netflix.Id, 1m, true), new ProjectionSubscriptionInput(netflix.Id, 2m, true)));

        Assert.Equal(new FieldError("subscriptions[0].subscriptionId", ErrorCodes.InvalidValue), Assert.Single(refused.Errors));
        Assert.Equal(new ProjectionSubscriptionParameterView(netflix.Id, 19.99m, false), Assert.Single(saved.Value!.Subscriptions));
        Assert.Equal(new FieldError("subscriptions[1].subscriptionId", ErrorCodes.InvalidValue), Assert.Single(twice.Errors));
    }

    private static SaveProjectionCommand Save(params ProjectionSubscriptionInput[] subscriptions) =>
        new(12, 2000m, 700m, 600m, 300m, 2m, 2m, false, [], [], null, subscriptions);

    public void Dispose() => _app.Dispose();
}
