# 程式碼模板 — 鼎新 ERP 作業轉換

## Entity 模板

### 單頭 Entity

```csharp
using [英文名].Core.Common;

namespace [英文名].Core.Entities;

/// <summary>
/// [中文名] 單頭 (對應 ERP 表格 [TABLE_A])
/// </summary>
public class [TableA] : IAuditableEntity
{
    // ===== 主鍵欄位 =====
    /// <summary>[欄位說明] (PK1, char([長度]))</summary>
    public string [TA001] { get; set; } = string.Empty;

    /// <summary>[欄位說明] (PK2, char([長度]))</summary>
    public string [TA002] { get; set; } = string.Empty;

    // ===== 業務欄位 =====
    // ... 依需求新增

    // ===== 審計欄位 =====
    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    // ===== Navigation Property =====
    public virtual ICollection<[TableB]> Details { get; set; } = new List<[TableB]>();
}
```

### 單身 Entity

```csharp
namespace [英文名].Core.Entities;

/// <summary>
/// [中文名] 單身 (對應 ERP 表格 [TABLE_B])
/// </summary>
public class [TableB]
{
    // ===== 主鍵欄位 (複合PK: TB001+TB002+TB003) =====
    /// <summary>[說明] (PK1/FK, char([長度]))</summary>
    public string [TB001] { get; set; } = string.Empty;

    // ... FK 欄位 + 序號 PK

    // ===== 業務欄位 =====
    // ...

    // ===== Navigation Property =====
    public virtual [TableA]? Header { get; set; }
}
```

## Configuration 模板

```csharp
using [英文名].Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace [英文名].Infrastructure.Configurations;

public class [TableA]Configuration : IEntityTypeConfiguration<[TableA]>
{
    public void Configure(EntityTypeBuilder<[TableA]> builder)
    {
        builder.ToTable("[TABLE_A]");  // ★ 使用 ERP 實際表格名

        // 複合主鍵
        builder.HasKey(e => new { e.[PK1], e.[PK2] });

        // char 欄位: IsFixedLength() + IsUnicode(false)
        builder.Property(e => e.[TA001])
            .HasColumnName("[TA001]")
            .HasMaxLength([長度])
            .IsFixedLength()
            .IsUnicode(false);

        // nvarchar 欄位: 不需 IsFixedLength/IsUnicode
        builder.Property(e => e.[TA005])
            .HasColumnName("[TA005]")
            .HasMaxLength([長度]);

        // decimal 欄位
        builder.Property(e => e.[金額欄位])
            .HasColumnName("[TB008]")
            .HasColumnType("decimal(16,4)");

        // 審計欄位
        builder.Property(e => e.Creator).HasColumnName("CREATOR").HasMaxLength(10).IsFixedLength().IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnName("CREATE_DATE").HasMaxLength(8).IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnName("MODIFIER").HasMaxLength(10).IsFixedLength().IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnName("MODI_DATE").HasMaxLength(8).IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnName("FLAG").HasColumnType("decimal(1,0)");

        // 單頭→單身 一對多 + Cascade Delete
        builder.HasMany(e => e.Details)
            .WithOne(d => d.Header)
            .HasForeignKey(d => new { d.[FK1], d.[FK2] })
            .HasPrincipalKey(e => new { e.[PK1], e.[PK2] })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Repository 模板

```csharp
public async Task<PagedResult<[TableA]>> GetPagedAsync(int page, int pageSize, string? search = null)
{
    var query = _context.[TableA]s.Include(h => h.Details).AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim();
        query = query.Where(h =>
            h.[PK1].Contains(s) || h.[PK2].Contains(s) || /* 其他可搜尋欄位 */);
    }

    var totalItems = await query.CountAsync();

    // 動態排序 (InMemory 路徑)
    var orderCol = GetSafeOrderColumn(sortBy);
    var desc = IsDescending(sortDir);
    IOrderedQueryable<[TableA]> ordered = (orderCol, desc) switch
    {
        ("[PK1]", false) => query.OrderBy(h => h.[PK1]),
        ("[PK1]", true)  => query.OrderByDescending(h => h.[PK1]),
        ("[PK2]", false) => query.OrderBy(h => h.[PK2]),
        ("[PK2]", true)  => query.OrderByDescending(h => h.[PK2]),
        _ => query.OrderBy(h => h.[PK1])
    };
    var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return new PagedResult<[TableA]> { Items = items, TotalItems = totalItems, ... };
}
```

## Service 模板 (CrudResult 回傳)

### 單頭 Service (僅處理單頭)

```csharp
public async Task<CrudResult<[HeaderDto]>> CreateAsync(Create[Header]Request request)
{
    var exists = await _repository.ExistsAsync(request.[PK1], request.[PK2]);
    if (exists)
        return CrudResult<[HeaderDto]>.ErrorResult("資料已存在");

    var entity = request.ToEntity();
    entity.Creator = "SYSTEM";
    entity.CreateDate = DateTime.Now.ToString("yyyyMMdd");
    entity.Modifier = "SYSTEM";
    entity.ModiDate = DateTime.Now.ToString("yyyyMMdd");
    entity.Flag = 1;

    await _repository.AddAsync(entity);
    return CrudResult<[HeaderDto]>.SuccessResult(entity.ToDto(), "新增成功");
}
```

### 單身 Service (獨立 CRUD)

```csharp
public async Task<CrudResult<[DetailDto]>> CreateDetailAsync(
    string pk1, string pk2, Create[Detail]Request request)
{
    var headerExists = await _repository.ExistsAsync(pk1, pk2);
    if (!headerExists)
        return CrudResult<[DetailDto]>.ErrorResult("找不到單頭資料");

    var detailExists = await _repository.GetDetailByKeyAsync(pk1, pk2, request.[TB003]);
    if (detailExists != null)
        return CrudResult<[DetailDto]>.ErrorResult("此序號已存在");

    var entity = request.ToEntity(pk1, pk2);
    await _repository.AddDetailAsync(entity);

    // 更新單頭修改資訊
    var header = await _repository.GetByKeyAsync(pk1, pk2);
    if (header != null)
    {
        header.Modifier = "SYSTEM";
        header.ModiDate = DateTime.Now.ToString("yyyyMMdd");
        await _repository.UpdateAsync(header);
    }

    return CrudResult<[DetailDto]>.SuccessResult(entity.ToDto(), "單身新增成功");
}

public async Task<CrudResult<[DetailDto]>> UpdateDetailAsync(
    string pk1, string pk2, string tb003, Update[Detail]Request request)
{
    var entity = await _repository.GetDetailByKeyAsync(pk1, pk2, tb003);
    if (entity == null)
        return CrudResult<[DetailDto]>.ErrorResult("找不到單身資料");

    // 更新單身欄位
    entity.[TB004] = request.[TB004];
    // ... 其他欄位

    await _repository.UpdateDetailAsync(entity);
    return CrudResult<[DetailDto]>.SuccessResult(entity.ToDto(), "單身更新成功");
}

public async Task<CrudResult<bool>> DeleteDetailAsync(
    string pk1, string pk2, string tb003)
{
    await _repository.DeleteDetailAsync(pk1, pk2, tb003);
    return CrudResult<bool>.SuccessResult(true, "單身刪除成功");
}
```

## Controller 模板 (API Endpoints)

★ 單頭和單身 CRUD 完全分離，各自獨立操作

```
# 單頭 CRUD (僅單頭欄位)
GET    /api/[entity]                                    # 分頁查詢 + 搜尋
GET    /api/[entity]/{pk1}/{pk2}                        # 依主鍵取得單頭
POST   /api/[entity]                                    # 新增單頭 (不含單身)
PUT    /api/[entity]/{pk1}/{pk2}                        # 更新單頭 (不含單身)
DELETE /api/[entity]/{pk1}/{pk2}                        # 刪除單頭 (Cascade 刪除單身)

# 單身 CRUD (獨立逐筆操作)
GET    /api/[entity]/{pk1}/{pk2}/details                # 列出該單頭的所有單身
GET    /api/[entity]/{pk1}/{pk2}/details/{seq}          # 取得單筆單身
POST   /api/[entity]/{pk1}/{pk2}/details                # 新增單筆單身
PUT    /api/[entity]/{pk1}/{pk2}/details/{seq}          # 更新單筆單身
DELETE /api/[entity]/{pk1}/{pk2}/details/{seq}          # 刪除單筆單身
```

## Repository 模板 (單身獨立方法)

```csharp
// ===== 單身獨立 CRUD =====
Task<[DetailEntity]?> GetDetailByKeyAsync(string pk1, string pk2, string seq);
Task AddDetailAsync([DetailEntity] entity);
Task UpdateDetailAsync([DetailEntity] entity);
Task DeleteDetailAsync(string pk1, string pk2, string seq);
```
