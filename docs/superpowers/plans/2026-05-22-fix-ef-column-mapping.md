# Fix EF Core Column Mapping (Schema-Driven) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 依 schema.json 將 TCSMA / TCSMB / TCSTA / TCSTB 四張表的 EF Core 欄位配置改以 `HasColumnName()` 對應實際資料庫欄位（MA001、MB001…），修正 Flag 型別，並重建 Migration baseline，消除 `SqlException: 無效的資料行名稱` 500 錯誤。

**Architecture:** 只改 `Configurations/` 裡的四個 `IEntityTypeConfiguration<T>`，C# 實體屬性名稱不動（`LicenseType`、`Description`…），由 `HasColumnName()` 在 EF 查詢時轉換為資料庫實際欄位名（`MA001`、`MA002`…）。完成後刪除全部舊 migrations、以空白 Up() 重建 InitialCreate baseline，再執行 `database update` 讓 `__EFMigrationsHistory` 記錄新 baseline，不改動資料庫結構。

**Tech Stack:** .NET 8, EF Core 8, SQL Server (Microsoft.Data.SqlClient), xUnit

---

## 欄位對照速查表

| C# 屬性 | TCSMA | TCSMB | TCSTA | TCSTB |
|---|---|---|---|---|
| LicenseType / EmployeeId / Plant(PK) | MA001 | MB001 / MB002 | TA001 / TA002 | TB001 / TB002 |
| Description / RequiredCount / Plant(FK) / TrainingDate | MA002 | MB003 | TA003 | TB003 |
| Category / Hours | MA003 | — | TA004 | — |
| Hours / TrainingType | MA004 | — | — | TB004 |
| Years / Hours(detail) | MA005 | — | TA006 | TB005 |
| Remark | — | — | TA005 | — |
| Creator | CREATOR | CREATOR | CREATOR | CREATOR |
| CreateDate | CREATE_DATE | CREATE_DATE | CREATE_DATE | CREATE_DATE |
| Modifier | MODIFIER | MODIFIER | MODIFIER | MODIFIER |
| ModiDate | MODI_DATE | MODI_DATE | MODI_DATE | MODI_DATE |
| Flag | FLAG decimal(3,0) | FLAG decimal(3,0) | FLAG decimal(3,0) | FLAG decimal(3,0) |
| Company | COMPANY | COMPANY | COMPANY | COMPANY |
| UsrGroup | USR_GROUP | USR_GROUP | USR_GROUP | USR_GROUP |

---

## Task 1: 修正 LicenseMasterConfiguration（TCSMA）

**Files:**
- Modify: `src/TCS.Infrastructure/Configurations/LicenseMasterConfiguration.cs`

- [ ] **Step 1: 完整替換 Configure 方法**

```csharp
// src/TCS.Infrastructure/Configurations/LicenseMasterConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class LicenseMasterConfiguration : IEntityTypeConfiguration<LicenseMaster>
{
    public void Configure(EntityTypeBuilder<LicenseMaster> builder)
    {
        builder.ToTable("TCSMA");
        builder.HasKey(e => e.LicenseType);

        builder.Property(e => e.LicenseType).HasColumnName("MA001").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Description).HasColumnName("MA002").HasMaxLength(100);
        builder.Property(e => e.Category).HasColumnName("MA003").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Hours).HasColumnName("MA004");
        builder.Property(e => e.Years).HasColumnName("MA005");
        builder.Property(e => e.Creator).HasColumnName("CREATOR").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnName("CREATE_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnName("MODIFIER").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnName("MODI_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnName("FLAG").HasColumnType("decimal(3,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);

        builder.HasMany(e => e.PlantRequirements)
            .WithOne(r => r.LicenseMasterNav)
            .HasForeignKey(r => r.LicenseType)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.TrainingHeaders)
            .WithOne(h => h.LicenseMasterNav)
            .HasForeignKey(h => h.LicenseType)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 2: 確認 build 通過**

```powershell
dotnet build src/TCS.Infrastructure/TCS.Infrastructure.csproj
```
Expected: `Build succeeded.`

---

## Task 2: 修正 LicensePlantRequirementConfiguration（TCSMB）

**Files:**
- Modify: `src/TCS.Infrastructure/Configurations/LicensePlantRequirementConfiguration.cs`

- [ ] **Step 1: 完整替換 Configure 方法**

```csharp
// src/TCS.Infrastructure/Configurations/LicensePlantRequirementConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class LicensePlantRequirementConfiguration : IEntityTypeConfiguration<LicensePlantRequirement>
{
    public void Configure(EntityTypeBuilder<LicensePlantRequirement> builder)
    {
        builder.ToTable("TCSMB");
        builder.HasKey(e => new { e.LicenseType, e.Plant });

        builder.Property(e => e.LicenseType).HasColumnName("MB001").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Plant).HasColumnName("MB002").HasMaxLength(10).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.RequiredCount).HasColumnName("MB003");
        builder.Property(e => e.Creator).HasColumnName("CREATOR").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnName("CREATE_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnName("MODIFIER").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnName("MODI_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnName("FLAG").HasColumnType("decimal(3,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/TCS.Infrastructure/TCS.Infrastructure.csproj
```
Expected: `Build succeeded.`

---

## Task 3: 修正 TrainingHeaderConfiguration（TCSTA）

**Files:**
- Modify: `src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs`

> 注意：schema.json 的 TA005 = Remark、TA006 = Years（順序不是數字排列）。

- [ ] **Step 1: 完整替換 Configure 方法**

```csharp
// src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class TrainingHeaderConfiguration : IEntityTypeConfiguration<TrainingHeader>
{
    public void Configure(EntityTypeBuilder<TrainingHeader> builder)
    {
        builder.ToTable("TCSTA");
        builder.HasKey(e => new { e.EmployeeId, e.LicenseType });

        builder.Property(e => e.EmployeeId).HasColumnName("TA001").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.LicenseType).HasColumnName("TA002").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Plant).HasColumnName("TA003").HasMaxLength(6).IsFixedLength(true).IsUnicode(false).IsRequired(false);
        builder.Property(e => e.Hours).HasColumnName("TA004").HasColumnType("int");
        builder.Property(e => e.Remark).HasColumnName("TA005").HasMaxLength(200);
        builder.Property(e => e.Years).HasColumnName("TA006").HasColumnType("int").IsRequired(false);
        builder.Property(e => e.Creator).HasColumnName("CREATOR").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnName("CREATE_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnName("MODIFIER").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnName("MODI_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnName("FLAG").HasColumnType("decimal(3,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);

        builder.HasIndex(e => e.LicenseType).HasDatabaseName("IX_TCSTA_LicenseType");

        builder.HasOne(e => e.LicenseMasterNav)
            .WithMany(m => m.TrainingHeaders)
            .HasForeignKey(e => e.LicenseType)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Details)
            .WithOne()
            .HasForeignKey(d => new { d.EmployeeId, d.LicenseType })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/TCS.Infrastructure/TCS.Infrastructure.csproj
```
Expected: `Build succeeded.`

---

## Task 4: 修正 TrainingDetailConfiguration（TCSTB）

**Files:**
- Modify: `src/TCS.Infrastructure/Configurations/TrainingDetailConfiguration.cs`

- [ ] **Step 1: 完整替換 Configure 方法**

```csharp
// src/TCS.Infrastructure/Configurations/TrainingDetailConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class TrainingDetailConfiguration : IEntityTypeConfiguration<TrainingDetail>
{
    public void Configure(EntityTypeBuilder<TrainingDetail> builder)
    {
        builder.ToTable("TCSTB");
        builder.HasKey(e => new { e.EmployeeId, e.LicenseType, e.TrainingDate });

        builder.Property(e => e.EmployeeId).HasColumnName("TB001").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.LicenseType).HasColumnName("TB002").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.TrainingDate).HasColumnName("TB003").HasColumnType("date");
        builder.Property(e => e.TrainingType).HasColumnName("TB004");
        builder.Property(e => e.Hours).HasColumnName("TB005").HasColumnType("decimal(6,1)");
        builder.Property(e => e.Creator).HasColumnName("CREATOR").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnName("CREATE_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnName("MODIFIER").HasMaxLength(10).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnName("MODI_DATE").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnName("FLAG").HasColumnType("decimal(3,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);
    }
}
```

- [ ] **Step 2: Full solution build**

```powershell
dotnet build src/TCS.Web/TCS.Web.csproj
```
Expected: `Build succeeded.`

---

## Task 5: 重建 Migration Baseline

舊 migrations 是依錯誤欄位名建立的，全部刪除並以空白 Up() 重建 baseline，讓 `__EFMigrationsHistory` 記錄目前正確 model 狀態，不對資料庫執行任何 DDL。

**Files:**
- Delete: `src/TCS.Infrastructure/Migrations/` 下全部 `*.cs` 與 `*.Designer.cs`
- Create: 新的 `InitialCreate` migration（由 EF 工具產生，然後手動清空 Up()）

- [ ] **Step 1: 刪除所有舊 migration 檔**

```powershell
Remove-Item "src\TCS.Infrastructure\Migrations\*.cs" -Force
```

確認只剩空目錄（或不存在其他 .cs 檔）：

```powershell
Get-ChildItem "src\TCS.Infrastructure\Migrations\" -Filter "*.cs"
```
Expected: 無輸出（空目錄）。

- [ ] **Step 2: 產生新 InitialCreate migration**

```powershell
dotnet ef migrations add InitialCreate `
  --project src/TCS.Infrastructure `
  --startup-project src/TCS.Web `
  --output-dir Migrations
```

Expected: `Build succeeded.` + `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 3: 清空 Up() 方法（保留 Down() 不動）**

開啟剛產生的 `src/TCS.Infrastructure/Migrations/<timestamp>_InitialCreate.cs`，找到 `Up()` 方法，將其內容全部清除，只留空方法體：

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Database already exists with correct schema — no DDL needed.
}
```

Down() 保持 EF 自動產生的內容，不動。

- [ ] **Step 4: 套用 migration（只寫入 __EFMigrationsHistory，不執行 DDL）**

```powershell
dotnet ef database update `
  --project src/TCS.Infrastructure `
  --startup-project src/TCS.Web
```

Expected 最後幾行：
```
Applying migration '..._InitialCreate'.
Done.
```
若 `__EFMigrationsHistory` 不存在，EF 會建立它並插入一筆記錄。

- [ ] **Step 5: Commit**

```powershell
git add src/TCS.Infrastructure/Configurations/ src/TCS.Infrastructure/Migrations/
git commit -m "[Fix] 依 schema.json 修正 EF HasColumnName 映射並重建 Migration baseline"
```

---

## Task 6: 驗證 API 正常回傳

- [ ] **Step 1: 啟動應用程式**

```powershell
dotnet run --project src/TCS.Web
```

- [ ] **Step 2: 瀏覽器確認 200 OK**

開啟 `http://127.0.0.2:8180/tcs/api/licenses?page=1&pageSize=20`（附帶有效的 JWT Bearer token，或直接在有登入狀態的瀏覽器中訪問 License 頁面）。

Network tab 應顯示：
- Status: **200**
- Response body: `{"Items":[...],"TotalPages":...,"CurrentPage":1}`

- [ ] **Step 3: 同樣確認 training-headers**

訪問 `http://127.0.0.2:8180/tcs/Training`，Network tab 中 `api/training-headers?page=1&pageSize=20` 應顯示 **200**。

---

## Self-Review

### Spec Coverage
| Requirement | Task |
|---|---|
| TCSMA 欄位對應（MA001~MA005 + 系統欄） | Task 1 |
| TCSMB 欄位對應（MB001~MB003 + 系統欄） | Task 2 |
| TCSTA 欄位對應（TA001~TA006 + 系統欄，含 TA005=Remark/TA006=Years 順序） | Task 3 |
| TCSTB 欄位對應（TB001~TB005 + 系統欄） | Task 4 |
| FLAG 型別 decimal(1,0) → decimal(3,0) | Task 1-4（所有表） |
| 舊 migrations 刪除 + 新 baseline | Task 5 |
| 端對端 API 200 驗證 | Task 6 |
| schema.json 不被修改 | ✓（沒有任何 task 觸及 schema.json） |

### Placeholder Scan
無 TBD / TODO / "類似 Task N" / 缺少程式碼的步驟。

### Type Consistency
- 所有 `HasColumnName()` 的字串均直接來自 schema.json，逐欄交叉確認。
- `Flag` 全部改為 `decimal(3,0)` 且加上 `HasColumnName("FLAG")`，與 memory note 一致。
- TrainingHeader 的 `Remark → TA005`、`Years → TA006` 順序已在 Task 3 加入注意事項。
