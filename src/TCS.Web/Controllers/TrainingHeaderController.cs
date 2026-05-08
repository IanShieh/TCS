using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.DTOs.Requests;
using TCS.Core.Interfaces;
using TCS.Web.Filters;

namespace TCS.Web.Controllers;

[ApiController]
[Route("api/training-headers")]
[Authorize]
public class TrainingHeaderApiController : ControllerBase
{
    private readonly ITrainingService _svc;
    public TrainingHeaderApiController(ITrainingService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? employeeId,
        [FromQuery] string? licenseType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TrainingSearchQuery? query = null,
        CancellationToken ct = default)
        => Ok(await _svc.GetHeadersAsync(employeeId, licenseType, page, pageSize, query, ct));

    [HttpGet("{employeeId}/{licenseType}")]
    public async Task<IActionResult> GetById(string employeeId, string licenseType, CancellationToken ct = default)
    {
        try { return Ok(await _svc.GetHeaderAsync(employeeId, licenseType, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost]
    [RequireAction("新增")]
    public async Task<IActionResult> Create([FromBody] CreateTrainingHeaderRequest req, CancellationToken ct = default)
    {
        try { return StatusCode(201, await _svc.CreateHeaderAsync(req, ct)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{employeeId}/{licenseType}")]
    [RequireAction("修改")]
    public async Task<IActionResult> Update(string employeeId, string licenseType, [FromBody] UpdateTrainingHeaderRequest req, CancellationToken ct = default)
    {
        try { return Ok(await _svc.UpdateHeaderAsync(req, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{employeeId}/{licenseType}")]
    [RequireAction("刪除")]
    public async Task<IActionResult> Delete(string employeeId, string licenseType, CancellationToken ct = default)
    {
        try { await _svc.DeleteHeaderAsync(employeeId, licenseType, ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
