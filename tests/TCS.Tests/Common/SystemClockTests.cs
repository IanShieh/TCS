using FluentAssertions;
using TCS.Core.Common;
using Xunit;

namespace TCS.Tests.Common;

public class SystemClockTests
{
    [Fact]
    public void LocalNow_should_be_8_hours_ahead_of_UtcNow()
    {
        var clock = new SystemClock();
        var diff = clock.LocalNow - clock.UtcNow;
        diff.TotalHours.Should().BeApproximately(8, 0.01);
    }
}
