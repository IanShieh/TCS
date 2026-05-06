namespace TCS.Core.Common;

public class SystemClock : IClock
{
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime LocalNow => TimeZoneInfo.ConvertTime(DateTime.UtcNow, TaipeiTz);
}
