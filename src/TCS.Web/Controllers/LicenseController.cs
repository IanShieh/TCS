using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.DTOs.Requests;
using TCS.Core.Interfaces;
using TCS.Web.Filters;

namespace TCS.Web.Controllers;

[ApiController]
[Route("api/licenses")]
[Authorize]
public class LicenseApiController : ControllerBase
{
    private readonly ILicenseService _svc;
    public LicenseApiController(ILicenseService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] LicenseSearchQuery? query = null,
        CancellationToken ct = default)
        => Ok(await _svc.GetAllAsync(page, pageSize, search, query, ct));

    [HttpGet("{licenseType}")]
    public async Task<IActionResult> GetById(string licenseType, CancellationToken ct = default)
    {
        try { return Ok(await _svc.GetByIdAsync(licenseType, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost]
    [RequireAction("新增")]
    public async Task<IActionResult> Create([FromBody] CreateLicenseMasterRequest req, CancellationToken ct = default)
    {
        try { return StatusCode(201, await _svc.CreateAsync(req, ct)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{licenseType}")]
    [RequireAction("修改")]
    public async Task<IActionResult> Update(string licenseType, [FromBody] UpdateLicenseMasterRequest req, CancellationToken ct = default)
    {
        if (licenseType != req.LicenseType) return BadRequest(new { message = "Route LicenseType does not match body." });
        try { return Ok(await _svc.UpdateAsync(req, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{licenseType}")]
    [RequireAction("刪除")]
    public async Task<IActionResult> Delete(string licenseType, CancellationToken ct = default)
    {
        try { await _svc.DeleteAsync(licenseType, ct); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
