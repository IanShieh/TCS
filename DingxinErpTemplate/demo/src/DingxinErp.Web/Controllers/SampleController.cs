using DingxinErp.Core.DTOs;
using DingxinErp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DingxinErp.Web.Controllers;

/// <summary>
/// 範例作業 API Controller — 提供 CRUD + 搜尋 + 單身管理
/// ★ 實際使用時，將此 Controller 改名為對應的作業名稱
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class SampleController : ControllerBase
{
    private readonly ISampleService _service;

    public SampleController(ISampleService service)
    {
        _service = service;
    }

    /// <summary>分頁查詢單頭 (含搜尋)</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null)
    {
        var result = await _service.GetPagedAsync(page, pageSize, search, sortBy, sortDir);
        return Ok(result);
    }

    /// <summary>依主鍵取得單頭</summary>
    [HttpGet("{ta001}/{ta002}")]
    public async Task<IActionResult> GetByKey(string ta001, string ta002)
    {
        var result = await _service.GetByKeyAsync(ta001, ta002);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>取得指定單頭的單身明細</summary>
    [HttpGet("{ta001}/{ta002}/details")]
    public async Task<IActionResult> GetDetails(string ta001, string ta002)
    {
        var result = await _service.GetDetailsAsync(ta001, ta002);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>新增單頭 (僅單頭欄位)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSampleHeaderRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetByKey), new { ta001 = request.TA001, ta002 = request.TA002 }, result);
    }

    /// <summary>更新單頭 (僅單頭欄位)</summary>
    [HttpPut("{ta001}/{ta002}")]
    public async Task<IActionResult> Update(string ta001, string ta002, [FromBody] UpdateSampleHeaderRequest request)
    {
        var result = await _service.UpdateAsync(ta001, ta002, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>刪除單頭 (Cascade 刪除單身)</summary>
    [HttpDelete("{ta001}/{ta002}")]
    public async Task<IActionResult> Delete(string ta001, string ta002)
    {
        var result = await _service.DeleteAsync(ta001, ta002);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>依主鍵取得單筆單身</summary>
    [HttpGet("{ta001}/{ta002}/details/{tb003}")]
    public async Task<IActionResult> GetDetailByKey(string ta001, string ta002, string tb003)
    {
        var result = await _service.GetDetailByKeyAsync(ta001, ta002, tb003);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>新增單筆單身</summary>
    [HttpPost("{ta001}/{ta002}/details")]
    public async Task<IActionResult> CreateDetail(string ta001, string ta002, [FromBody] CreateSampleDetailRequest request)
    {
        var result = await _service.CreateDetailAsync(ta001, ta002, request);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetDetailByKey), new { ta001, ta002, tb003 = request.TB003 }, result);
    }

    /// <summary>更新單筆單身</summary>
    [HttpPut("{ta001}/{ta002}/details/{tb003}")]
    public async Task<IActionResult> UpdateDetail(string ta001, string ta002, string tb003, [FromBody] UpdateSampleDetailRequest request)
    {
        var result = await _service.UpdateDetailAsync(ta001, ta002, tb003, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>刪除單筆單身</summary>
    [HttpDelete("{ta001}/{ta002}/details/{tb003}")]
    public async Task<IActionResult> DeleteDetail(string ta001, string ta002, string tb003)
    {
        var result = await _service.DeleteDetailAsync(ta001, ta002, tb003);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }
}
