using FinHorizon.MySync.Application.Common;
using FinHorizon.MySync.Application.Features.Accounts;
using Xunit;

namespace FinHorizon.MySync.Application.Tests.Common;

/// <summary>Every use case is wrapped the same way: logging outside, validation inside.</summary>
public sealed class CompositionTests : IDisposable
{
    private readonly ApplicationHarness _app = new();

    [Fact]
    public void Handlers_AreDecorated()
    {
        var handler = _app.Handler<CreateAccountCommand, AccountView>();

        Assert.IsType<LoggingDecorator<CreateAccountCommand, AccountView>>(handler);
    }

    [Fact]
    public void EveryRequestHasItsHandler()
    {
        var requestTypes = typeof(IRequestHandler<,>).Assembly.GetTypes()
            .SelectMany(type => type.GetInterfaces())
            .Where(type => type.IsGenericType && !type.ContainsGenericParameters && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .Distinct()
            .ToList();

        Assert.NotEmpty(requestTypes);
        Assert.All(requestTypes, type => Assert.NotNull(typeof(ApplicationHarness)
            .GetMethod(nameof(ApplicationHarness.Handler))!
            .MakeGenericMethod(type.GetGenericArguments())
            .Invoke(_app, null)));
    }

    public void Dispose() => _app.Dispose();
}
