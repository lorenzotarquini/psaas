using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinHorizon.MySync.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void WithoutTheConnectionString_TheBackendDoesNotStart()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddMySyncInfrastructure(new ConfigurationBuilder().Build()));

        Assert.Contains("mysync-db", exception.Message);
    }
}
