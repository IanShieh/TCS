using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.Interfaces;
using TCS.Core.Mapping;

namespace TCS.Web.Controllers;

[ApiController]
[Route("api/plants")]
[Authorize]
public class PlantController : ControllerBase
{
    private readonly IPlantRepository _repo;
    private readonly ILicenseService _licenseSvc;

    public PlantController(IPlantRepository repo, ILicenseService licenseSvc)
    {
        _repo = repo;
        _licenseSvc = licenseSvc;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok((await _repo.GetAllAsync(ct)).Select(p => p.ToDto()));

    /// <summary>廠別需求總覽：依廠別反查證照需求（唯讀，僅 [Authorize]；todo-2026-07-09 T3）</summary>
    [HttpGet("{plant}/requirements")]
    public async Task<IActionResult> GetRequirements(string plant, CancellationToken ct = default)
        => Ok(await _licenseSvc.GetRequirementsByPlantAsync(plant, ct));
}
