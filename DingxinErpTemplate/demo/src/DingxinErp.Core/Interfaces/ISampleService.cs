using DingxinErp.Core.Common;
using DingxinErp.Core.DTOs;

namespace DingxinErp.Core.Interfaces;

/// <summary>
/// 範例 Service 介面 — 定義業務邏輯契約
/// </summary>
public interface ISampleService
{
    Task<PagedResult<SampleHeaderDto>> GetPagedAsync(int page, int pageSize, string? search = null, string? sortBy = null, string? sortDir = null);
    Task<CrudResult<SampleHeaderDto>> GetByKeyAsync(string ta001, string ta002);
    Task<CrudResult<List<SampleDetailDto>>> GetDetailsAsync(string ta001, string ta002);
    Task<CrudResult<SampleHeaderDto>> CreateAsync(CreateSampleHeaderRequest request);
    Task<CrudResult<SampleHeaderDto>> UpdateAsync(string ta001, string ta002, UpdateSampleHeaderRequest request);
    Task<CrudResult<bool>> DeleteAsync(string ta001, string ta002);
    Task<CrudResult<SampleDetailDto>> GetDetailByKeyAsync(string ta001, string ta002, string tb003);
    Task<CrudResult<SampleDetailDto>> CreateDetailAsync(string ta001, string ta002, CreateSampleDetailRequest request);
    Task<CrudResult<SampleDetailDto>> UpdateDetailAsync(string ta001, string ta002, string tb003, UpdateSampleDetailRequest request);
    Task<CrudResult<bool>> DeleteDetailAsync(string ta001, string ta002, string tb003);
}
