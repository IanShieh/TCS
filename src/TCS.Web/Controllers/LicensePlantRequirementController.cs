using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.DTOs.Requests;
using TCS.Core.Interfaces;
using TCS.Web.Filters;

namespace TCS.Web.Controllers;

[ApiController]
[Route("api/licenses/{licenseType}/plants")]
[Authorize]
public class LicensePlantRequirementController : ControllerBase
{
    private readonly ILicenseService _svc;
    public LicensePlantRequirementController(ILicenseService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(string licenseType, CancellationToken ct = default)
    {
        try { return Ok(await _svc.GetPlantRequirementsAsync(licenseType, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost]
    [RequireAction("新增")]
    public async Task<IActionResult> Create(string licenseType, [FromBody] CreateLicensePlantRequirementRequest req, CancellationToken ct = default)
    {
        if (licenseType != req.LicenseType) return BadRequest(new { message = "Route LicenseType does not match body." });
        try { return StatusCode(201, await _svc.CreatePlantRequirementAsync(req, ct)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPut("{plant}")]
    [RequireAction("修改")]
    public async Task<IActionResult> Update(string licenseType, string plant, [FromBody] UpdateLicensePlantRequirementRequest req, CancellationToken ct = default)
    {
        try { return Ok(await _svc.UpdatePlantRequirementAsync(req, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{plant}")]
    [RequireAction("刪除")]
    public async Task<IActionResult> Delete(string licenseType, string plant, CancellationToken ct = default)
    {
        try { await _svc.DeletePlantRequirementAsync(licenseType, plant, ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
