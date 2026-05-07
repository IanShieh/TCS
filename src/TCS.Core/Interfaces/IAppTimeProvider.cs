namespace TCS.Core.Interfaces;

public interface IAppTimeProvider
{
    DateTimeOffset UtcNow { get; }
    TimeZoneInfo LocalTimeZone { get; }
}
