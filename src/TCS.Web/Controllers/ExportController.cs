using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.DTOs.Requests;
using TCS.Core.Interfaces;
using TCS.Web.Filters;

namespace TCS.Web.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly ITrainingService _trainingSvc;
    private readonly ILicenseService _licenseSvc;
    private readonly IExcelExportService _excelSvc;

    public ExportController(ITrainingService trainingSvc, ILicenseService licenseSvc, IExcelExportService excelSvc)
    {
        _trainingSvc = trainingSvc;
        _licenseSvc = licenseSvc;
        _excelSvc = excelSvc;
    }

    [HttpGet("training-headers")]
    [RequireAction("列印")]
    public async Task<IActionResult> ExportHeaders(
        [FromQuery] TrainingSearchQuery? query = null,
        CancellationToken ct = default)
    {
        var result = await _trainingSvc.GetHeadersAsync(null, null, 1, int.MaxValue, query, ct);
        var bytes = _excelSvc.ExportTrainingHeaders(result.Items);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"training_export_{DateTime.Today:yyyyMMdd}.xlsx");
    }

    [HttpGet("plant-requirements")]
    [RequireAction("列印")]
    public async Task<IActionResult> ExportPlantRequirements([FromQuery] string plant, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plant)) return BadRequest(new { message = "plant 為必填。" });
        var rows = await _licenseSvc.GetRequirementsByPlantAsync(plant, ct);
        var bytes = _excelSvc.ExportPlantRequirements(rows);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"plant_requirements_{plant}_{DateTime.Today:yyyyMMdd}.xlsx");
    }
}
