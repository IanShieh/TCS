using FluentAssertions;
using TCS.Core.Helpers;
using Xunit;

namespace TCS.Tests.Helpers;

public class OtherLicenseCodeTests
{
    [Theory]
    [InlineData("99", "99")]      // 其他大類
    [InlineData("1", "1.0")]      // 其他小類（母大類 1）
    [InlineData("12", "12.0")]
    public void Prefix_DerivesCorrectly(string baseCategory, string expected) =>
        OtherLicenseCode.Prefix(baseCategory).Should().Be(expected);

    [Fact]
    public void Next_NoExisting_StartsAtOne() =>
        OtherLicenseCode.Next("99", new string[0]).Should().Be("99.1");

    [Fact]
    public void Next_SkipsToMaxPlusOne()
    {
        var existing = new[] { "99.1", "99.2", "99.5" };
        OtherLicenseCode.Next("99", existing).Should().Be("99.6");
    }

    [Fact]
    public void Next_MinorPrefix_TwoDots()
    {
        var existing = new[] { "1.0.1", "1.0.2" };
        OtherLicenseCode.Next("1.0", existing).Should().Be("1.0.3");
    }

    [Fact]
    public void Next_IgnoresNonMatchingPrefixAndNonNumericTail()
    {
        var existing = new[] { "1.1", "1.0.x", "10.0.1", "1.0.2" };
        OtherLicenseCode.Next("1.0", existing).Should().Be("1.0.3");
    }
}
