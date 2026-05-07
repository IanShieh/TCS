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
    public PlantController(IPlantRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok((await _repo.GetAllAsync(ct)).Select(p => p.ToDto()));
}
