using DingxinErp.Core.Common;
using DingxinErp.Core.Entities;

namespace DingxinErp.Core.Interfaces;

/// <summary>
/// 範例 Repository 介面 — 定義資料存取契約
/// </summary>
public interface ISampleRepository
{
    Task<PagedResult<SampleHeader>> GetPagedAsync(int page, int pageSize, string? search = null, string? sortBy = null, string? sortDir = null);
    Task<SampleHeader?> GetByKeyAsync(string ta001, string ta002);
    Task<List<SampleDetail>> GetDetailsByKeyAsync(string ta001, string ta002);
    Task<bool> ExistsAsync(string ta001, string ta002);
    Task AddAsync(SampleHeader entity);
    Task UpdateAsync(SampleHeader entity);
    Task DeleteAsync(string ta001, string ta002);
    Task<SampleDetail?> GetDetailByKeyAsync(string ta001, string ta002, string tb003);
    Task AddDetailAsync(SampleDetail entity);
    Task UpdateDetailAsync(SampleDetail entity);
    Task DeleteDetailAsync(string ta001, string ta002, string tb003);
}
