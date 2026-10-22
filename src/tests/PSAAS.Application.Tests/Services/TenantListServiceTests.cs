using PSAAS.Application.Abstractions.Tenants;
using PSAAS.Application.Abstractions.Views;
using PSAAS.Application.Contracts;
using PSAAS.Application.Contracts.Tenants;
using PSAAS.Application.Contracts.Views;
using PSAAS.Application.Services.Tenants;
using PSAAS.Application.Services.Views;
using Xunit;

namespace PSAAS.Application.Tests.Services;

/// <summary>Derived from UC21 (elenco clienti) and UC22 (viste salvate).</summary>
public sealed class TenantListServiceTests
{
    private sealed class FakeList : ITenantListRepository
    {
        public List<TenantListItem> Items { get; } = [];

        public Task<PagedResult<TenantListItem>> GetListAsync(TenantListQuery query, DateTimeOffset now, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<TenantListItem>(Items, Items.Count, query.Page, query.PageSize));

        public Task<TenantCounts> GetCountsAsync(DateTimeOffset now, int renewalDays, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeViews : ISavedViewRepository
    {
        public List<(string Owner, SavedView View)> Views { get; } = [];

        public Task<IReadOnlyList<SavedView>> GetAsync(string ownerId, string area, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavedView>>(Views.Where(v => v.Owner == ownerId && v.View.Area == area).Select(v => v.View).ToArray());

        public Task AddAsync(string ownerId, SavedView view, CancellationToken cancellationToken = default)
        {
            Views.Add((ownerId, view));
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(string ownerId, Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Views.RemoveAll(v => v.Owner == ownerId && v.View.Id == id) > 0);
    }

    private readonly FakeList _list = new();
    private readonly FakeUserDirectory _users = new();

    private TenantListService Service => new(_list, _users, TimeProvider.System);

    private static TenantListItem Item(string name, params string[] profileIds) => new(
        Guid.NewGuid(), name, "1", "IT1", "a@pec.it", "Roma", "RM", "tn_1", "Active", DateTimeOffset.UtcNow,
        null, null, null, null, null, [], profileIds);

    [Theory]
    [InlineData("colore", null, null, "sort")]
    [InlineData("-renewsAt", "Felice", null, "status")]
    [InlineData("city", null, 0, "renewalWithinDays")]
    [InlineData("-plan", "Trial", 30, null)]
    public void UC21_S7_InvalidParameters(string sort, string? status, int? days, string? field)
    {
        var query = new TenantListQuery(Statuses: status is null ? null : [status], RenewalWithinDays: days, Sort: sort);

        Assert.Equal(field, TenantListService.InvalidParameter(query));
    }

    [Fact]
    public async Task UC21_S5_CountsUsersOfThePageTenants()
    {
        _list.Items.AddRange([Item("Alfa Srl", "p1"), Item("Beta Snc")]);
        _users.Users.Add(new("u1", "M", "R", "m@x.it", null, ["p1"]));

        var page = await Service.GetListAsync(new TenantListQuery());

        Assert.Equal([1, 0], page.Items.Select(t => t.UserCount));
    }

    [Fact]
    public async Task UC21_3a_KeycloakDown_ListWithoutUserCounts()
    {
        _list.Items.Add(Item("Alfa Srl", "p1"));
        _users.Unavailable = true;
        _users.Users.Add(new("u1", "M", "R", "m@x.it", null, ["p1"]));

        var page = await Service.GetListAsync(new TenantListQuery());

        Assert.Null(Assert.Single(page.Items).UserCount);
    }

    [Fact]
    public async Task UC21_S6_CsvQuotesAndNeutralizesFormulas()
    {
        _list.Items.Add(Item("Rossi, Bianchi & C. \"Snc\""));
        _list.Items.Add(Item("=HYPERLINK(\"x\")"));

        var lines = (await Service.ExportCsvAsync(new TenantListQuery())).TrimEnd().Split('\n');

        Assert.StartsWith("\"Rossi, Bianchi & C. \"\"Snc\"\"\",IT1,", lines[1]);
        Assert.StartsWith("\"'=HYPERLINK(\"\"x\"\")\",", lines[2]);
    }

    [Fact]
    public async Task UC22_NamesAreUniquePerOwnerIgnoringCase_AndLimited()
    {
        var views = new FakeViews();
        var service = new SavedViewService(views, new FakeCurrentActor(), TimeProvider.System);

        Assert.NotNull((await service.SaveAsync(new("tenants", " Pro a Milano ", "?province=MI"))).View);
        Assert.Equal("NotUnique", (await service.SaveAsync(new("tenants", "PRO A MILANO", ""))).Errors["name"]);
        Assert.Equal("InvalidValue", (await service.SaveAsync(new("invoices", "X", ""))).Errors["area"]);
        Assert.Equal("MaxLengthExceeded", (await service.SaveAsync(new("tenants", new string('x', 41), ""))).Errors["name"]);
        for (var i = 1; i < SavedViewService.MaxViews; i++) await service.SaveAsync(new("tenants", $"v{i}", ""));
        Assert.Equal("LimitExceeded", (await service.SaveAsync(new("tenants", "una di troppo", ""))).Errors["name"]);
        Assert.Equal("province=MI", views.Views[0].View.Query);
    }
}
