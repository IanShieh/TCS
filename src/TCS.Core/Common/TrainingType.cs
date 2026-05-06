namespace TCS.Core.Common;

/// <summary>受訓類型（對應 spec §4-4）</summary>
public enum TrainingType : byte
{
    /// <summary>取得證照（首次或更新版本取得）</summary>
    取得證照 = 1,
    /// <summary>回訓</summary>
    回訓 = 2
}
