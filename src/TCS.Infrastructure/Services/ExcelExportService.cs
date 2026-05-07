using ClosedXML.Excel;
using TCS.Core.DTOs;
using TCS.Core.Interfaces;

namespace TCS.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportTrainingHeaders(IReadOnlyList<TrainingHeaderDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("受訓紀錄");

        var headers = new[]
        {
            "員工代號", "姓名", "部門", "證照類別", "證照說明",
            "所需時數", "備註", "最後取得日", "最後複訓日",
            "下次複審日", "累計時數", "剩餘時數", "狀態"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.EmployeeId;
            ws.Cell(row, 2).Value = r.EmployeeName ?? "";
            ws.Cell(row, 3).Value = r.Department ?? "";
            ws.Cell(row, 4).Value = r.LicenseType;
            ws.Cell(row, 5).Value = r.Description ?? "";
            ws.Cell(row, 6).Value = r.RequiredHours;
            ws.Cell(row, 7).Value = r.Remark ?? "";
            ws.Cell(row, 8).Value = r.LatestAcquireDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 9).Value = r.LatestRetrainDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 10).Value = r.NextReviewDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 11).Value = (double)r.AccumulatedHours;
            ws.Cell(row, 12).Value = (double)r.RemainingHours;
            ws.Cell(row, 13).Value = r.OverallStatus.ToString();
            row++;
        }

        ws.ColumnsUsed().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
