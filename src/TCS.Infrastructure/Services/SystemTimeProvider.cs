using TCS.Core.Interfaces;

namespace TCS.Infrastructure.Services;

public class SystemTimeProvider : IAppTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public TimeZoneInfo LocalTimeZone =>
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
}
