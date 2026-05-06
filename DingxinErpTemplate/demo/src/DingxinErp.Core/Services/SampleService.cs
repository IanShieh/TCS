using DingxinErp.Core.Common;
using DingxinErp.Core.DTOs;
using DingxinErp.Core.Entities;
using DingxinErp.Core.Interfaces;

namespace DingxinErp.Core.Services;

/// <summary>
/// 範例 Service 實作 — 業務邏輯 + 審計欄位自動填寫
/// </summary>
public class SampleService : ISampleService
{
    private readonly ISampleRepository _repository;

    public SampleService(ISampleRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<SampleHeaderDto>> GetPagedAsync(int page, int pageSize, string? search = null, string? sortBy = null, string? sortDir = null)
    {
        var pagedEntities = await _repository.GetPagedAsync(page, pageSize, search, sortBy, sortDir);

        return new PagedResult<SampleHeaderDto>
        {
            Items = pagedEntities.Items.Select(e => e.ToDto()).ToList(),
            TotalItems = pagedEntities.TotalItems,
            TotalPages = pagedEntities.TotalPages,
            CurrentPage = pagedEntities.CurrentPage,
            PageSize = pagedEntities.PageSize,
            SearchString = pagedEntities.SearchString
        };
    }

    public async Task<CrudResult<SampleHeaderDto>> GetByKeyAsync(string ta001, string ta002)
    {
        var entity = await _repository.GetByKeyAsync(ta001, ta002);
        if (entity == null)
            return CrudResult<SampleHeaderDto>.ErrorResult($"找不到資料 (單別={ta001}, 單號={ta002})");

        return CrudResult<SampleHeaderDto>.SuccessResult(entity.ToDto());
    }

    public async Task<CrudResult<List<SampleDetailDto>>> GetDetailsAsync(string ta001, string ta002)
    {
        var exists = await _repository.ExistsAsync(ta001, ta002);
        if (!exists)
            return CrudResult<List<SampleDetailDto>>.ErrorResult($"找不到單頭資料 (單別={ta001}, 單號={ta002})");

        var details = await _repository.GetDetailsByKeyAsync(ta001, ta002);
        return CrudResult<List<SampleDetailDto>>.SuccessResult(details.Select(d => d.ToDto()).ToList());
    }

    public async Task<CrudResult<SampleHeaderDto>> CreateAsync(CreateSampleHeaderRequest request)
    {
        var exists = await _repository.ExistsAsync(request.TA001, request.TA002);
        if (exists)
            return CrudResult<SampleHeaderDto>.ErrorResult($"資料已存在 (單別={request.TA001}, 單號={request.TA002})");

        var entity = request.ToEntity();
        var now = DateTime.Now.ToString("yyyyMMdd");
        entity.Creator = "SYSTEM";
        entity.CreateDate = now;
        entity.Modifier = "SYSTEM";
        entity.ModiDate = now;
        entity.Flag = 1;

        await _repository.AddAsync(entity);
        return CrudResult<SampleHeaderDto>.SuccessResult(entity.ToDto(), "新增成功");
    }

    public async Task<CrudResult<SampleHeaderDto>> UpdateAsync(string ta001, string ta002, UpdateSampleHeaderRequest request)
    {
        var entity = await _repository.GetByKeyAsync(ta001, ta002);
        if (entity == null)
            return CrudResult<SampleHeaderDto>.ErrorResult($"找不到資料 (單別={ta001}, 單號={ta002})");

        entity.UpdateFrom(request);
        entity.Modifier = "SYSTEM";
        entity.ModiDate = DateTime.Now.ToString("yyyyMMdd");

        await _repository.UpdateAsync(entity);
        return CrudResult<SampleHeaderDto>.SuccessResult(entity.ToDto(), "更新成功");
    }

    public async Task<CrudResult<bool>> DeleteAsync(string ta001, string ta002)
    {
        var exists = await _repository.ExistsAsync(ta001, ta002);
        if (!exists)
            return CrudResult<bool>.ErrorResult($"找不到資料 (單別={ta001}, 單號={ta002})");

        await _repository.DeleteAsync(ta001, ta002);
        return CrudResult<bool>.SuccessResult(true, "刪除成功");
    }

    public async Task<CrudResult<SampleDetailDto>> GetDetailByKeyAsync(string ta001, string ta002, string tb003)
    {
        var exists = await _repository.ExistsAsync(ta001, ta002);
        if (!exists)
            return CrudResult<SampleDetailDto>.ErrorResult($"找不到單頭資料 (單別={ta001}, 單號={ta002})");

        var detail = await _repository.GetDetailByKeyAsync(ta001, ta002, tb003);
        if (detail == null)
            return CrudResult<SampleDetailDto>.ErrorResult($"找不到單身資料 (序號={tb003})");

        return CrudResult<SampleDetailDto>.SuccessResult(detail.ToDto());
    }

    public async Task<CrudResult<SampleDetailDto>> CreateDetailAsync(string ta001, string ta002, CreateSampleDetailRequest request)
    {
        var header = await _repository.GetByKeyAsync(ta001, ta002);
        if (header == null)
            return CrudResult<SampleDetailDto>.ErrorResult($"找不到單頭資料 (單別={ta001}, 單號={ta002})");

        // 檢查序號是否已存在
        var existingDetail = await _repository.GetDetailByKeyAsync(ta001, ta002, request.TB003);
        if (existingDetail != null)
            return CrudResult<SampleDetailDto>.ErrorResult($"單身序號已存在 (序號={request.TB003})");

        var entity = request.ToEntity(ta001, ta002);
        await _repository.AddDetailAsync(entity);

        // 更新單頭審計欄位
        header.Modifier = "SYSTEM";
        header.ModiDate = DateTime.Now.ToString("yyyyMMdd");
        await _repository.UpdateAsync(header);

        return CrudResult<SampleDetailDto>.SuccessResult(entity.ToDto(), "單身新增成功");
    }

    public async Task<CrudResult<SampleDetailDto>> UpdateDetailAsync(string ta001, string ta002, string tb003, UpdateSampleDetailRequest request)
    {
        var header = await _repository.GetByKeyAsync(ta001, ta002);
        if (header == null)
            return CrudResult<SampleDetailDto>.ErrorResult($"找不到單頭資料 (單別={ta001}, 單號={ta002})");

        var detail = await _repository.GetDetailByKeyAsync(ta001, ta002, tb003);
        if (detail == null)
            return CrudResult<SampleDetailDto>.ErrorResult($"找不到單身資料 (序號={tb003})");

        // 更新單身欄位
        detail.TB004 = request.TB004;
        detail.TB005 = request.TB005;
        detail.TB006 = request.TB006;
        detail.TB007 = request.TB007;
        detail.TB008 = request.TB008;
        detail.TB009 = request.TB009;
        await _repository.UpdateDetailAsync(detail);

        // 更新單頭審計欄位
        header.Modifier = "SYSTEM";
        header.ModiDate = DateTime.Now.ToString("yyyyMMdd");
        await _repository.UpdateAsync(header);

        return CrudResult<SampleDetailDto>.SuccessResult(detail.ToDto(), "單身更新成功");
    }

    public async Task<CrudResult<bool>> DeleteDetailAsync(string ta001, string ta002, string tb003)
    {
        var header = await _repository.GetByKeyAsync(ta001, ta002);
        if (header == null)
            return CrudResult<bool>.ErrorResult($"找不到單頭資料 (單別={ta001}, 單號={ta002})");

        var detail = await _repository.GetDetailByKeyAsync(ta001, ta002, tb003);
        if (detail == null)
            return CrudResult<bool>.ErrorResult($"找不到單身資料 (序號={tb003})");

        await _repository.DeleteDetailAsync(ta001, ta002, tb003);

        // 更新單頭審計欄位
        header.Modifier = "SYSTEM";
        header.ModiDate = DateTime.Now.ToString("yyyyMMdd");
        await _repository.UpdateAsync(header);

        return CrudResult<bool>.SuccessResult(true, "單身刪除成功");
    }
}
