using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.Interfaces;
using TCS.Core.Mapping;

namespace TCS.Web.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeRepository _repo;
    public EmployeeController(IEmployeeRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok((await _repo.GetAllAsync(ct)).Select(e => e.ToDto()));

    [HttpGet("{id:regex(^(?!search$).+$)}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct = default)
    {
        var emp = await _repo.GetByIdAsync(id, ct);
        return emp == null ? NotFound() : Ok(emp.ToDto());
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q = "", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
        return Ok((await _repo.SearchAsync(q, 50, ct)).Select(e => e.ToDto()));
    }
}
