using FinHorizon.MySync.Domain.Common;
using Xunit;

namespace FinHorizon.MySync.Domain.Tests.Common;

public sealed class DescriptorTests
{
    [Fact]
    public void Text_IsTrimmedAndEmptyBecomesNull()
    {
        var descriptor = new Descriptor("  Conto Intesa ", "   ", " Banca ");

        Assert.Equal("Conto Intesa", descriptor.Name);
        Assert.Null(descriptor.Description);
        Assert.Equal("Banca", descriptor.Tag);
    }

    [Fact]
    public void NormalizedName_IgnoresCaseAndOuterSpaces() =>
        Assert.Equal(new Descriptor("Conto Intesa", null, null).NormalizedName, new Descriptor(" conto intesa ", null, null).NormalizedName);

    [Fact]
    public void MissingName_IsAProgrammingError() => Assert.Throws<ArgumentException>(() => new Descriptor(" ", null, null));

    [Fact]
    public void TooLongName_IsAProgrammingError() =>
        Assert.Throws<ArgumentException>(() => new Descriptor(new string('x', Descriptor.MaxNameLength + 1), null, null));
}
