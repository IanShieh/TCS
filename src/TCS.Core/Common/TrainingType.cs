namespace TCS.Core.Common;

/// <summary>受訓類型（對應 spec §4-4 / retrain-cycle §6 規則3）</summary>
public enum TrainingType : byte
{
    /// <summary>取得證照（初始，每張表頭恰好一筆，作為回訓週期 anchor；時數不計入回訓累計）</summary>
    取得證照 = 1,
    /// <summary>回訓（第二筆起一律此類；依日期累加時數推進週期）</summary>
    回訓 = 2
}
