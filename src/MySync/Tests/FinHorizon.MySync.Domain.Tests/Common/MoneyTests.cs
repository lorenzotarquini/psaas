using FinHorizon.MySync.Domain.Common;
using Xunit;

namespace FinHorizon.MySync.Domain.Tests.Common;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("10", true)]
    [InlineData("10.5", true)]
    [InlineData("10.50", true)]
    [InlineData("-300.00", true)]
    [InlineData("10.005", false)]
    [InlineData("0.001", false)]
    public void Precision_IsEuroCents(string amount, bool valid) =>
        Assert.Equal(valid, Money.HasValidPrecision(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Arithmetic_StaysDecimal()
    {
        var total = new Money(0.1m) + new Money(0.2m);

        Assert.Equal(new Money(0.3m), total);
        Assert.Equal(new Money(-0.3m), -total);
        Assert.True(total > Money.Zero);
    }

    [Fact]
    public void Sum_OfNothingIsZero() => Assert.Equal(Money.Zero, Money.Sum([]));
}
