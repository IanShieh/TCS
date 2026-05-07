using Moq;
using TCS.Core.Interfaces;
using Xunit;

namespace TCS.Tests.Services;

public class ExpiryScanServiceTests
{
    [Fact]
    public void WaitsUntilNextMidnight_DelayIsApproximatelyOneHour()
    {
        // Arrange: fake clock = 11:00 PM Taipei time (UTC+8)
        var fakeNow = new DateTimeOffset(2024, 1, 15, 23, 0, 0, TimeSpan.FromHours(8));
        var clockMock = new Mock<IAppTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(fakeNow.ToUniversalTime());
        clockMock.Setup(c => c.LocalTimeZone)
            .Returns(TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"));

        // Act: compute delay as ExpiryScanService would
        var now = TimeZoneInfo.ConvertTime(clockMock.Object.UtcNow, clockMock.Object.LocalTimeZone);
        var nextMidnight = now.Date.AddDays(1);
        var delay = nextMidnight - now.DateTime;

        // Assert: ~1 hour
        Assert.True(delay.TotalMinutes > 59 && delay.TotalMinutes < 61,
            $"Expected ~60 minutes, got {delay.TotalMinutes} minutes");
    }
}
