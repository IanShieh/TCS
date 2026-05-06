namespace TCS.Core.Common;

/// <summary>時鐘抽象，方便測試注入 FakeClock 推進時間</summary>
public interface IClock
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }   // Asia/Taipei
}
