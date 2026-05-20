namespace TCS.Core.DTOs;

/// <summary>證照整體狀態</summary>
public enum OverallStatus
{
    回訓完成 = 0,
    待回訓 = 1,
    已過期 = 2,
    無 = 3      // 未達時數 > 0 且下次回訓未在一年內（或無回訓日）
}
