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
            "員編", "姓名", "部門", "到職日", "證照類別", "證照名稱",
            "應訓時數", "最新回訓日", "未達時數", "下次回訓", "廠別", "備註", "狀態"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.EmployeeId;
            ws.Cell(row, 2).Value = r.EmployeeName ?? "";
            ws.Cell(row, 3).Value = r.Department ?? "";
            ws.Cell(row, 4).Value = FormatHireDate(r.HireDate);
            ws.Cell(row, 5).Value = r.LicenseType;
            ws.Cell(row, 6).Value = r.Description ?? "";
            if (r.Hours.HasValue) ws.Cell(row, 7).Value = r.Hours.Value;
            else ws.Cell(row, 7).Value = "";
            ws.Cell(row, 8).Value = r.LatestRetrainDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 9).Value = (double)r.RemainingHours;
            ws.Cell(row, 10).Value = r.NextReviewDate?.ToString("yyyy-MM-dd") ?? "";
            ws.Cell(row, 11).Value = r.Plant ?? "";
            ws.Cell(row, 12).Value = r.Remark ?? "";
            ws.Cell(row, 13).Value = r.OverallStatus == OverallStatus.無 ? "" : r.OverallStatus.ToString();
            row++;
        }

        ws.ColumnsUsed().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportPlantRequirements(IReadOnlyList<PlantRequirementOverviewDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("廠別需求");

        var headers = new[] { "證照類別", "類別名稱", "需求數" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.LicenseType;
            ws.Cell(row, 2).Value = r.Description ?? "";
            ws.Cell(row, 3).Value = r.RequiredCount;
            row++;
        }

        ws.ColumnsUsed().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string FormatHireDate(string? s) =>
        s?.Length == 8 && s.All(char.IsDigit)
            ? $"{s[..4]}-{s[4..6]}-{s[6..8]}"
            : s ?? "";
}
