using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Categories;
using FinHorizon.MySync.Domain.Common;
using FinHorizon.MySync.Domain.Transactions;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Features;

/// <summary>UC6 at the application level.</summary>
public sealed class CategoriesTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    [Fact]
    public async Task Create_NewIsCreated()
    {
        var result = await _app.SendAsync<CreateCategoryCommand, CategoryView>(new("expense", "Palestra"));

        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal(TransactionKind.Expense, result.Value!.Kind);
    }

    [Fact]
    public async Task Create_ExistingIsReturnedAsItIs()
    {
        var existing = _app.GivenCategory("Palestra");

        var result = await _app.SendAsync<CreateCategoryCommand, CategoryView>(new("Expense", " palestra "));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(existing.Id, result.Value!.Id);
        Assert.Single(_app.Store.Categories);
    }

    [Fact]
    public async Task Create_LosingTheRaceReturnsTheWinner()
    {
        var winner = Domain.Categories.Category.Create(_app.Store.Id, TransactionKind.Expense, "Palestra");
        _app.Store.FailNextSaveWithDuplicate = true;
        _app.Store.BeforeDuplicateFailure = () =>
        {
            _app.Store.Categories.Clear();
            _app.Store.Categories.Add(winner);
        };

        var result = await _app.SendAsync<CreateCategoryCommand, CategoryView>(new("Expense", "Palestra"));

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(winner.Id, result.Value!.Id);
    }

    [Theory]
    [InlineData(null, "x", "kind", ErrorCodes.Required)]
    [InlineData("0", "x", "kind", ErrorCodes.InvalidValue)]
    [InlineData("Transfer", "x", "kind", ErrorCodes.InvalidValue)]
    [InlineData("Expense", " ", "name", ErrorCodes.Required)]
    public async Task Create_InvalidInput(string? kind, string name, string field, string code)
    {
        var result = await _app.SendAsync<CreateCategoryCommand, CategoryView>(new(kind, name));

        Assert.Contains(new FieldError(field, code), result.Errors);
    }

    [Fact]
    public async Task Create_NameTooLong()
    {
        var result = await _app.SendAsync<CreateCategoryCommand, CategoryView>(new("Expense", new string('x', 41)));

        Assert.Equal([new FieldError("name", ErrorCodes.MaxLengthExceeded)], result.Errors);
    }

    [Fact]
    public async Task Search_FiltersByKindAndTermOfTheUser()
    {
        _app.GivenCategory("Palestra");
        _app.GivenCategory("Palio", TransactionKind.Income);
        _app.GivenCategory("Pala", owner: FakeStore.Anna);

        var found = (await _app.SendAsync<SearchCategoriesQuery, IReadOnlyList<CategoryView>>(new("Expense", "pal"))).Value!;

        Assert.Equal(["Palestra"], found.Select(category => category.Name));
    }

    public void Dispose() => _app.Dispose();
}
