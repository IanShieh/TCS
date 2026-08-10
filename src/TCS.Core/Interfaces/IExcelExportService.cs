using TCS.Core.DTOs;

namespace TCS.Core.Interfaces;

public interface IExcelExportService
{
    byte[] ExportTrainingHeaders(IReadOnlyList<TrainingHeaderDto> rows);
    /// <summary>廠別需求匯出（單一廠別，欄位與廠別需求頁一致；廠別由檔名承載）</summary>
    byte[] ExportPlantRequirements(IReadOnlyList<PlantRequirementOverviewDto> rows);
}
