using DingxinErp.Core.Entities;

namespace DingxinErp.Core.DTOs;

/// <summary>
/// Entity ↔ DTO 手動映射擴充方法 (不依賴 AutoMapper)
/// </summary>
public static class MappingExtensions
{
    public static SampleHeaderDto ToDto(this SampleHeader entity)
    {
        return new SampleHeaderDto
        {
            TA001 = entity.TA001,
            TA002 = entity.TA002,
            TA003 = entity.TA003,
            TA004 = entity.TA004,
            TA005 = entity.TA005,
            TA006 = entity.TA006,
            TA007 = entity.TA007,
            Creator = entity.Creator,
            CreateDate = entity.CreateDate,
            Modifier = entity.Modifier,
            ModiDate = entity.ModiDate,
            DetailCount = entity.Details?.Count ?? 0
        };
    }

    public static SampleDetailDto ToDto(this SampleDetail entity)
    {
        return new SampleDetailDto
        {
            TB001 = entity.TB001,
            TB002 = entity.TB002,
            TB003 = entity.TB003,
            TB004 = entity.TB004,
            TB005 = entity.TB005,
            TB006 = entity.TB006,
            TB007 = entity.TB007,
            TB008 = entity.TB008,
            TB009 = entity.TB009
        };
    }

    public static SampleHeader ToEntity(this CreateSampleHeaderRequest request)
    {
        return new SampleHeader
        {
            TA001 = request.TA001,
            TA002 = request.TA002,
            TA003 = request.TA003,
            TA004 = request.TA004,
            TA005 = request.TA005,
            TA006 = request.TA006,
            TA007 = request.TA007
        };
    }

    /// <summary>
    /// 將新增單身請求 DTO 轉換為 SampleDetail Entity
    /// </summary>
    public static SampleDetail ToEntity(this CreateSampleDetailRequest request, string ta001, string ta002)
    {
        return new SampleDetail
        {
            TB001 = ta001,
            TB002 = ta002,
            TB003 = request.TB003,
            TB004 = request.TB004,
            TB005 = request.TB005,
            TB006 = request.TB006,
            TB007 = request.TB007,
            TB008 = request.TB008,
            TB009 = request.TB009
        };
    }

    public static void UpdateFrom(this SampleHeader entity, UpdateSampleHeaderRequest request)
    {
        entity.TA003 = request.TA003;
        entity.TA004 = request.TA004;
        entity.TA005 = request.TA005;
        entity.TA006 = request.TA006;
        entity.TA007 = request.TA007;
    }
}
