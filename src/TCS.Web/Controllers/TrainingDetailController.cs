using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.DTOs.Requests;
using TCS.Core.Interfaces;
using TCS.Web.Filters;

namespace TCS.Web.Controllers;

[ApiController]
[Route("api/training-details")]
[Authorize]
public class TrainingDetailController : ControllerBase
{
    private readonly ITrainingService _svc;
    public TrainingDetailController(ITrainingService svc) => _svc = svc;

    [HttpGet("{employeeId}/{licenseType}")]
    public async Task<IActionResult> GetByHeader(string employeeId, string licenseType, CancellationToken ct = default)
    {
        try { return Ok(await _svc.GetDetailsAsync(employeeId, licenseType, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost]
    [RequireAction("新增")]
    public async Task<IActionResult> Create([FromBody] CreateTrainingDetailRequest req, CancellationToken ct = default)
    {
        try { return StatusCode(201, await _svc.AddDetailAsync(req, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{employeeId}/{licenseType}/{trainingDate}")]
    [RequireAction("修改")]
    public async Task<IActionResult> Update(string employeeId, string licenseType, DateOnly trainingDate, [FromBody] UpdateTrainingDetailRequest req, CancellationToken ct = default)
    {
        try { return Ok(await _svc.UpdateDetailAsync(req, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{employeeId}/{licenseType}/{trainingDate}")]
    [RequireAction("刪除")]
    public async Task<IActionResult> Delete(string employeeId, string licenseType, DateOnly trainingDate, CancellationToken ct = default)
    {
        var dt = trainingDate.ToDateTime(TimeOnly.MinValue);
        try { await _svc.DeleteDetailAsync(employeeId, licenseType, dt, ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
