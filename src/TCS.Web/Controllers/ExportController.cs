using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.Interfaces;

namespace TCS.Web.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly ITrainingService _trainingSvc;
    private readonly IExcelExportService _excelSvc;

    public ExportController(ITrainingService trainingSvc, IExcelExportService excelSvc)
    {
        _trainingSvc = trainingSvc;
        _excelSvc = excelSvc;
    }

    [HttpGet("training-headers")]
    public async Task<IActionResult> ExportHeaders(
        [FromQuery] string? employeeId,
        [FromQuery] string? licenseType,
        CancellationToken ct = default)
    {
        var result = await _trainingSvc.GetHeadersAsync(employeeId, licenseType, 1, int.MaxValue, ct);
        var bytes = _excelSvc.ExportTrainingHeaders(result.Items);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"training_export_{DateTime.Today:yyyyMMdd}.xlsx");
    }
}
