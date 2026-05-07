using TCS.Core.DTOs;

namespace TCS.Core.Interfaces;

public interface IExcelExportService
{
    byte[] ExportTrainingHeaders(IReadOnlyList<TrainingHeaderDto> rows);
}
