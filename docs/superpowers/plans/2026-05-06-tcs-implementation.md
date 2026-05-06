# 受訓證件作業（TCS）實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 依 spec `docs/superpowers/specs/2026-05-06-tcs-design.md` 實作 TCS（受訓證件作業）系統，含 5 個資料表、6 個 module、Aspire 部署。

**Architecture:** Clean Architecture（Web ← Core → Infrastructure），複製 DingxinErpTemplate `demo/` 為起點，替換 Sample Entity 為 TCS Entity，新增 JWT 授權、ExpiryScanService、Aspire AppHost。

**Tech Stack:** .NET 8 / Razor Pages / EF Core / FluentValidation / xUnit / FluentAssertions / Moq / ClosedXML（Excel 匯出）/ .NET Aspire / SQL Server 2008 RTM

**Spec 對照：** 見 `docs/superpowers/specs/2026-05-06-tcs-design.md`，本計畫所有 Task 編號末尾以「→ §X-Y」標註對應 spec 章節。

---

## File Structure

```
src/
├── TCS.Core/
│   ├── Common/
│   │   ├── CrudResult.cs              ← 沿用 demo
│   │   ├── PagedResult.cs             ← 沿用 demo
│   │   ├── IAuditableEntity.cs        ← 沿用 demo
│   │   ├── IClock.cs                  ← 新增
│   │   ├── SystemClock.cs             ← 新增
│   │   └── TrainingType.cs            ← 新增 (enum)
│   ├── Entities/
│   │   ├── LicenseMaster.cs
│   │   ├── LicensePlantRequirement.cs
│   │   ├── TrainingHeader.cs
│   │   ├── TrainingDetail.cs
│   │   ├── Employee.cs                ← view
│   │   └── Plant.cs                   ← view
│   ├── DTOs/
│   │   ├── LicenseMasterDto.cs / Create/Update Request
│   │   ├── LicensePlantRequirementDto.cs / Create/Update Request
│   │   ├── TrainingHeaderDto.cs (含衍生欄位) / Create/Update Request
│   │   ├── TrainingDetailDto.cs / Create/Update Request
│   │   ├── EmployeeDto.cs / PlantDto.cs
│   │   └── MappingExtensions.cs
│   ├── Interfaces/
│   │   ├── ILicenseRepository.cs / ILicenseService.cs
│   │   ├── ITrainingRepository.cs / ITrainingService.cs
│   │   ├── IEmployeeRepository.cs / IPlantRepository.cs
│   │   ├── IExportService.cs
│   │   └── IExpiryCalculator.cs
│   ├── Services/
│   │   ├── LicenseService.cs
│   │   ├── TrainingService.cs
│   │   ├── ExpiryCalculator.cs
│   │   └── ExcelExportService.cs
│   └── Validators/
│       ├── CreateLicenseMasterValidator.cs / UpdateLicenseMasterValidator.cs
│       ├── CreateLicensePlantRequirementValidator.cs / Update...
│       ├── CreateTrainingHeaderValidator.cs / Update...
│       └── CreateTrainingDetailValidator.cs / Update...
├── TCS.Infrastructure/
│   ├── Data/AppDbContext.cs
│   ├── Configurations/
│   │   ├── LicenseMasterConfiguration.cs
│   │   ├── LicensePlantRequirementConfiguration.cs
│   │   ├── TrainingHeaderConfiguration.cs
│   │   ├── TrainingDetailConfiguration.cs
│   │   ├── EmployeeConfiguration.cs           ← ToView
│   │   └── PlantConfiguration.cs              ← ToView
│   ├── Repositories/
│   │   ├── LicenseRepository.cs
│   │   ├── TrainingRepository.cs
│   │   ├── EmployeeRepository.cs
│   │   └── PlantRepository.cs
│   └── BackgroundServices/
│       └── ExpiryScanService.cs
├── TCS.Web/
│   ├── Authorization/
│   │   ├── RequireActionAttribute.cs
│   │   └── RequireActionFilter.cs
│   ├── Controllers/
│   │   ├── LicenseController.cs
│   │   ├── LicensePlantRequirementController.cs
│   │   ├── TrainingController.cs
│   │   ├── TrainingDetailController.cs
│   │   ├── EmployeeController.cs
│   │   └── ExportController.cs
│   ├── Views/
│   │   ├── License/Index.cshtml
│   │   └── Training/Index.cshtml
│   ├── wwwroot/js/
│   │   ├── tcs-common.js   ← jwt parse, action check, toast, modal helpers
│   │   ├── license-page.js
│   │   └── training-page.js
│   ├── Middleware/ExceptionHandlingMiddleware.cs    ← 沿用 demo
│   ├── Program.cs
│   └── appsettings.json
├── TCS.AppHost/                          ← Aspire orchestrator
│   ├── Program.cs
│   └── TCS.AppHost.csproj
└── TCS.ServiceDefaults/                  ← Aspire shared defaults
    └── Extensions.cs

tests/
└── TCS.Tests/
    ├── Services/
    │   ├── ExpiryCalculatorTests.cs
    │   ├── LicenseServiceTests.cs
    │   └── TrainingServiceTests.cs
    ├── Validators/
    │   └── (各 validator 測試)
    └── BackgroundServices/
        └── ExpiryScanServiceTests.cs
```

---

## 階段總覽

| Phase | 任務 | 產出 |
|---|---|---|
| 0 | 專案初始化 | TCS.sln + 4 個 src 專案 + 1 個測試專案 + Aspire AppHost |
| 1 | 共用型別 | TrainingType enum、IClock、IAuditableEntity |
| 2 | Entities + EF Configurations | 5 張表 + 2 個 view |
| 3 | DTO + Mapping | 完整資料傳輸層 |
| 4 | Validators | FluentValidation 規則 |
| 5 | ExpiryCalculator | 週期計算與過期判定 (核心邏輯) |
| 6 | Repositories | DB 存取 |
| 7 | Services | 業務邏輯 |
| 8 | Authorization | RequireAction + JWT |
| 9 | Controllers | API 端點 |
| 10 | BackgroundService | ExpiryScanService |
| 11 | UI (Razor + JS) | 兩個頁面 |
| 12 | Aspire 整合 + 種子 | AppHost + InMemory seed |
| 13 | 端對端煙霧測試 | 完整 smoke test |

預計約 30-35 個 Task。每個 Task 包含若干 bite-sized 步驟。

---

## Phase 0：專案初始化

### Task 1: 建立 Solution 與專案骨架 → §12

**Files:**
- Create: `C:\Users\ian2213\source\repos\TCS\TCS.sln`
- Create: `src/TCS.Core/TCS.Core.csproj`
- Create: `src/TCS.Infrastructure/TCS.Infrastructure.csproj`
- Create: `src/TCS.Web/TCS.Web.csproj`
- Create: `src/TCS.AppHost/TCS.AppHost.csproj`
- Create: `src/TCS.ServiceDefaults/TCS.ServiceDefaults.csproj`
- Create: `tests/TCS.Tests/TCS.Tests.csproj`

- [ ] **Step 1: 在 `C:\Users\ian2213\source\repos\TCS\` 下建立目錄結構**

```bash
cd C:/Users/ian2213/source/repos/TCS
mkdir -p src tests
```

- [ ] **Step 2: 建立解決方案與各專案**

```bash
dotnet new sln -n TCS
dotnet new classlib -n TCS.Core -o src/TCS.Core -f net8.0
dotnet new classlib -n TCS.Infrastructure -o src/TCS.Infrastructure -f net8.0
dotnet new webapp -n TCS.Web -o src/TCS.Web -f net8.0
dotnet new xunit -n TCS.Tests -o tests/TCS.Tests -f net8.0

dotnet sln add src/TCS.Core src/TCS.Infrastructure src/TCS.Web tests/TCS.Tests
```

- [ ] **Step 3: 設定專案參考**

```bash
dotnet add src/TCS.Infrastructure reference src/TCS.Core
dotnet add src/TCS.Web reference src/TCS.Core src/TCS.Infrastructure
dotnet add tests/TCS.Tests reference src/TCS.Core src/TCS.Infrastructure src/TCS.Web
```

- [ ] **Step 4: 安裝 Core 層 NuGet 套件**

```bash
dotnet add src/TCS.Core package FluentValidation --version 11.9.0
```

- [ ] **Step 5: 安裝 Infrastructure 層 NuGet 套件**

```bash
dotnet add src/TCS.Infrastructure package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add src/TCS.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add src/TCS.Infrastructure package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0
dotnet add src/TCS.Infrastructure package Microsoft.Extensions.Hosting.Abstractions --version 8.0.0
dotnet add src/TCS.Infrastructure package ClosedXML --version 0.102.2
```

- [ ] **Step 6: 安裝 Web 層 NuGet 套件**

```bash
dotnet add src/TCS.Web package FluentValidation.AspNetCore --version 11.3.0
dotnet add src/TCS.Web package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add src/TCS.Web package Swashbuckle.AspNetCore --version 6.5.0
```

- [ ] **Step 7: 安裝測試套件**

```bash
dotnet add tests/TCS.Tests package FluentAssertions --version 6.12.0
dotnet add tests/TCS.Tests package Moq --version 4.20.70
dotnet add tests/TCS.Tests package Microsoft.EntityFrameworkCore.InMemory --version 8.0.0
```

- [ ] **Step 8: 建置驗證**

```bash
dotnet build
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 9: 初始化 git 與首次 commit**

```bash
git init
git add .
git commit -m "chore: initial solution scaffold"
```

### Task 2: 加入 Aspire AppHost 與 ServiceDefaults → §12

**Files:**
- Create: `src/TCS.AppHost/TCS.AppHost.csproj`
- Create: `src/TCS.AppHost/Program.cs`
- Create: `src/TCS.ServiceDefaults/TCS.ServiceDefaults.csproj`
- Create: `src/TCS.ServiceDefaults/Extensions.cs`

- [ ] **Step 1: 安裝 Aspire workload**

```bash
dotnet workload install aspire
```
Expected: `Successfully installed workload(s) aspire.`

- [ ] **Step 2: 建立 AppHost 與 ServiceDefaults 專案**

```bash
dotnet new aspire-apphost -n TCS.AppHost -o src/TCS.AppHost
dotnet new aspire-servicedefaults -n TCS.ServiceDefaults -o src/TCS.ServiceDefaults
dotnet sln add src/TCS.AppHost src/TCS.ServiceDefaults
```

- [ ] **Step 3: 設定 AppHost 參考 Web 專案**

```bash
dotnet add src/TCS.AppHost reference src/TCS.Web
dotnet add src/TCS.Web reference src/TCS.ServiceDefaults
```

- [ ] **Step 4: 編輯 `src/TCS.AppHost/Program.cs` 加入 TCS.Web，固定 1 副本**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TCS_Web>("tcs-web")
    .WithReplicas(1); // 因 ExpiryScanService 為定時任務，禁止多副本

builder.Build().Run();
```

- [ ] **Step 5: 建置驗證**

```bash
dotnet build
```
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add .
git commit -m "feat: add Aspire AppHost and ServiceDefaults"
```

---

## Phase 1：共用型別

### Task 3: 建立 IAuditableEntity / CrudResult / PagedResult → §10

**Files:**
- Create: `src/TCS.Core/Common/IAuditableEntity.cs`
- Create: `src/TCS.Core/Common/CrudResult.cs`
- Create: `src/TCS.Core/Common/PagedResult.cs`

- [ ] **Step 1: 建立 `IAuditableEntity.cs`**

```csharp
namespace TCS.Core.Common;

/// <summary>審計欄位介面，沿襲鼎新 ERP 慣例</summary>
public interface IAuditableEntity
{
    string? Creator { get; set; }
    string? CreateDate { get; set; }
    string? Modifier { get; set; }
    string? ModiDate { get; set; }
    decimal? Flag { get; set; }
}
```

- [ ] **Step 2: 建立 `CrudResult.cs`**

```csharp
namespace TCS.Core.Common;

public class CrudResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static CrudResult<T> SuccessResult(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static CrudResult<T> ErrorResult(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}
```

- [ ] **Step 3: 建立 `PagedResult.cs`**

```csharp
namespace TCS.Core.Common;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public string? SearchString { get; set; }
}
```

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Core/Common/
git commit -m "feat: add common types (IAuditableEntity, CrudResult, PagedResult)"
```

### Task 4: 建立 IClock / SystemClock / TrainingType enum → §4-4, §8-4

**Files:**
- Create: `src/TCS.Core/Common/IClock.cs`
- Create: `src/TCS.Core/Common/SystemClock.cs`
- Create: `src/TCS.Core/Common/TrainingType.cs`
- Create: `tests/TCS.Tests/Common/SystemClockTests.cs`

- [ ] **Step 1: 建立 `TrainingType.cs`**

```csharp
namespace TCS.Core.Common;

/// <summary>受訓類型（對應 spec §4-4）</summary>
public enum TrainingType : byte
{
    /// <summary>取得證照（首次或更新版本取得）</summary>
    取得證照 = 1,
    /// <summary>回訓</summary>
    回訓 = 2
}
```

- [ ] **Step 2: 建立 `IClock.cs`**

```csharp
namespace TCS.Core.Common;

/// <summary>時鐘抽象，方便測試注入 FakeClock 推進時間</summary>
public interface IClock
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }   // Asia/Taipei
}
```

- [ ] **Step 3: 建立 `SystemClock.cs`**

```csharp
namespace TCS.Core.Common;

public class SystemClock : IClock
{
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime LocalNow => TimeZoneInfo.ConvertTime(DateTime.UtcNow, TaipeiTz);
}
```

- [ ] **Step 4: 寫 SystemClock 測試**

```csharp
using FluentAssertions;
using TCS.Core.Common;
using Xunit;

namespace TCS.Tests.Common;

public class SystemClockTests
{
    [Fact]
    public void LocalNow_should_be_8_hours_ahead_of_UtcNow()
    {
        var clock = new SystemClock();
        var diff = clock.LocalNow - clock.UtcNow;
        diff.TotalHours.Should().BeApproximately(8, 0.01);
    }
}
```

- [ ] **Step 5: 執行測試**

```bash
dotnet test tests/TCS.Tests/TCS.Tests.csproj --filter SystemClockTests
```
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Core/Common/ tests/TCS.Tests/Common/
git commit -m "feat: add IClock abstraction and TrainingType enum"
```

---

## Phase 2：Entities + EF Configurations

### Task 5: LicenseMaster + LicensePlantRequirement Entities & Configurations → §4-1, §4-2

**Files:**
- Create: `src/TCS.Core/Entities/LicenseMaster.cs`
- Create: `src/TCS.Core/Entities/LicensePlantRequirement.cs`
- Create: `src/TCS.Infrastructure/Configurations/LicenseMasterConfiguration.cs`
- Create: `src/TCS.Infrastructure/Configurations/LicensePlantRequirementConfiguration.cs`

- [ ] **Step 1: 建立 `LicenseMaster.cs`**

```csharp
using TCS.Core.Common;

namespace TCS.Core.Entities;

public class LicenseMaster : IAuditableEntity
{
    public string LicenseType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Category { get; set; }
    public int? Hours { get; set; }
    public int? Years { get; set; }

    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    public LicenseMaster? ParentCategory { get; set; }
    public ICollection<LicensePlantRequirement> PlantRequirements { get; set; } = new List<LicensePlantRequirement>();
    public ICollection<TrainingHeader> TrainingHeaders { get; set; } = new List<TrainingHeader>();
}
```

- [ ] **Step 2: 建立 `LicensePlantRequirement.cs`**

```csharp
using TCS.Core.Common;

namespace TCS.Core.Entities;

public class LicensePlantRequirement : IAuditableEntity
{
    public string LicenseType { get; set; } = null!;
    public string Plant { get; set; } = null!;
    public int RequiredCount { get; set; }

    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    public LicenseMaster LicenseMasterNav { get; set; } = null!;
}
```

- [ ] **Step 3: 建立 `LicenseMasterConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class LicenseMasterConfiguration : IEntityTypeConfiguration<LicenseMaster>
{
    public void Configure(EntityTypeBuilder<LicenseMaster> builder)
    {
        builder.ToTable("LicenseMaster");
        builder.HasKey(e => e.LicenseType);

        builder.Property(e => e.LicenseType).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Description).HasColumnType("nvarchar(70)").IsRequired();
        builder.Property(e => e.Category).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Creator).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnType("char(8)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnType("char(8)").IsFixedLength().IsUnicode(false);

        builder.HasOne(e => e.ParentCategory)
            .WithMany()
            .HasForeignKey(e => e.Category)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.PlantRequirements)
            .WithOne(e => e.LicenseMasterNav)
            .HasForeignKey(e => e.LicenseType)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.TrainingHeaders)
            .WithOne(e => e.LicenseMasterNav)
            .HasForeignKey(e => e.LicenseType)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 4: 建立 `LicensePlantRequirementConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class LicensePlantRequirementConfiguration : IEntityTypeConfiguration<LicensePlantRequirement>
{
    public void Configure(EntityTypeBuilder<LicensePlantRequirement> builder)
    {
        builder.ToTable("LicensePlantRequirement");
        builder.HasKey(e => new { e.LicenseType, e.Plant });

        builder.Property(e => e.LicenseType).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Plant).HasColumnType("char(6)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.RequiredCount).IsRequired();
        builder.Property(e => e.Creator).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnType("char(8)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnType("char(8)").IsFixedLength().IsUnicode(false);
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Entities/ src/TCS.Infrastructure/Configurations/
git commit -m "feat: add LicenseMaster and LicensePlantRequirement entities"
```

---

### Task 6: TrainingHeader + TrainingDetail Entities & Configurations → §4-3, §4-4

**Files:**
- Create: `src/TCS.Core/Entities/TrainingHeader.cs`
- Create: `src/TCS.Core/Entities/TrainingDetail.cs`
- Create: `src/TCS.Infrastructure/Configurations/TrainingHeaderConfiguration.cs`
- Create: `src/TCS.Infrastructure/Configurations/TrainingDetailConfiguration.cs`

- [ ] **Step 1: 建立 `TrainingHeader.cs`**

```csharp
using TCS.Core.Common;

namespace TCS.Core.Entities;

public class TrainingHeader : IAuditableEntity
{
    public string EmployeeId { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public int RequiredHours { get; set; }
    public string? Remark { get; set; }

    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    public LicenseMaster LicenseMasterNav { get; set; } = null!;
    public ICollection<TrainingDetail> Details { get; set; } = new List<TrainingDetail>();
}
```

- [ ] **Step 2: 建立 `TrainingDetail.cs`**

```csharp
using TCS.Core.Common;

namespace TCS.Core.Entities;

public class TrainingDetail : IAuditableEntity
{
    public string EmployeeId { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public DateTime TrainingDate { get; set; }
    public byte TrainingType { get; set; }   // 1=取得證照, 2=回訓
    public bool IsExpired { get; set; }
    public decimal Hours { get; set; }

    public string? Creator { get; set; }
    public string? CreateDate { get; set; }
    public string? Modifier { get; set; }
    public string? ModiDate { get; set; }
    public decimal? Flag { get; set; }

    public TrainingHeader Header { get; set; } = null!;
}
```

- [ ] **Step 3: 建立 `TrainingHeaderConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class TrainingHeaderConfiguration : IEntityTypeConfiguration<TrainingHeader>
{
    public void Configure(EntityTypeBuilder<TrainingHeader> builder)
    {
        builder.ToTable("TrainingHeader");
        builder.HasKey(e => new { e.EmployeeId, e.LicenseType });

        builder.Property(e => e.EmployeeId).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.LicenseType).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.RequiredHours).IsRequired();
        builder.Property(e => e.Remark).HasColumnType("nvarchar(70)");
        builder.Property(e => e.Creator).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnType("char(8)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnType("char(8)").IsFixedLength().IsUnicode(false);

        builder.HasMany(e => e.Details)
            .WithOne(e => e.Header)
            .HasForeignKey(e => new { e.EmployeeId, e.LicenseType })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 4: 建立 `TrainingDetailConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class TrainingDetailConfiguration : IEntityTypeConfiguration<TrainingDetail>
{
    public void Configure(EntityTypeBuilder<TrainingDetail> builder)
    {
        builder.ToTable("TrainingDetail");
        builder.HasKey(e => new { e.EmployeeId, e.LicenseType, e.TrainingDate });

        builder.Property(e => e.EmployeeId).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.LicenseType).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.TrainingDate).HasColumnType("date");
        builder.Property(e => e.TrainingType).HasColumnType("tinyint").IsRequired();
        builder.Property(e => e.IsExpired).HasColumnType("bit").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.Hours).HasColumnType("decimal(5,1)").IsRequired();
        builder.Property(e => e.Creator).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.CreateDate).HasColumnType("char(8)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Modifier).HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.ModiDate).HasColumnType("char(8)").IsFixedLength().IsUnicode(false);
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Entities/ src/TCS.Infrastructure/Configurations/
git commit -m "feat: add TrainingHeader and TrainingDetail entities"
```

---

### Task 7: Employee + Plant View Entities & Configurations → §4-5

**Files:**
- Create: `src/TCS.Core/Entities/Employee.cs`
- Create: `src/TCS.Core/Entities/Plant.cs`
- Create: `src/TCS.Infrastructure/Configurations/EmployeeConfiguration.cs`
- Create: `src/TCS.Infrastructure/Configurations/PlantConfiguration.cs`

- [ ] **Step 1: 建立 `Employee.cs`**

```csharp
namespace TCS.Core.Entities;

/// <summary>唯讀 View 映射，不實作 IAuditableEntity，禁止寫入</summary>
public class Employee
{
    public string EmployeeId { get; set; } = null!;  // MV001
    public string Name { get; set; } = null!;         // MV002
    public string Department { get; set; } = null!;   // MV004
    public string HireDate { get; set; } = null!;     // MV021
}
```

- [ ] **Step 2: 建立 `Plant.cs`**

```csharp
namespace TCS.Core.Entities;

/// <summary>唯讀 View 映射 CMSMB，禁止寫入</summary>
public class Plant
{
    public string PlantCode { get; set; } = null!;  // MB001
    public string? PlantName { get; set; }           // MB002
}
```

- [ ] **Step 3: 建立 `EmployeeConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToView("CMSMV");   // 既有 ERP view 名稱，待確認 §15 Open Item 4
        builder.HasKey(e => e.EmployeeId);

        builder.Property(e => e.EmployeeId).HasColumnName("MV001").HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Name).HasColumnName("MV002").HasColumnType("char(10)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.Department).HasColumnName("MV004").HasColumnType("char(6)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.HireDate).HasColumnName("MV021").HasColumnType("char(8)").IsFixedLength().IsUnicode(false);
    }
}
```

- [ ] **Step 4: 建立 `PlantConfiguration.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.ToView("CMSMB");
        builder.HasKey(e => e.PlantCode);

        builder.Property(e => e.PlantCode).HasColumnName("MB001").HasColumnType("char(6)").IsFixedLength().IsUnicode(false);
        builder.Property(e => e.PlantName).HasColumnName("MB002");   // 長度待確認 §15 Open Item 6
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Entities/ src/TCS.Infrastructure/Configurations/
git commit -m "feat: add Employee and Plant view entities"
```

---

### Task 8: AppDbContext → §12

**Files:**
- Create: `src/TCS.Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: 建立 `AppDbContext.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LicenseMaster> LicenseMasters => Set<LicenseMaster>();
    public DbSet<LicensePlantRequirement> LicensePlantRequirements => Set<LicensePlantRequirement>();
    public DbSet<TrainingHeader> TrainingHeaders => Set<TrainingHeader>();
    public DbSet<TrainingDetail> TrainingDetails => Set<TrainingDetail>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Plant> Plants => Set<Plant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 2: 建置驗證**

```bash
dotnet build src/TCS.Infrastructure
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add src/TCS.Infrastructure/Data/
git commit -m "feat: add AppDbContext"
```

---

### Task 9: EF Migration → §12

**Files:**
- Create: `src/TCS.Infrastructure/Migrations/` (自動產生)

- [ ] **Step 1: 安裝 EF Core Tools（若尚未安裝）**

```bash
dotnet tool install --global dotnet-ef
```
Expected: `You can invoke the tool using the following command: dotnet-ef`（已安裝則顯示 already installed）

- [ ] **Step 2: 新增 Migration**

```bash
dotnet ef migrations add InitialCreate \
  --project src/TCS.Infrastructure \
  --startup-project src/TCS.Web \
  -- --environment Development
```
Expected: `Done. To undo this action, use 'ef migrations remove'`

> **注意**：Employee 與 Plant 使用 `ToView()`，EF 不會為它們產生建表 SQL，Migration 中不會出現 `CMSMV` / `CMSMB`。

- [ ] **Step 3: 確認 Migration 內容不含 view 表**

```bash
dotnet ef migrations script --project src/TCS.Infrastructure --startup-project src/TCS.Web
```
Expected：輸出中應有 `CREATE TABLE [LicenseMaster]`、`CREATE TABLE [TrainingHeader]` 等，但**不應有** `CMSMV` 或 `CMSMB`。

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Infrastructure/Migrations/
git commit -m "feat: add EF InitialCreate migration"
```

---

## Phase 3：DTO + Mapping

### Task 10: 建立所有 DTOs → §4, §4-6

**Files:**
- Create: `src/TCS.Core/DTOs/LicenseMasterDto.cs`
- Create: `src/TCS.Core/DTOs/LicensePlantRequirementDto.cs`
- Create: `src/TCS.Core/DTOs/TrainingHeaderDto.cs`
- Create: `src/TCS.Core/DTOs/TrainingDetailDto.cs`
- Create: `src/TCS.Core/DTOs/EmployeeDto.cs`
- Create: `src/TCS.Core/DTOs/PlantDto.cs`

- [ ] **Step 1: 建立 `LicenseMasterDto.cs`**

```csharp
namespace TCS.Core.DTOs;

public class LicenseMasterDto
{
    public string LicenseType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Category { get; set; }
    public int? Hours { get; set; }
    public int? Years { get; set; }
}

public class CreateLicenseMasterRequest
{
    public string LicenseType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Category { get; set; }
    public int? Hours { get; set; }
    public int? Years { get; set; }
}

public class UpdateLicenseMasterRequest
{
    public string Description { get; set; } = null!;
    public string? Category { get; set; }
    public int? Hours { get; set; }
    public int? Years { get; set; }
}
```

- [ ] **Step 2: 建立 `LicensePlantRequirementDto.cs`**

```csharp
namespace TCS.Core.DTOs;

public class LicensePlantRequirementDto
{
    public string LicenseType { get; set; } = null!;
    public string Plant { get; set; } = null!;
    public string? PlantName { get; set; }
    public int RequiredCount { get; set; }
}

public class CreateLicensePlantRequirementRequest
{
    public string Plant { get; set; } = null!;
    public int RequiredCount { get; set; }
}

public class UpdateLicensePlantRequirementRequest
{
    public int RequiredCount { get; set; }
}
```

- [ ] **Step 3: 建立 `TrainingHeaderDto.cs`**（含衍生欄位 §4-6）

```csharp
namespace TCS.Core.DTOs;

public class TrainingHeaderDto
{
    public string EmployeeId { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public int RequiredHours { get; set; }
    public string? Remark { get; set; }

    // Employee fields
    public string? EmployeeName { get; set; }
    public string? Department { get; set; }
    public string? HireDate { get; set; }

    // LicenseMaster fields
    public string? LicenseDescription { get; set; }

    // Computed (§4-6)
    public DateTime? LatestAcquireDate { get; set; }
    public DateTime? LatestRetrainDate { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public decimal AccumulatedHours { get; set; }
    public decimal RemainingHours { get; set; }
    public string OverallStatus { get; set; } = null!;  // 未取得/通過/進行中/已過期
}

public class CreateTrainingHeaderRequest
{
    public string EmployeeId { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public string? Remark { get; set; }
    // RequiredHours intentionally excluded — system fills it
}

public class UpdateTrainingHeaderRequest
{
    public string? Remark { get; set; }
    // Only Remark is editable per §7-3
}
```

- [ ] **Step 4: 建立 `TrainingDetailDto.cs`**

```csharp
namespace TCS.Core.DTOs;

public class TrainingDetailDto
{
    public string EmployeeId { get; set; } = null!;
    public string LicenseType { get; set; } = null!;
    public DateTime TrainingDate { get; set; }
    public byte TrainingType { get; set; }
    public string TrainingTypeLabel => TrainingType == 1 ? "取得證照" : "回訓";
    public bool IsExpired { get; set; }
    public decimal Hours { get; set; }
}

public class CreateTrainingDetailRequest
{
    public DateTime TrainingDate { get; set; }
    public byte TrainingType { get; set; }
    public decimal Hours { get; set; }
}

public class UpdateTrainingDetailRequest
{
    public byte TrainingType { get; set; }
    public decimal Hours { get; set; }
}
```

- [ ] **Step 5: 建立 `EmployeeDto.cs` + `PlantDto.cs`**

```csharp
// EmployeeDto.cs
namespace TCS.Core.DTOs;

public class EmployeeDto
{
    public string EmployeeId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Department { get; set; } = null!;
    public string HireDate { get; set; } = null!;
}
```

```csharp
// PlantDto.cs
namespace TCS.Core.DTOs;

public class PlantDto
{
    public string PlantCode { get; set; } = null!;
    public string? PlantName { get; set; }
}
```

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Core/DTOs/
git commit -m "feat: add all DTOs"
```

---

### Task 11: MappingExtensions → §4-6

**Files:**
- Create: `src/TCS.Core/DTOs/MappingExtensions.cs`

- [ ] **Step 1: 建立 `MappingExtensions.cs`**

```csharp
using TCS.Core.Entities;

namespace TCS.Core.DTOs;

public static class MappingExtensions
{
    public static LicenseMasterDto ToDto(this LicenseMaster e) => new()
    {
        LicenseType = e.LicenseType.Trim(),
        Description = e.Description,
        Category    = e.Category?.Trim(),
        Hours       = e.Hours,
        Years       = e.Years
    };

    public static LicensePlantRequirementDto ToDto(this LicensePlantRequirement e, string? plantName = null) => new()
    {
        LicenseType   = e.LicenseType.Trim(),
        Plant         = e.Plant.Trim(),
        PlantName     = plantName,
        RequiredCount = e.RequiredCount
    };

    public static TrainingDetailDto ToDto(this TrainingDetail e) => new()
    {
        EmployeeId   = e.EmployeeId.Trim(),
        LicenseType  = e.LicenseType.Trim(),
        TrainingDate = e.TrainingDate,
        TrainingType = e.TrainingType,
        IsExpired    = e.IsExpired,
        Hours        = e.Hours
    };

    public static EmployeeDto ToDto(this Employee e) => new()
    {
        EmployeeId = e.EmployeeId.Trim(),
        Name       = e.Name.Trim(),
        Department = e.Department.Trim(),
        HireDate   = e.HireDate.Trim()
    };

    public static PlantDto ToDto(this Plant e) => new()
    {
        PlantCode = e.PlantCode.Trim(),
        PlantName = e.PlantName?.Trim()
    };

    /// <summary>
    /// TrainingHeader → DTO 需要外部傳入衍生欄位（由 Service 層計算後傳入）
    /// </summary>
    public static TrainingHeaderDto ToDto(
        this TrainingHeader h,
        string?   employeeName,
        string?   department,
        string?   hireDate,
        string?   licenseDescription,
        DateTime? latestAcquireDate,
        DateTime? latestRetrainDate,
        decimal   accumulatedHours,
        int       requiredHours) => new()
    {
        EmployeeId          = h.EmployeeId.Trim(),
        LicenseType         = h.LicenseType.Trim(),
        RequiredHours       = requiredHours,
        Remark              = h.Remark,
        EmployeeName        = employeeName?.Trim(),
        Department          = department?.Trim(),
        HireDate            = hireDate?.Trim(),
        LicenseDescription  = licenseDescription,
        LatestAcquireDate   = latestAcquireDate,
        LatestRetrainDate   = latestRetrainDate,
        NextReviewDate      = latestAcquireDate.HasValue && h.LicenseMasterNav?.Years != null
                                ? latestAcquireDate.Value.AddYears(h.LicenseMasterNav.Years.Value)
                                : null,
        AccumulatedHours    = accumulatedHours,
        RemainingHours      = Math.Max(0, requiredHours - accumulatedHours),
        OverallStatus       = ComputeStatus(latestAcquireDate, accumulatedHours, requiredHours,
                                            latestAcquireDate.HasValue && h.LicenseMasterNav?.Years != null
                                                ? latestAcquireDate.Value.AddYears(h.LicenseMasterNav.Years.Value)
                                                : null,
                                            h.Details)
    };

    private static string ComputeStatus(
        DateTime? latestAcquireDate,
        decimal   accumulated,
        int       required,
        DateTime? nextReviewDate,
        IEnumerable<TrainingDetail> details)
    {
        if (!latestAcquireDate.HasValue) return "未取得";
        if (details.Any(d => d.IsExpired)) return "已過期";
        if (accumulated >= required) return "通過";
        if (nextReviewDate.HasValue && nextReviewDate.Value >= DateTime.Today) return "進行中";
        return "已過期";
    }
}
```

- [ ] **Step 2: 建置驗證**

```bash
dotnet build src/TCS.Core
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add src/TCS.Core/DTOs/MappingExtensions.cs
git commit -m "feat: add MappingExtensions"
```

---

## Phase 4：Validators

### Task 12: License Validators → §9

**Files:**
- Create: `src/TCS.Core/Validators/CreateLicenseMasterValidator.cs`
- Create: `src/TCS.Core/Validators/UpdateLicenseMasterValidator.cs`
- Create: `src/TCS.Core/Validators/CreateLicensePlantRequirementValidator.cs`
- Create: `src/TCS.Core/Validators/UpdateLicensePlantRequirementValidator.cs`
- Create: `src/TCS.Core/Interfaces/ILicenseRepository.cs` (引用介面，先建空殼)

> ILicenseRepository 的完整定義在 Task 16 補齊；此處只需 `ExistsByTypeAsync` 與 `IsSubTypeAsync`，提前宣告以便 Validator 注入。

- [ ] **Step 1: 建立 `ILicenseRepository.cs` 驗證用最小介面**

```csharp
namespace TCS.Core.Interfaces;

public interface ILicenseRepository
{
    Task<bool> ExistsByTypeAsync(string licenseType, CancellationToken ct = default);
    Task<bool> IsSubTypeAsync(string licenseType, CancellationToken ct = default);
    Task<bool> IsCategoryTypeAsync(string licenseType, CancellationToken ct = default);
    Task<bool> PlantRequirementExistsAsync(string licenseType, string plant, CancellationToken ct = default);
}
```

- [ ] **Step 2: 建立 `CreateLicenseMasterValidator.cs`**

```csharp
using FluentValidation;
using TCS.Core.DTOs;
using TCS.Core.Interfaces;

namespace TCS.Core.Validators;

public class CreateLicenseMasterValidator : AbstractValidator<CreateLicenseMasterRequest>
{
    // 純整數 or 含小數點之合法數字（如 1.1, 2.3.1）
    private static readonly System.Text.RegularExpressions.Regex LicenseTypeRegex =
        new(@"^\d+(\.\d+)*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public CreateLicenseMasterValidator(ILicenseRepository repo)
    {
        RuleFor(x => x.LicenseType)
            .NotEmpty().WithMessage("證照類別為必填")
            .MaximumLength(10).WithMessage("證照類別不可超過 10 碼")
            .Matches(LicenseTypeRegex).WithMessage("證照類別格式錯誤，須為純整數（大類）或含小數點數字（小類）")
            .MustAsync(async (t, ct) => !await repo.ExistsByTypeAsync(t, ct))
                .WithMessage("證照類別已存在");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("描述為必填")
            .MaximumLength(70).WithMessage("描述不可超過 70 字");

        // 大類（純整數）→ Category 必須為 null
        When(x => !x.LicenseType.Contains('.'), () =>
        {
            RuleFor(x => x.Category)
                .Null().WithMessage("大類列的對應大類必須為空");
        });

        // 小類（含小數點）→ Category 必填且須對應存在的大類
        When(x => x.LicenseType.Contains('.'), () =>
        {
            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("小類列的對應大類為必填")
                .MaximumLength(10)
                .MustAsync(async (cat, ct) => cat != null && await repo.IsCategoryTypeAsync(cat, ct))
                    .WithMessage("對應大類不存在或本身為小類");

            RuleFor(x => x.Hours)
                .NotNull().WithMessage("小類列的時數為必填")
                .GreaterThan(0).WithMessage("時數必須大於 0");

            RuleFor(x => x.Years)
                .NotNull().WithMessage("小類列的年數為必填")
                .GreaterThanOrEqualTo(1).WithMessage("年數至少為 1");
        });
    }
}
```

- [ ] **Step 3: 建立 `UpdateLicenseMasterValidator.cs`**

```csharp
using FluentValidation;
using TCS.Core.DTOs;
using TCS.Core.Interfaces;

namespace TCS.Core.Validators;

public class UpdateLicenseMasterValidator : AbstractValidator<UpdateLicenseMasterRequest>
{
    public UpdateLicenseMasterValidator(ILicenseRepository repo)
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("描述為必填")
            .MaximumLength(70).WithMessage("描述不可超過 70 字");
        // Category / Hours / Years 規則與 Create 相同，由外層傳入 licenseType 判斷
        // 具體大類/小類判斷在 LicenseService.UpdateAsync 中執行
    }
}
```

- [ ] **Step 4: 建立 `CreateLicensePlantRequirementValidator.cs`**

```csharp
using FluentValidation;
using TCS.Core.DTOs;
using TCS.Core.Interfaces;

namespace TCS.Core.Validators;

public class CreateLicensePlantRequirementValidator : AbstractValidator<CreateLicensePlantRequirementRequest>
{
    public CreateLicensePlantRequirementValidator(ILicenseRepository repo, string licenseType)
    {
        RuleFor(x => x.Plant)
            .NotEmpty().WithMessage("廠別為必填")
            .MaximumLength(6).WithMessage("廠別不可超過 6 碼")
            .MustAsync(async (plant, ct) => !await repo.PlantRequirementExistsAsync(licenseType, plant, ct))
                .WithMessage("此廠別需求已存在");

        RuleFor(x => x.RequiredCount)
            .GreaterThanOrEqualTo(0).WithMessage("需求數不可為負數");
    }
}
```

- [ ] **Step 5: 建立 `UpdateLicensePlantRequirementValidator.cs`**

```csharp
using FluentValidation;
using TCS.Core.DTOs;

namespace TCS.Core.Validators;

public class UpdateLicensePlantRequirementValidator : AbstractValidator<UpdateLicensePlantRequirementRequest>
{
    public UpdateLicensePlantRequirementValidator()
    {
        RuleFor(x => x.RequiredCount)
            .GreaterThanOrEqualTo(0).WithMessage("需求數不可為負數");
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Core/Interfaces/ src/TCS.Core/Validators/
git commit -m "feat: add license validators"
```

---

### Task 13: Training Validators → §9

**Files:**
- Create: `src/TCS.Core/Interfaces/ITrainingRepository.cs` (最小介面)
- Create: `src/TCS.Core/Interfaces/IEmployeeRepository.cs` (最小介面)
- Create: `src/TCS.Core/Validators/CreateTrainingHeaderValidator.cs`
- Create: `src/TCS.Core/Validators/UpdateTrainingHeaderValidator.cs`
- Create: `src/TCS.Core/Validators/CreateTrainingDetailValidator.cs`
- Create: `src/TCS.Core/Validators/UpdateTrainingDetailValidator.cs`

- [ ] **Step 1: 建立 `ITrainingRepository.cs` 驗證用最小介面**

```csharp
namespace TCS.Core.Interfaces;

public interface ITrainingRepository
{
    Task<bool> HeaderExistsAsync(string empId, string licType, CancellationToken ct = default);
    Task<bool> DetailExistsAsync(string empId, string licType, DateTime date, CancellationToken ct = default);
}
```

- [ ] **Step 2: 建立 `IEmployeeRepository.cs`**

```csharp
namespace TCS.Core.Interfaces;

public interface IEmployeeRepository
{
    Task<bool> ExistsAsync(string employeeId, CancellationToken ct = default);
}
```

- [ ] **Step 3: 建立 `CreateTrainingHeaderValidator.cs`**

```csharp
using FluentValidation;
using TCS.Core.DTOs;
using TCS.Core.Interfaces;

namespace TCS.Core.Validators;

public class CreateTrainingHeaderValidator : AbstractValidator<CreateTrainingHeaderRequest>
{
    public CreateTrainingHeaderValidator(IEmployeeRepository empRepo, ILicenseRepository licRepo)
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("員工編號為必填")
            .MustAsync(async (id, ct) => await empRepo.ExistsAsync(id, ct))
                .WithMessage("員工編號不存在");

        RuleFor(x => x.LicenseType)
            .NotEmpty().WithMessage("證照類別為必填")
            .MustAsync(async (lt, ct) => await licRepo.IsSubTypeAsync(lt, ct))
                .WithMessage("證照類別必須為小類（含小數點）且存在");

        RuleFor(x => x.Remark)
            .MaximumLength(70).WithMessage("備註不可超過 70 字");
    }
}
```

- [ ] **Step 4: 建立 `UpdateTrainingHeaderValidator.cs`**

```csharp
using FluentValidation;
using TCS.Core.DTOs;

namespace TCS.Core.Validators;

public class UpdateTrainingHeaderValidator : AbstractValidator<UpdateTrainingHeaderRequest>
{
    public UpdateTrainingHeaderValidator()
    {
        RuleFor(x => x.Remark)
            .MaximumLength(70).WithMessage("備註不可超過 70 字");
    }
}
```

- [ ] **Step 5: 建立 `CreateTrainingDetailValidator.cs`**

```csharp
using FluentValidation;
using TCS.Core.DTOs;
using TCS.Core.Interfaces;

namespace TCS.Core.Validators;

public class CreateTrainingDetailValidator : AbstractValidator<CreateTrainingDetailRequest>
{
    public CreateTrainingDetailValidator(ITrainingRepository trainingRepo, string empId, string licType)
    {
        RuleFor(x => x.TrainingDate)
            .NotEmpty().WithMessage("受訓日期為必填")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("受訓日期不可為未來日期")
            .MustAsync(async (date, ct) => !await trainingRepo.DetailExistsAsync(empId, licType, date, ct))
                .WithMessage("此受訓日期記錄已存在");

        RuleFor(x => x.TrainingType)
            .InclusiveBetween((byte)1, (byte)2).WithMessage("受訓類型必須為 1（取得證照）或 2（回訓）");

        RuleFor(x => x.Hours)
            .GreaterThan(0m).WithMessage("時數必須大於 0")
            .LessThanOrEqualTo(9999.9m).WithMessage("時數不可超過 9999.9");
    }
}
```

- [ ] **Step 6: 建立 `UpdateTrainingDetailValidator.cs`**

```csharp
using FluentValidation;
using TCS.Core.DTOs;

namespace TCS.Core.Validators;

public class UpdateTrainingDetailValidator : AbstractValidator<UpdateTrainingDetailRequest>
{
    public UpdateTrainingDetailValidator()
    {
        RuleFor(x => x.TrainingType)
            .InclusiveBetween((byte)1, (byte)2).WithMessage("受訓類型必須為 1（取得證照）或 2（回訓）");

        RuleFor(x => x.Hours)
            .GreaterThan(0m).WithMessage("時數必須大於 0")
            .LessThanOrEqualTo(9999.9m).WithMessage("時數不可超過 9999.9");
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add src/TCS.Core/Interfaces/ src/TCS.Core/Validators/
git commit -m "feat: add training validators and repository interfaces"
```

---

## Phase 5：ExpiryCalculator

### Task 14: IExpiryCalculator + ExpiryCalculator + Tests → §8-3, §8-4

**Files:**
- Create: `src/TCS.Core/Interfaces/IExpiryCalculator.cs`
- Create: `src/TCS.Core/Services/ExpiryCalculator.cs`
- Create: `tests/TCS.Tests/Services/ExpiryCalculatorTests.cs`

- [ ] **Step 1: 建立 `IExpiryCalculator.cs`**

```csharp
using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface IExpiryCalculator
{
    /// <summary>
    /// 計算給定 details 清單中每筆應設定的 IsExpired 值。
    /// 回傳 Dictionary(TrainingDate, shouldBeExpired)。
    /// </summary>
    Dictionary<DateTime, bool> Calculate(
        IReadOnlyList<TrainingDetail> details,
        int requiredHours,
        int years,
        DateTime today);
}
```

- [ ] **Step 2: 寫失敗測試**

```csharp
// tests/TCS.Tests/Services/ExpiryCalculatorTests.cs
using FluentAssertions;
using TCS.Core.Entities;
using TCS.Core.Services;
using Xunit;

namespace TCS.Tests.Services;

public class ExpiryCalculatorTests
{
    private readonly ExpiryCalculator _sut = new();
    private static TrainingDetail D(DateTime date, byte type, decimal hours) => new()
    {
        EmployeeId = "E001", LicenseType = "1.1",
        TrainingDate = date, TrainingType = type, Hours = hours, IsExpired = false
    };

    [Fact]
    public void Single_acquire_within_period_and_hours_met_should_not_expire()
    {
        var details = new List<TrainingDetail>
        {
            D(new DateTime(2024, 1, 1), 1, 8),  // 取得證照
            D(new DateTime(2024, 6, 1), 2, 8)   // 回訓，累計 16 >= 8
        };
        var result = _sut.Calculate(details, requiredHours: 8, years: 2, today: new DateTime(2024, 12, 1));
        result.Values.Should().AllSatisfy(v => v.Should().BeFalse());
    }

    [Fact]
    public void Period_ended_and_hours_not_met_should_expire()
    {
        var details = new List<TrainingDetail>
        {
            D(new DateTime(2020, 1, 1), 1, 4)   // 取得證照但只有 4h < 8h required, period ends 2022-01-01
        };
        var result = _sut.Calculate(details, requiredHours: 8, years: 2, today: new DateTime(2024, 1, 1));
        result[new DateTime(2020, 1, 1)].Should().BeTrue();
    }

    [Fact]
    public void Multiple_acquire_each_starts_new_cycle()
    {
        // First cycle 2020-2022: only 4h (expired)
        // Second cycle from 2022-07-01: 8h met (not expired)
        var details = new List<TrainingDetail>
        {
            D(new DateTime(2020, 1, 1), 1, 4),
            D(new DateTime(2022, 7, 1), 1, 8)   // new acquire, new cycle
        };
        var result = _sut.Calculate(details, requiredHours: 8, years: 2, today: new DateTime(2024, 1, 1));
        result[new DateTime(2020, 1, 1)].Should().BeTrue("first cycle expired");
        result[new DateTime(2022, 7, 1)].Should().BeFalse("second cycle met");
    }

    [Fact]
    public void No_details_returns_empty()
    {
        var result = _sut.Calculate(new List<TrainingDetail>(), 8, 2, DateTime.Today);
        result.Should().BeEmpty();
    }
}
```

- [ ] **Step 3: 執行測試，確認失敗**

```bash
dotnet test tests/TCS.Tests --filter ExpiryCalculatorTests
```
Expected: FAIL（`ExpiryCalculator` 不存在）

- [ ] **Step 4: 實作 `ExpiryCalculator.cs`**

```csharp
using TCS.Core.Entities;
using TCS.Core.Interfaces;

namespace TCS.Core.Services;

public class ExpiryCalculator : IExpiryCalculator
{
    public Dictionary<DateTime, bool> Calculate(
        IReadOnlyList<TrainingDetail> details,
        int requiredHours,
        int years,
        DateTime today)
    {
        if (!details.Any()) return new Dictionary<DateTime, bool>();

        var result = new Dictionary<DateTime, bool>();
        var sorted = details.OrderBy(d => d.TrainingDate).ToList();

        // 找出所有取得證照節點
        var acquireDates = sorted
            .Where(d => d.TrainingType == 1)
            .Select(d => d.TrainingDate)
            .OrderBy(d => d)
            .ToList();

        if (!acquireDates.Any())
        {
            // 孤兒紀錄（無取得證照）→ 全部不過期
            foreach (var d in sorted) result[d.TrainingDate] = false;
            return result;
        }

        for (int i = 0; i < acquireDates.Count; i++)
        {
            var cycleStart = acquireDates[i];
            // 週期結束 = MIN(cycleStart + years, 下一筆取得日 or MaxValue)
            var nextAcquire = i + 1 < acquireDates.Count ? acquireDates[i + 1] : DateTime.MaxValue;
            var naturalEnd  = cycleStart.AddYears(years);
            var cycleEnd    = naturalEnd < nextAcquire ? naturalEnd : nextAcquire;

            // 此週期內的 details
            var inCycle = sorted.Where(d => d.TrainingDate >= cycleStart && d.TrainingDate < cycleEnd).ToList();
            var accumulated = inCycle.Sum(d => d.Hours);
            bool periodEnded = cycleEnd <= today;
            bool expired = periodEnded && accumulated < requiredHours;

            foreach (var d in inCycle)
                result[d.TrainingDate] = expired;
        }

        return result;
    }
}
```

- [ ] **Step 5: 執行測試，確認通過**

```bash
dotnet test tests/TCS.Tests --filter ExpiryCalculatorTests
```
Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Core/Interfaces/IExpiryCalculator.cs src/TCS.Core/Services/ExpiryCalculator.cs tests/TCS.Tests/Services/ExpiryCalculatorTests.cs
git commit -m "feat: add ExpiryCalculator with tests"
```

---

## Phase 6：Repositories

### Task 15: License Repositories → §4-1, §4-2

**Files:**
- Modify: `src/TCS.Core/Interfaces/ILicenseRepository.cs` (補全方法)
- Create: `src/TCS.Infrastructure/Repositories/LicenseRepository.cs`

- [ ] **Step 1: 補全 `ILicenseRepository.cs`**

```csharp
using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface ILicenseRepository
{
    Task<bool> ExistsByTypeAsync(string licenseType, CancellationToken ct = default);
    Task<bool> IsSubTypeAsync(string licenseType, CancellationToken ct = default);
    Task<bool> IsCategoryTypeAsync(string licenseType, CancellationToken ct = default);
    Task<bool> PlantRequirementExistsAsync(string licenseType, string plant, CancellationToken ct = default);
    Task<bool> HasTrainingHeadersAsync(string licenseType, CancellationToken ct = default);

    Task<IReadOnlyList<LicenseMaster>> GetAllAsync(string? keyword, string? category,
        int? minHours, int? maxHours, int? minYears, int? maxYears, CancellationToken ct = default);
    Task<LicenseMaster?> GetByTypeAsync(string licenseType, CancellationToken ct = default);

    Task<IReadOnlyList<LicensePlantRequirement>> GetRequirementsByTypeAsync(
        string licenseType, CancellationToken ct = default);

    Task AddAsync(LicenseMaster entity, CancellationToken ct = default);
    Task AddRequirementAsync(LicensePlantRequirement entity, CancellationToken ct = default);
    void Update(LicenseMaster entity);
    void UpdateRequirement(LicensePlantRequirement entity);
    void Delete(LicenseMaster entity);
    void DeleteRequirement(LicensePlantRequirement entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: 建立 `LicenseRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Repositories;

public class LicenseRepository : ILicenseRepository
{
    private readonly AppDbContext _db;
    public LicenseRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsByTypeAsync(string licenseType, CancellationToken ct) =>
        _db.LicenseMasters.AnyAsync(e => e.LicenseType == licenseType, ct);

    public Task<bool> IsSubTypeAsync(string licenseType, CancellationToken ct) =>
        _db.LicenseMasters.AnyAsync(e => e.LicenseType == licenseType && e.LicenseType.Contains("."), ct);

    public Task<bool> IsCategoryTypeAsync(string licenseType, CancellationToken ct) =>
        _db.LicenseMasters.AnyAsync(e => e.LicenseType == licenseType && !e.LicenseType.Contains("."), ct);

    public Task<bool> PlantRequirementExistsAsync(string licenseType, string plant, CancellationToken ct) =>
        _db.LicensePlantRequirements.AnyAsync(e => e.LicenseType == licenseType && e.Plant == plant, ct);

    public Task<bool> HasTrainingHeadersAsync(string licenseType, CancellationToken ct) =>
        _db.TrainingHeaders.AnyAsync(e => e.LicenseType == licenseType, ct);

    public async Task<IReadOnlyList<LicenseMaster>> GetAllAsync(string? keyword, string? category,
        int? minHours, int? maxHours, int? minYears, int? maxYears, CancellationToken ct)
    {
        var q = _db.LicenseMasters.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(e => e.LicenseType.Contains(keyword) || e.Description.Contains(keyword));
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(e => e.Category == category);
        if (minHours.HasValue) q = q.Where(e => e.Hours >= minHours);
        if (maxHours.HasValue) q = q.Where(e => e.Hours <= maxHours);
        if (minYears.HasValue) q = q.Where(e => e.Years >= minYears);
        if (maxYears.HasValue) q = q.Where(e => e.Years <= maxYears);
        return await q.OrderBy(e => e.LicenseType).ToListAsync(ct);
    }

    public Task<LicenseMaster?> GetByTypeAsync(string licenseType, CancellationToken ct) =>
        _db.LicenseMasters.Include(e => e.PlantRequirements)
            .FirstOrDefaultAsync(e => e.LicenseType == licenseType, ct);

    public async Task<IReadOnlyList<LicensePlantRequirement>> GetRequirementsByTypeAsync(
        string licenseType, CancellationToken ct) =>
        await _db.LicensePlantRequirements
            .Where(e => e.LicenseType == licenseType)
            .ToListAsync(ct);

    public Task AddAsync(LicenseMaster entity, CancellationToken ct) =>
        _db.LicenseMasters.AddAsync(entity, ct).AsTask();

    public Task AddRequirementAsync(LicensePlantRequirement entity, CancellationToken ct) =>
        _db.LicensePlantRequirements.AddAsync(entity, ct).AsTask();

    public void Update(LicenseMaster entity) => _db.LicenseMasters.Update(entity);
    public void UpdateRequirement(LicensePlantRequirement entity) => _db.LicensePlantRequirements.Update(entity);
    public void Delete(LicenseMaster entity) => _db.LicenseMasters.Remove(entity);
    public void DeleteRequirement(LicensePlantRequirement entity) => _db.LicensePlantRequirements.Remove(entity);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
```

- [ ] **Step 3: Commit**

```bash
git add src/TCS.Core/Interfaces/ILicenseRepository.cs src/TCS.Infrastructure/Repositories/
git commit -m "feat: add LicenseRepository"
```

---

### Task 16: Training + Employee + Plant Repositories → §4-3, §4-4, §4-5

**Files:**
- Modify: `src/TCS.Core/Interfaces/ITrainingRepository.cs` (補全)
- Modify: `src/TCS.Core/Interfaces/IEmployeeRepository.cs` (補全)
- Create: `src/TCS.Core/Interfaces/IPlantRepository.cs`
- Create: `src/TCS.Infrastructure/Repositories/TrainingRepository.cs`
- Create: `src/TCS.Infrastructure/Repositories/EmployeeRepository.cs`
- Create: `src/TCS.Infrastructure/Repositories/PlantRepository.cs`

- [ ] **Step 1: 補全 `ITrainingRepository.cs`**

```csharp
using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface ITrainingRepository
{
    Task<bool> HeaderExistsAsync(string empId, string licType, CancellationToken ct = default);
    Task<bool> DetailExistsAsync(string empId, string licType, DateTime date, CancellationToken ct = default);

    Task<IReadOnlyList<TrainingHeader>> GetHeadersAsync(string? empId, string? empName,
        string? department, string? licType, bool? expiredOnly, CancellationToken ct = default);
    Task<TrainingHeader?> GetHeaderAsync(string empId, string licType, CancellationToken ct = default);

    Task<IReadOnlyList<TrainingDetail>> GetDetailsAsync(string empId, string licType, CancellationToken ct = default);
    Task<TrainingDetail?> GetDetailAsync(string empId, string licType, DateTime date, CancellationToken ct = default);

    Task AddHeaderAsync(TrainingHeader entity, CancellationToken ct = default);
    Task AddDetailAsync(TrainingDetail entity, CancellationToken ct = default);
    void UpdateHeader(TrainingHeader entity);
    void UpdateDetail(TrainingDetail entity);
    void DeleteHeader(TrainingHeader entity);
    void DeleteDetail(TrainingDetail entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: 補全 `IEmployeeRepository.cs`**

```csharp
using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface IEmployeeRepository
{
    Task<bool> ExistsAsync(string employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> SearchAsync(string keyword, CancellationToken ct = default);
}
```

- [ ] **Step 3: 建立 `IPlantRepository.cs`**

```csharp
using TCS.Core.Entities;

namespace TCS.Core.Interfaces;

public interface IPlantRepository
{
    Task<IReadOnlyList<Plant>> GetAllAsync(CancellationToken ct = default);
}
```

- [ ] **Step 4: 建立 `TrainingRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Repositories;

public class TrainingRepository : ITrainingRepository
{
    private readonly AppDbContext _db;
    public TrainingRepository(AppDbContext db) => _db = db;

    public Task<bool> HeaderExistsAsync(string empId, string licType, CancellationToken ct) =>
        _db.TrainingHeaders.AnyAsync(e => e.EmployeeId == empId && e.LicenseType == licType, ct);

    public Task<bool> DetailExistsAsync(string empId, string licType, DateTime date, CancellationToken ct) =>
        _db.TrainingDetails.AnyAsync(e => e.EmployeeId == empId && e.LicenseType == licType && e.TrainingDate == date, ct);

    public async Task<IReadOnlyList<TrainingHeader>> GetHeadersAsync(string? empId, string? empName,
        string? department, string? licType, bool? expiredOnly, CancellationToken ct)
    {
        var q = _db.TrainingHeaders
            .Include(h => h.Details)
            .Include(h => h.LicenseMasterNav)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(empId)) q = q.Where(h => h.EmployeeId.Contains(empId));
        if (!string.IsNullOrWhiteSpace(licType)) q = q.Where(h => h.LicenseType == licType);
        if (expiredOnly == true) q = q.Where(h => h.Details.Any(d => d.IsExpired));
        return await q.ToListAsync(ct);
    }

    public Task<TrainingHeader?> GetHeaderAsync(string empId, string licType, CancellationToken ct) =>
        _db.TrainingHeaders
            .Include(h => h.Details)
            .Include(h => h.LicenseMasterNav)
            .FirstOrDefaultAsync(h => h.EmployeeId == empId && h.LicenseType == licType, ct);

    public async Task<IReadOnlyList<TrainingDetail>> GetDetailsAsync(string empId, string licType, CancellationToken ct) =>
        await _db.TrainingDetails
            .Where(d => d.EmployeeId == empId && d.LicenseType == licType)
            .OrderBy(d => d.TrainingDate)
            .ToListAsync(ct);

    public Task<TrainingDetail?> GetDetailAsync(string empId, string licType, DateTime date, CancellationToken ct) =>
        _db.TrainingDetails.FirstOrDefaultAsync(
            d => d.EmployeeId == empId && d.LicenseType == licType && d.TrainingDate == date, ct);

    public Task AddHeaderAsync(TrainingHeader entity, CancellationToken ct) =>
        _db.TrainingHeaders.AddAsync(entity, ct).AsTask();

    public Task AddDetailAsync(TrainingDetail entity, CancellationToken ct) =>
        _db.TrainingDetails.AddAsync(entity, ct).AsTask();

    public void UpdateHeader(TrainingHeader entity) => _db.TrainingHeaders.Update(entity);
    public void UpdateDetail(TrainingDetail entity) => _db.TrainingDetails.Update(entity);
    public void DeleteHeader(TrainingHeader entity) => _db.TrainingHeaders.Remove(entity);
    public void DeleteDetail(TrainingDetail entity) => _db.TrainingDetails.Remove(entity);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
```

- [ ] **Step 5: 建立 `EmployeeRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;
    public EmployeeRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(string employeeId, CancellationToken ct) =>
        _db.Employees.AnyAsync(e => e.EmployeeId == employeeId, ct);

    public async Task<IReadOnlyList<Employee>> SearchAsync(string keyword, CancellationToken ct) =>
        await _db.Employees
            .Where(e => e.EmployeeId.Contains(keyword) || e.Name.Contains(keyword))
            .Take(50)
            .ToListAsync(ct);
}
```

- [ ] **Step 6: 建立 `PlantRepository.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Repositories;

public class PlantRepository : IPlantRepository
{
    private readonly AppDbContext _db;
    public PlantRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Plant>> GetAllAsync(CancellationToken ct) =>
        await _db.Plants.OrderBy(p => p.PlantCode).ToListAsync(ct);
}
```

- [ ] **Step 7: Commit**

```bash
git add src/TCS.Core/Interfaces/ src/TCS.Infrastructure/Repositories/
git commit -m "feat: add Training, Employee, Plant repositories"
```

---

## Phase 7：Services

### Task 17: LicenseService + Tests → §8-5

**Files:**
- Create: `src/TCS.Core/Interfaces/ILicenseService.cs`
- Create: `src/TCS.Core/Services/LicenseService.cs`
- Create: `tests/TCS.Tests/Services/LicenseServiceTests.cs`

- [ ] **Step 1: 建立 `ILicenseService.cs`**

```csharp
using TCS.Core.Common;
using TCS.Core.DTOs;

namespace TCS.Core.Interfaces;

public interface ILicenseService
{
    Task<CrudResult<IReadOnlyList<LicenseMasterDto>>> GetAllAsync(
        string? keyword, string? category, int? minHours, int? maxHours, int? minYears, int? maxYears,
        CancellationToken ct = default);
    Task<CrudResult<LicenseMasterDto>> GetByTypeAsync(string licenseType, CancellationToken ct = default);
    Task<CrudResult<LicenseMasterDto>> CreateAsync(CreateLicenseMasterRequest req, string creatorId, CancellationToken ct = default);
    Task<CrudResult<LicenseMasterDto>> UpdateAsync(string licenseType, UpdateLicenseMasterRequest req, string modifierId, CancellationToken ct = default);
    Task<CrudResult<bool>> DeleteAsync(string licenseType, CancellationToken ct = default);

    Task<CrudResult<IReadOnlyList<LicensePlantRequirementDto>>> GetRequirementsAsync(string licenseType, CancellationToken ct = default);
    Task<CrudResult<LicensePlantRequirementDto>> CreateRequirementAsync(string licenseType, CreateLicensePlantRequirementRequest req, string creatorId, CancellationToken ct = default);
    Task<CrudResult<LicensePlantRequirementDto>> UpdateRequirementAsync(string licenseType, string plant, UpdateLicensePlantRequirementRequest req, string modifierId, CancellationToken ct = default);
    Task<CrudResult<bool>> DeleteRequirementAsync(string licenseType, string plant, CancellationToken ct = default);
}
```

- [ ] **Step 2: 建立 `LicenseService.cs`**

```csharp
using TCS.Core.Common;
using TCS.Core.DTOs;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Core.Validators;

namespace TCS.Core.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository _repo;
    private readonly IPlantRepository _plantRepo;

    public LicenseService(ILicenseRepository repo, IPlantRepository plantRepo)
    {
        _repo = repo;
        _plantRepo = plantRepo;
    }

    public async Task<CrudResult<IReadOnlyList<LicenseMasterDto>>> GetAllAsync(
        string? keyword, string? category,
        int? minHours, int? maxHours, int? minYears, int? maxYears, CancellationToken ct)
    {
        var list = await _repo.GetAllAsync(keyword, category, minHours, maxHours, minYears, maxYears, ct);
        return CrudResult<IReadOnlyList<LicenseMasterDto>>.SuccessResult(
            list.Select(e => e.ToDto()).ToList());
    }

    public async Task<CrudResult<LicenseMasterDto>> GetByTypeAsync(string licenseType, CancellationToken ct)
    {
        var entity = await _repo.GetByTypeAsync(licenseType, ct);
        return entity is null
            ? CrudResult<LicenseMasterDto>.ErrorResult("證照類別不存在")
            : CrudResult<LicenseMasterDto>.SuccessResult(entity.ToDto());
    }

    public async Task<CrudResult<LicenseMasterDto>> CreateAsync(
        CreateLicenseMasterRequest req, string creatorId, CancellationToken ct)
    {
        var validator = new CreateLicenseMasterValidator(_repo);
        var vr = await validator.ValidateAsync(req, ct);
        if (!vr.IsValid)
            return CrudResult<LicenseMasterDto>.ErrorResult("驗證失敗", vr.Errors.Select(e => e.ErrorMessage).ToList());

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var entity = new LicenseMaster
        {
            LicenseType = req.LicenseType,
            Description = req.Description,
            Category    = req.Category,
            Hours       = req.Hours,
            Years       = req.Years,
            Creator     = creatorId,
            CreateDate  = today,
            Modifier    = creatorId,
            ModiDate    = today,
            Flag        = 0
        };
        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);
        return CrudResult<LicenseMasterDto>.SuccessResult(entity.ToDto(), "新增成功");
    }

    public async Task<CrudResult<LicenseMasterDto>> UpdateAsync(
        string licenseType, UpdateLicenseMasterRequest req, string modifierId, CancellationToken ct)
    {
        var entity = await _repo.GetByTypeAsync(licenseType, ct);
        if (entity is null)
            return CrudResult<LicenseMasterDto>.ErrorResult("證照類別不存在");

        var validator = new UpdateLicenseMasterValidator(_repo);
        var vr = await validator.ValidateAsync(req, ct);
        if (!vr.IsValid)
            return CrudResult<LicenseMasterDto>.ErrorResult("驗證失敗", vr.Errors.Select(e => e.ErrorMessage).ToList());

        entity.Description = req.Description;
        entity.Category    = req.Category;
        entity.Hours       = req.Hours;
        entity.Years       = req.Years;
        entity.Modifier    = modifierId;
        entity.ModiDate    = DateTime.UtcNow.ToString("yyyyMMdd");

        _repo.Update(entity);
        await _repo.SaveChangesAsync(ct);
        return CrudResult<LicenseMasterDto>.SuccessResult(entity.ToDto(), "修改成功");
    }

    public async Task<CrudResult<bool>> DeleteAsync(string licenseType, CancellationToken ct)
    {
        var entity = await _repo.GetByTypeAsync(licenseType, ct);
        if (entity is null) return CrudResult<bool>.ErrorResult("證照類別不存在");

        if (await _repo.HasTrainingHeadersAsync(licenseType, ct))
            return CrudResult<bool>.ErrorResult("尚有受訓資料引用此證照，無法刪除");

        _repo.Delete(entity);
        await _repo.SaveChangesAsync(ct);
        return CrudResult<bool>.SuccessResult(true, "刪除成功");
    }

    public async Task<CrudResult<IReadOnlyList<LicensePlantRequirementDto>>> GetRequirementsAsync(
        string licenseType, CancellationToken ct)
    {
        var plants = await _plantRepo.GetAllAsync(ct);
        var plantMap = plants.ToDictionary(p => p.PlantCode.Trim(), p => p.PlantName?.Trim());
        var list = await _repo.GetRequirementsByTypeAsync(licenseType, ct);
        var dtos = list.Select(r => r.ToDto(plantMap.GetValueOrDefault(r.Plant.Trim()))).ToList();
        return CrudResult<IReadOnlyList<LicensePlantRequirementDto>>.SuccessResult(dtos);
    }

    public async Task<CrudResult<LicensePlantRequirementDto>> CreateRequirementAsync(
        string licenseType, CreateLicensePlantRequirementRequest req, string creatorId, CancellationToken ct)
    {
        if (!await _repo.IsSubTypeAsync(licenseType, ct))
            return CrudResult<LicensePlantRequirementDto>.ErrorResult("證照類別必須為小類");

        var validator = new CreateLicensePlantRequirementValidator(_repo, licenseType);
        var vr = await validator.ValidateAsync(req, ct);
        if (!vr.IsValid)
            return CrudResult<LicensePlantRequirementDto>.ErrorResult("驗證失敗", vr.Errors.Select(e => e.ErrorMessage).ToList());

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var entity = new LicensePlantRequirement
        {
            LicenseType   = licenseType,
            Plant         = req.Plant,
            RequiredCount = req.RequiredCount,
            Creator       = creatorId,
            CreateDate    = today,
            Modifier      = creatorId,
            ModiDate      = today
        };
        await _repo.AddRequirementAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);
        return CrudResult<LicensePlantRequirementDto>.SuccessResult(entity.ToDto(), "新增成功");
    }

    public async Task<CrudResult<LicensePlantRequirementDto>> UpdateRequirementAsync(
        string licenseType, string plant, UpdateLicensePlantRequirementRequest req, string modifierId, CancellationToken ct)
    {
        if (!await _repo.PlantRequirementExistsAsync(licenseType, plant, ct))
            return CrudResult<LicensePlantRequirementDto>.ErrorResult("廠別需求不存在");

        var requirements = await _repo.GetRequirementsByTypeAsync(licenseType, ct);
        var entity = requirements.FirstOrDefault(r => r.Plant.Trim() == plant.Trim());
        if (entity is null) return CrudResult<LicensePlantRequirementDto>.ErrorResult("廠別需求不存在");

        var validator = new UpdateLicensePlantRequirementValidator();
        var vr = validator.Validate(req);
        if (!vr.IsValid)
            return CrudResult<LicensePlantRequirementDto>.ErrorResult("驗證失敗", vr.Errors.Select(e => e.ErrorMessage).ToList());

        entity.RequiredCount = req.RequiredCount;
        entity.Modifier      = modifierId;
        entity.ModiDate      = DateTime.UtcNow.ToString("yyyyMMdd");

        _repo.UpdateRequirement(entity);
        await _repo.SaveChangesAsync(ct);
        return CrudResult<LicensePlantRequirementDto>.SuccessResult(entity.ToDto(), "修改成功");
    }

    public async Task<CrudResult<bool>> DeleteRequirementAsync(string licenseType, string plant, CancellationToken ct)
    {
        var requirements = await _repo.GetRequirementsByTypeAsync(licenseType, ct);
        var entity = requirements.FirstOrDefault(r => r.Plant.Trim() == plant.Trim());
        if (entity is null) return CrudResult<bool>.ErrorResult("廠別需求不存在");

        _repo.DeleteRequirement(entity);
        await _repo.SaveChangesAsync(ct);
        return CrudResult<bool>.SuccessResult(true, "刪除成功");
    }
}
```

- [ ] **Step 3: 寫失敗測試**

```csharp
// tests/TCS.Tests/Services/LicenseServiceTests.cs
using FluentAssertions;
using Moq;
using TCS.Core.DTOs;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Core.Services;
using Xunit;

namespace TCS.Tests.Services;

public class LicenseServiceTests
{
    private readonly Mock<ILicenseRepository> _repoMock = new();
    private readonly Mock<IPlantRepository>   _plantMock = new();
    private LicenseService Sut() => new(_repoMock.Object, _plantMock.Object);

    [Fact]
    public async Task DeleteAsync_with_existing_training_header_returns_error()
    {
        _repoMock.Setup(r => r.GetByTypeAsync("1.1", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "1.1", Description = "Test" });
        _repoMock.Setup(r => r.HasTrainingHeadersAsync("1.1", default)).ReturnsAsync(true);

        var result = await Sut().DeleteAsync("1.1", default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("尚有受訓資料");
    }

    [Fact]
    public async Task DeleteAsync_without_training_header_succeeds()
    {
        _repoMock.Setup(r => r.GetByTypeAsync("1.1", default))
            .ReturnsAsync(new LicenseMaster { LicenseType = "1.1", Description = "Test" });
        _repoMock.Setup(r => r.HasTrainingHeadersAsync("1.1", default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await Sut().DeleteAsync("1.1", default);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetByTypeAsync_not_found_returns_error()
    {
        _repoMock.Setup(r => r.GetByTypeAsync("9.9", default)).ReturnsAsync((LicenseMaster?)null);

        var result = await Sut().GetByTypeAsync("9.9", default);

        result.Success.Should().BeFalse();
    }
}
```

- [ ] **Step 4: 執行測試**

```bash
dotnet test tests/TCS.Tests --filter LicenseServiceTests
```
Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Interfaces/ src/TCS.Core/Services/LicenseService.cs tests/TCS.Tests/Services/LicenseServiceTests.cs
git commit -m "feat: add LicenseService with tests"
```

---

### Task 18: TrainingService + Tests → §8-1, §8-2, §8-5

**Files:**
- Create: `src/TCS.Core/Interfaces/ITrainingService.cs`
- Create: `src/TCS.Core/Services/TrainingService.cs`
- Create: `tests/TCS.Tests/Services/TrainingServiceTests.cs`

- [ ] **Step 1: 建立 `ITrainingService.cs`**

```csharp
using TCS.Core.Common;
using TCS.Core.DTOs;

namespace TCS.Core.Interfaces;

public interface ITrainingService
{
    Task<CrudResult<IReadOnlyList<TrainingHeaderDto>>> GetHeadersAsync(
        string? empId, string? empName, string? department, string? licType, bool? expiredOnly,
        CancellationToken ct = default);
    Task<CrudResult<TrainingHeaderDto>> GetHeaderAsync(string empId, string licType, CancellationToken ct = default);
    Task<CrudResult<TrainingHeaderDto>> CreateHeaderAsync(CreateTrainingHeaderRequest req, string creatorId, CancellationToken ct = default);
    Task<CrudResult<TrainingHeaderDto>> UpdateHeaderAsync(string empId, string licType, UpdateTrainingHeaderRequest req, string modifierId, CancellationToken ct = default);
    Task<CrudResult<bool>> DeleteHeaderAsync(string empId, string licType, CancellationToken ct = default);

    Task<CrudResult<IReadOnlyList<TrainingDetailDto>>> GetDetailsAsync(string empId, string licType, CancellationToken ct = default);
    Task<CrudResult<TrainingDetailDto>> CreateDetailAsync(string empId, string licType, CreateTrainingDetailRequest req, string creatorId, CancellationToken ct = default);
    Task<CrudResult<TrainingDetailDto>> UpdateDetailAsync(string empId, string licType, DateTime date, UpdateTrainingDetailRequest req, string modifierId, CancellationToken ct = default);
    Task<CrudResult<bool>> DeleteDetailAsync(string empId, string licType, DateTime date, CancellationToken ct = default);
}
```

- [ ] **Step 2: 建立 `TrainingService.cs`**

```csharp
using TCS.Core.Common;
using TCS.Core.DTOs;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Core.Validators;

namespace TCS.Core.Services;

public class TrainingService : ITrainingService
{
    private readonly ITrainingRepository  _repo;
    private readonly ILicenseRepository   _licRepo;
    private readonly IEmployeeRepository  _empRepo;
    private readonly IExpiryCalculator    _calc;

    public TrainingService(ITrainingRepository repo, ILicenseRepository licRepo,
        IEmployeeRepository empRepo, IExpiryCalculator calc)
    {
        _repo    = repo;
        _licRepo = licRepo;
        _empRepo = empRepo;
        _calc    = calc;
    }

    public async Task<CrudResult<IReadOnlyList<TrainingHeaderDto>>> GetHeadersAsync(
        string? empId, string? empName, string? department, string? licType, bool? expiredOnly,
        CancellationToken ct)
    {
        var headers = await _repo.GetHeadersAsync(empId, empName, department, licType, expiredOnly, ct);
        var dtos = new List<TrainingHeaderDto>();
        foreach (var h in headers)
            dtos.Add(await BuildHeaderDtoAsync(h, ct));
        return CrudResult<IReadOnlyList<TrainingHeaderDto>>.SuccessResult(dtos);
    }

    public async Task<CrudResult<TrainingHeaderDto>> GetHeaderAsync(string empId, string licType, CancellationToken ct)
    {
        var h = await _repo.GetHeaderAsync(empId, licType, ct);
        if (h is null) return CrudResult<TrainingHeaderDto>.ErrorResult("受訓記錄不存在");
        return CrudResult<TrainingHeaderDto>.SuccessResult(await BuildHeaderDtoAsync(h, ct));
    }

    public async Task<CrudResult<TrainingHeaderDto>> CreateHeaderAsync(
        CreateTrainingHeaderRequest req, string creatorId, CancellationToken ct)
    {
        var validator = new CreateTrainingHeaderValidator(_empRepo, _licRepo);
        var vr = await validator.ValidateAsync(req, ct);
        if (!vr.IsValid)
            return CrudResult<TrainingHeaderDto>.ErrorResult("驗證失敗", vr.Errors.Select(e => e.ErrorMessage).ToList());

        if (await _repo.HeaderExistsAsync(req.EmployeeId, req.LicenseType, ct))
            return CrudResult<TrainingHeaderDto>.ErrorResult("此員工的證照受訓記錄已存在");

        var license = await _licRepo.GetByTypeAsync(req.LicenseType, ct);
        var today   = DateTime.UtcNow.ToString("yyyyMMdd");

        var entity = new TrainingHeader
        {
            EmployeeId    = req.EmployeeId,
            LicenseType   = req.LicenseType,
            RequiredHours = license!.Hours ?? 0,  // §8-1: 系統自動帶入
            Remark        = req.Remark,
            Creator       = creatorId,
            CreateDate    = today,
            Modifier      = creatorId,
            ModiDate      = today
        };
        await _repo.AddHeaderAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        var saved = await _repo.GetHeaderAsync(req.EmployeeId, req.LicenseType, ct);
        return CrudResult<TrainingHeaderDto>.SuccessResult(await BuildHeaderDtoAsync(saved!, ct), "新增成功");
    }

    public async Task<CrudResult<TrainingHeaderDto>> UpdateHeaderAsync(
        string empId, string licType, UpdateTrainingHeaderRequest req, string modifierId, CancellationToken ct)
    {
        var h = await _repo.GetHeaderAsync(empId, licType, ct);
        if (h is null) return CrudResult<TrainingHeaderDto>.ErrorResult("受訓記錄不存在");

        var validator = new UpdateTrainingHeaderValidator();
        var vr        = validator.Validate(req);
        if (!vr.IsValid)
            return CrudResult<TrainingHeaderDto>.ErrorResult("驗證失敗", vr.Errors.Select(e => e.ErrorMessage).ToList());

        h.Remark   = req.Remark;
        h.Modifier = modifierId;
        h.ModiDate = DateTime.UtcNow.ToString("yyyyMMdd");
        _repo.UpdateHeader(h);
        await _repo.SaveChangesAsync(ct);
        return CrudResult<TrainingHeaderDto>.SuccessResult(await BuildHeaderDtoAsync(h, ct), "修改成功");
    }

    public async Task<CrudResult<bool>> DeleteHeaderAsync(string empId, string licType, CancellationToken ct)
    {
        var h = await _repo.GetHeaderAsync(empId, licType, ct);
        if (h is null) return CrudResult<bool>.ErrorResult("受訓記錄不存在");

        _repo.DeleteHeader(h);
        await _repo.SaveChangesAsync(ct);
        return CrudResult<bool>.SuccessResult(true, "刪除成功");
    }

    public async Task<CrudResult<IReadOnlyList<TrainingDetailDto>>> GetDetailsAsync(
        string empId, string licType, CancellationToken ct)
    {
        var details = await _repo.GetDetailsAsync(empId, licType, ct);
        return CrudResult<IReadOnlyList<TrainingDetailDto>>.SuccessResult(
            details.Select(d => d.ToDto()).ToList());
    }

    public async Task<CrudResult<TrainingDetailDto>> CreateDetailAsync(
        string empId, string licType, CreateTrainingDetailRequest req, string creatorId, CancellationToken ct)
    {
        var validator = new CreateTrainingDetailValidator(_repo, empId, licType);
        var vr = await validator.ValidateAsync(req, ct);
        if (!vr.IsValid)
            return CrudResult<TrainingDetailDto>.ErrorResult("驗證失敗", vr.Errors.Select(e => e.ErrorMessage).ToList());

        var today  = DateTime.UtcNow.ToString("yyyyMMdd");
        var entity = new TrainingDetail
        {
            EmployeeId   = empId,
            LicenseType  = licType,
            TrainingDate = req.TrainingDate,
            TrainingType = req.TrainingType,
            Hours        = req.Hours,
            IsExpired    = false,
            Creator      = creatorId,
            CreateDate   = today,
            Modifier     = creatorId,
            ModiDate     = today
        };
        await _repo.AddDetailAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        var orderError = await ValidateDetailOrderAsync(empId, licType, ct);
        if (orderError is not null)
        {
            _repo.DeleteDetail(entity);
            await _repo.SaveChangesAsync(ct);
            return CrudResult<TrainingDetailDto>.ErrorResult(orderError);
        }

        return CrudResult<TrainingDetailDto>.SuccessResult(entity.ToDto(), "新增成功");
    }

    public async Task<CrudResult<TrainingDetailDto>> UpdateDetailAsync(
        string empId, string licType, DateTime date, UpdateTrainingDetailRequest req, string modifierId, CancellationToken ct)
    {
        var entity = await _repo.GetDetailAsync(empId, licType, date, ct);
        if (entity is null) return CrudResult<TrainingDetailDto>.ErrorResult("受訓明細不存在");

        var validator = new UpdateTrainingDetailValidator();
        var vr        = validator.Validate(req);
        if (!vr.IsValid)
            return CrudResult<TrainingDetailDto>.ErrorResult("驗證失敗", vr.Errors.Select(e => e.ErrorMessage).ToList());

        var old = (entity.TrainingType, entity.Hours);
        entity.TrainingType = req.TrainingType;
        entity.Hours        = req.Hours;
        entity.Modifier     = modifierId;
        entity.ModiDate     = DateTime.UtcNow.ToString("yyyyMMdd");
        _repo.UpdateDetail(entity);
        await _repo.SaveChangesAsync(ct);

        var orderError = await ValidateDetailOrderAsync(empId, licType, ct);
        if (orderError is not null)
        {
            entity.TrainingType = old.TrainingType;
            entity.Hours        = old.Hours;
            _repo.UpdateDetail(entity);
            await _repo.SaveChangesAsync(ct);
            return CrudResult<TrainingDetailDto>.ErrorResult(orderError);
        }

        return CrudResult<TrainingDetailDto>.SuccessResult(entity.ToDto(), "修改成功");
    }

    public async Task<CrudResult<bool>> DeleteDetailAsync(
        string empId, string licType, DateTime date, CancellationToken ct)
    {
        var entity = await _repo.GetDetailAsync(empId, licType, date, ct);
        if (entity is null) return CrudResult<bool>.ErrorResult("受訓明細不存在");

        _repo.DeleteDetail(entity);
        await _repo.SaveChangesAsync(ct);

        var orderError = await ValidateDetailOrderAsync(empId, licType, ct);
        if (orderError is not null)
        {
            // 回滾：重新加入
            await _repo.AddDetailAsync(entity, ct);
            await _repo.SaveChangesAsync(ct);
            return CrudResult<bool>.ErrorResult("刪除此筆後將造成首筆非取得證照，請先處理其他紀錄");
        }

        return CrudResult<bool>.SuccessResult(true, "刪除成功");
    }

    // §8-2 受訓事件次序驗證
    private async Task<string?> ValidateDetailOrderAsync(string empId, string licType, CancellationToken ct)
    {
        var details = await _repo.GetDetailsAsync(empId, licType, ct);
        if (!details.Any()) return null;
        var first = details.OrderBy(d => d.TrainingDate).First();
        if (first.TrainingType != 1) return "首筆受訓紀錄必須為「取得證照」";
        return null;
    }

    private async Task<TrainingHeaderDto> BuildHeaderDtoAsync(TrainingHeader h, CancellationToken ct)
    {
        var emp     = await _empRepo.SearchAsync(h.EmployeeId.Trim(), ct);
        var empData = emp.FirstOrDefault(e => e.EmployeeId.Trim() == h.EmployeeId.Trim());
        var license = h.LicenseMasterNav ?? await _licRepo.GetByTypeAsync(h.LicenseType, ct);

        var details = h.Details.Any() ? h.Details.ToList()
            : (await _repo.GetDetailsAsync(h.EmployeeId, h.LicenseType, ct)).ToList();

        var sorted         = details.OrderBy(d => d.TrainingDate).ToList();
        var latestAcquire  = sorted.Where(d => d.TrainingType == 1).OrderByDescending(d => d.TrainingDate).FirstOrDefault();
        var latestRetrain  = sorted.Where(d => d.TrainingType == 2).OrderByDescending(d => d.TrainingDate).FirstOrDefault();

        // 當前週期累計時數
        decimal accumulated = 0;
        if (latestAcquire is not null && license?.Years != null)
        {
            var cycleStart = latestAcquire.TrainingDate;
            var cycleEnd   = cycleStart.AddYears(license.Years.Value);
            accumulated = sorted
                .Where(d => d.TrainingDate >= cycleStart && d.TrainingDate < cycleEnd)
                .Sum(d => d.Hours);
        }

        return h.ToDto(
            employeeName:        empData?.Name,
            department:          empData?.Department,
            hireDate:            empData?.HireDate,
            licenseDescription:  license?.Description,
            latestAcquireDate:   latestAcquire?.TrainingDate,
            latestRetrainDate:   latestRetrain?.TrainingDate,
            accumulatedHours:    accumulated,
            requiredHours:       h.RequiredHours);
    }
}
```

- [ ] **Step 3: 寫失敗測試**

```csharp
// tests/TCS.Tests/Services/TrainingServiceTests.cs
using FluentAssertions;
using Moq;
using TCS.Core.DTOs;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Core.Services;
using Xunit;

namespace TCS.Tests.Services;

public class TrainingServiceTests
{
    private readonly Mock<ITrainingRepository> _repoMock  = new();
    private readonly Mock<ILicenseRepository>  _licMock   = new();
    private readonly Mock<IEmployeeRepository> _empMock   = new();
    private readonly Mock<IExpiryCalculator>   _calcMock  = new();

    private TrainingService Sut() => new(_repoMock.Object, _licMock.Object, _empMock.Object, _calcMock.Object);

    [Fact]
    public async Task CreateDetailAsync_first_record_must_be_acquire_type()
    {
        _repoMock.Setup(r => r.DetailExistsAsync("E001", "1.1", It.IsAny<DateTime>(), default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.AddDetailAsync(It.IsAny<TrainingDetail>(), default)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        // After save, first record is type=2 (invalid)
        _repoMock.Setup(r => r.GetDetailsAsync("E001", "1.1", default))
            .ReturnsAsync(new List<TrainingDetail>
            {
                new() { EmployeeId="E001", LicenseType="1.1", TrainingDate=DateTime.Today, TrainingType=2, Hours=8 }
            });
        _repoMock.Setup(r => r.DeleteDetail(It.IsAny<TrainingDetail>()));

        var req = new CreateTrainingDetailRequest
        {
            TrainingDate = DateTime.Today, TrainingType = 2, Hours = 8
        };
        var result = await Sut().CreateDetailAsync("E001", "1.1", req, "system", default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("首筆受訓紀錄必須為「取得證照」");
    }

    [Fact]
    public async Task DeleteHeaderAsync_not_found_returns_error()
    {
        _repoMock.Setup(r => r.GetHeaderAsync("E001", "1.1", default)).ReturnsAsync((TrainingHeader?)null);

        var result = await Sut().DeleteHeaderAsync("E001", "1.1", default);

        result.Success.Should().BeFalse();
    }
}
```

- [ ] **Step 4: 執行測試**

```bash
dotnet test tests/TCS.Tests --filter TrainingServiceTests
```
Expected: `Passed! - Failed: 0, Passed: 2`

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Core/Interfaces/ src/TCS.Core/Services/TrainingService.cs tests/TCS.Tests/Services/TrainingServiceTests.cs
git commit -m "feat: add TrainingService with tests"
```

---

### Task 19: ExcelExportService → §5-6

**Files:**
- Create: `src/TCS.Core/Interfaces/IExportService.cs`
- Create: `src/TCS.Core/Services/ExcelExportService.cs`

- [ ] **Step 1: 建立 `IExportService.cs`**

```csharp
namespace TCS.Core.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportTrainingAsync(string? empId, string? licType, CancellationToken ct = default);
}
```

- [ ] **Step 2: 建立 `ExcelExportService.cs`**

```csharp
using ClosedXML.Excel;
using TCS.Core.Interfaces;

namespace TCS.Core.Services;

public class ExcelExportService : IExportService
{
    private readonly ITrainingService _trainingService;

    public ExcelExportService(ITrainingService trainingService)
        => _trainingService = trainingService;

    public async Task<byte[]> ExportTrainingAsync(string? empId, string? licType, CancellationToken ct)
    {
        var result = await _trainingService.GetHeadersAsync(empId, null, null, licType, null, ct);
        var headers = result.Data ?? new List<TrainingHeaderDto>();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("受訓資料");

        // Header row
        var cols = new[] { "員工編號","姓名","部門","到職日","證照類別","描述",
                           "應訓時數","最新回訓日","未達時數","下次回訓","備註" };
        for (int i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];

        // Style header
        var headerRange = ws.Range(1, 1, 1, cols.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data rows
        int row = 2;
        foreach (var h in headers)
        {
            ws.Cell(row, 1).Value = h.EmployeeId;
            ws.Cell(row, 2).Value = h.EmployeeName ?? "";
            ws.Cell(row, 3).Value = h.Department ?? "";
            ws.Cell(row, 4).Value = h.HireDate ?? "";
            ws.Cell(row, 5).Value = h.LicenseType;
            ws.Cell(row, 6).Value = h.LicenseDescription ?? "";
            ws.Cell(row, 7).Value = h.RequiredHours;
            ws.Cell(row, 8).Value = h.LatestRetrainDate?.ToString("yyyy-MM-dd") ?? "—";
            ws.Cell(row, 9).Value = (double)h.RemainingHours;
            ws.Cell(row, 10).Value = h.NextReviewDate?.ToString("yyyy-MM-dd") ?? "—";
            ws.Cell(row, 11).Value = h.Remark ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/TCS.Core/Interfaces/IExportService.cs src/TCS.Core/Services/ExcelExportService.cs
git commit -m "feat: add ExcelExportService"
```

---

## Phase 8：Authorization

### Task 20: RequireActionAttribute + JWT Bearer → §6

**Files:**
- Create: `src/TCS.Web/Filters/RequireActionAttribute.cs`
- Create: `src/TCS.Web/Filters/RequireActionFilter.cs`
- Modify: `src/TCS.Web/Program.cs` (JWT 設定，在 Task 25 完整撰寫前先加入 JWT 片段)

- [ ] **Step 1: 建立 `RequireActionAttribute.cs`**

```csharp
namespace TCS.Web.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireActionAttribute : Attribute
{
    public string ActionName { get; }
    public RequireActionAttribute(string actionName) => ActionName = actionName;
}
```

- [ ] **Step 2: 建立 `RequireActionFilter.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TCS.Web.Filters;

public class RequireActionFilter : IAsyncActionFilter
{
    private readonly string _requiredAction;
    public RequireActionFilter(string requiredAction) => _requiredAction = requiredAction;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var actionClaim = context.HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == "action")?.Value ?? "";

        var actions = actionClaim.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim());

        if (!actions.Contains(_requiredAction))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
```

- [ ] **Step 3: 建立 `RequireActionFilterFactory.cs`**

```csharp
using Microsoft.AspNetCore.Mvc.Filters;

namespace TCS.Web.Filters;

public class RequireActionFilterFactory : IFilterFactory
{
    private readonly string _actionName;
    public bool IsReusable => false;
    public RequireActionFilterFactory(string actionName) => _actionName = actionName;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        => new RequireActionFilter(_actionName);
}
```

更新 `RequireActionAttribute.cs` 實作 `IFilterFactory` 以便直接作為 filter 使用：

```csharp
using Microsoft.AspNetCore.Mvc.Filters;

namespace TCS.Web.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireActionAttribute : Attribute, IFilterFactory
{
    public string ActionName { get; }
    public bool IsReusable => false;

    public RequireActionAttribute(string actionName) => ActionName = actionName;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        => new RequireActionFilter(ActionName);
}
```

- [ ] **Step 4: 加入 JWT Bearer NuGet（TCS.Web.csproj）**

在 `<ItemGroup>` 加入：
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
```

- [ ] **Step 5: Commit**

```bash
git add src/TCS.Web/Filters/
git commit -m "feat: add RequireActionAttribute and JWT filter"
```

---

## Phase 9：Controllers

### Task 21: LicenseController + LicensePlantRequirementController → §5-1, §5-2

**Files:**
- Create: `src/TCS.Web/Controllers/LicenseController.cs`

- [ ] **Step 1: 建立 `LicenseController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.DTOs;
using TCS.Core.Interfaces;
using TCS.Web.Filters;

namespace TCS.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/license")]
public class LicenseController : ControllerBase
{
    private readonly ILicenseService _svc;
    public LicenseController(ILicenseService svc) => _svc = svc;

    private string UserId => User.FindFirst("sub")?.Value
        ?? User.FindFirst("employeeId")?.Value ?? "system";

    // ── LicenseMaster ──────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword, [FromQuery] string? category,
        [FromQuery] int? minHours, [FromQuery] int? maxHours,
        [FromQuery] int? minYears, [FromQuery] int? maxYears,
        CancellationToken ct)
    {
        var r = await _svc.GetAllAsync(keyword, category, minHours, maxHours, minYears, maxYears, ct);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("{licenseType}")]
    public async Task<IActionResult> GetByType(string licenseType, CancellationToken ct)
    {
        var r = await _svc.GetByTypeAsync(licenseType, ct);
        return r.Success ? Ok(r) : NotFound(r);
    }

    [HttpPost]
    [RequireAction("新增")]
    public async Task<IActionResult> Create([FromBody] CreateLicenseMasterRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(req, UserId, ct);
        return r.Success ? CreatedAtAction(nameof(GetByType), new { licenseType = req.LicenseType }, r) : BadRequest(r);
    }

    [HttpPut("{licenseType}")]
    [RequireAction("修改")]
    public async Task<IActionResult> Update(string licenseType, [FromBody] UpdateLicenseMasterRequest req, CancellationToken ct)
    {
        var r = await _svc.UpdateAsync(licenseType, req, UserId, ct);
        return r.Success ? Ok(r) : (r.Message.Contains("不存在") ? NotFound(r) : BadRequest(r));
    }

    [HttpDelete("{licenseType}")]
    [RequireAction("刪除")]
    public async Task<IActionResult> Delete(string licenseType, CancellationToken ct)
    {
        var r = await _svc.DeleteAsync(licenseType, ct);
        if (!r.Success)
        {
            if (r.Message.Contains("不存在")) return NotFound(r);
            if (r.Message.Contains("尚有受訓資料")) return Conflict(r);
            return BadRequest(r);
        }
        return NoContent();
    }

    // ── LicensePlantRequirement ─────────────────────────────────────

    [HttpGet("{licenseType}/plants")]
    public async Task<IActionResult> GetRequirements(string licenseType, CancellationToken ct)
    {
        var r = await _svc.GetRequirementsAsync(licenseType, ct);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("{licenseType}/plants")]
    [RequireAction("新增")]
    public async Task<IActionResult> CreateRequirement(
        string licenseType, [FromBody] CreateLicensePlantRequirementRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateRequirementAsync(licenseType, req, UserId, ct);
        return r.Success ? Created("", r) : BadRequest(r);
    }

    [HttpPut("{licenseType}/plants/{plant}")]
    [RequireAction("修改")]
    public async Task<IActionResult> UpdateRequirement(
        string licenseType, string plant, [FromBody] UpdateLicensePlantRequirementRequest req, CancellationToken ct)
    {
        var r = await _svc.UpdateRequirementAsync(licenseType, plant, req, UserId, ct);
        return r.Success ? Ok(r) : (r.Message.Contains("不存在") ? NotFound(r) : BadRequest(r));
    }

    [HttpDelete("{licenseType}/plants/{plant}")]
    [RequireAction("刪除")]
    public async Task<IActionResult> DeleteRequirement(string licenseType, string plant, CancellationToken ct)
    {
        var r = await _svc.DeleteRequirementAsync(licenseType, plant, ct);
        return r.Success ? NoContent() : (r.Message.Contains("不存在") ? NotFound(r) : BadRequest(r));
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/TCS.Web/Controllers/LicenseController.cs
git commit -m "feat: add LicenseController"
```

---

### Task 22: TrainingController + TrainingDetailController → §5-3, §5-4

**Files:**
- Create: `src/TCS.Web/Controllers/TrainingController.cs`

- [ ] **Step 1: 建立 `TrainingController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.DTOs;
using TCS.Core.Interfaces;
using TCS.Web.Filters;

namespace TCS.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/training")]
public class TrainingController : ControllerBase
{
    private readonly ITrainingService _svc;
    public TrainingController(ITrainingService svc) => _svc = svc;

    private string UserId => User.FindFirst("sub")?.Value
        ?? User.FindFirst("employeeId")?.Value ?? "system";

    // ── TrainingHeader ──────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? empId, [FromQuery] string? empName,
        [FromQuery] string? department, [FromQuery] string? licType,
        [FromQuery] bool? expiredOnly, CancellationToken ct)
    {
        var r = await _svc.GetHeadersAsync(empId, empName, department, licType, expiredOnly, ct);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("{empId}/{licType}")]
    public async Task<IActionResult> GetHeader(string empId, string licType, CancellationToken ct)
    {
        var r = await _svc.GetHeaderAsync(empId, licType, ct);
        return r.Success ? Ok(r) : NotFound(r);
    }

    [HttpPost]
    [RequireAction("新增")]
    public async Task<IActionResult> CreateHeader([FromBody] CreateTrainingHeaderRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateHeaderAsync(req, UserId, ct);
        return r.Success ? CreatedAtAction(nameof(GetHeader), new { empId = req.EmployeeId, licType = req.LicenseType }, r) : BadRequest(r);
    }

    [HttpPut("{empId}/{licType}")]
    [RequireAction("修改")]
    public async Task<IActionResult> UpdateHeader(
        string empId, string licType, [FromBody] UpdateTrainingHeaderRequest req, CancellationToken ct)
    {
        var r = await _svc.UpdateHeaderAsync(empId, licType, req, UserId, ct);
        return r.Success ? Ok(r) : (r.Message.Contains("不存在") ? NotFound(r) : BadRequest(r));
    }

    [HttpDelete("{empId}/{licType}")]
    [RequireAction("刪除")]
    public async Task<IActionResult> DeleteHeader(string empId, string licType, CancellationToken ct)
    {
        var r = await _svc.DeleteHeaderAsync(empId, licType, ct);
        return r.Success ? NoContent() : (r.Message.Contains("不存在") ? NotFound(r) : BadRequest(r));
    }

    // ── TrainingDetail ──────────────────────────────────────────────

    [HttpGet("{empId}/{licType}/details")]
    public async Task<IActionResult> GetDetails(string empId, string licType, CancellationToken ct)
    {
        var r = await _svc.GetDetailsAsync(empId, licType, ct);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("{empId}/{licType}/details")]
    [RequireAction("新增")]
    public async Task<IActionResult> CreateDetail(
        string empId, string licType, [FromBody] CreateTrainingDetailRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateDetailAsync(empId, licType, req, UserId, ct);
        return r.Success ? Created("", r) : BadRequest(r);
    }

    [HttpPut("{empId}/{licType}/details/{date}")]
    [RequireAction("修改")]
    public async Task<IActionResult> UpdateDetail(
        string empId, string licType, DateTime date,
        [FromBody] UpdateTrainingDetailRequest req, CancellationToken ct)
    {
        var r = await _svc.UpdateDetailAsync(empId, licType, date, req, UserId, ct);
        return r.Success ? Ok(r) : (r.Message.Contains("不存在") ? NotFound(r) : BadRequest(r));
    }

    [HttpDelete("{empId}/{licType}/details/{date}")]
    [RequireAction("刪除")]
    public async Task<IActionResult> DeleteDetail(
        string empId, string licType, DateTime date, CancellationToken ct)
    {
        var r = await _svc.DeleteDetailAsync(empId, licType, date, ct);
        return r.Success ? NoContent() : BadRequest(r);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/TCS.Web/Controllers/TrainingController.cs
git commit -m "feat: add TrainingController"
```

---

### Task 23: EmployeeController + ExportController → §5-5, §5-6

**Files:**
- Create: `src/TCS.Web/Controllers/EmployeeController.cs`
- Create: `src/TCS.Web/Controllers/ExportController.cs`

- [ ] **Step 1: 建立 `EmployeeController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.Interfaces;

namespace TCS.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/employee")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeRepository _empRepo;
    private readonly IPlantRepository    _plantRepo;

    public EmployeeController(IEmployeeRepository empRepo, IPlantRepository plantRepo)
    {
        _empRepo   = empRepo;
        _plantRepo = plantRepo;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { Message = "關鍵字不可為空" });

        var employees = await _empRepo.SearchAsync(keyword, ct);
        return Ok(new { Data = employees.Select(e => e.ToDto()) });
    }

    [HttpGet("plants")]
    public async Task<IActionResult> GetPlants(CancellationToken ct)
    {
        var plants = await _plantRepo.GetAllAsync(ct);
        return Ok(new { Data = plants.Select(p => p.ToDto()) });
    }
}
```

- [ ] **Step 2: 建立 `ExportController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCS.Core.Interfaces;
using TCS.Web.Filters;

namespace TCS.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportSvc;
    public ExportController(IExportService exportSvc) => _exportSvc = exportSvc;

    [HttpGet("training")]
    [RequireAction("匯出")]
    public async Task<IActionResult> ExportTraining(
        [FromQuery] string? empId, [FromQuery] string? licType, CancellationToken ct)
    {
        var bytes    = await _exportSvc.ExportTrainingAsync(empId, licType, ct);
        var fileName = $"training-export-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/TCS.Web/Controllers/EmployeeController.cs src/TCS.Web/Controllers/ExportController.cs
git commit -m "feat: add EmployeeController and ExportController"
```

---

## Phase 10：Background Service

### Task 24: ExpiryScanService + Tests → §8-4

**Files:**
- Create: `src/TCS.Core/Interfaces/IClock.cs`
- Create: `src/TCS.Infrastructure/Services/SystemClock.cs`
- Create: `src/TCS.Web/Services/ExpiryScanService.cs`
- Create: `tests/TCS.Tests/Services/ExpiryScanServiceTests.cs`

- [ ] **Step 1: 建立 `IClock.cs`**

```csharp
namespace TCS.Core.Interfaces;

public interface IClock
{
    DateTimeOffset Now { get; }
    DateTime Today { get; }
}
```

- [ ] **Step 2: 建立 `SystemClock.cs`**

```csharp
using TCS.Core.Interfaces;

namespace TCS.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTimeOffset Now  => DateTimeOffset.Now;
    public DateTime Today      => DateTime.Today;
}
```

- [ ] **Step 3: 寫失敗測試**

```csharp
// tests/TCS.Tests/Services/ExpiryScanServiceTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TCS.Core.Entities;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;
using TCS.Web.Services;
using Xunit;

namespace TCS.Tests.Services;

public class ExpiryScanServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task ScanAsync_marks_expired_when_hours_insufficient_and_cycle_ended()
    {
        await using var db = CreateInMemoryDb();
        var licenseType = "1.1";
        var empId       = "E001";

        db.LicenseMasters.Add(new LicenseMaster
        {
            LicenseType = licenseType, Description = "Test", Hours = 8, Years = 1,
            Creator = "s", CreateDate = "20240101", Modifier = "s", ModiDate = "20240101"
        });
        db.TrainingHeaders.Add(new TrainingHeader
        {
            EmployeeId = empId, LicenseType = licenseType, RequiredHours = 8,
            Creator = "s", CreateDate = "20240101", Modifier = "s", ModiDate = "20240101"
        });
        db.TrainingDetails.Add(new TrainingDetail
        {
            EmployeeId = empId, LicenseType = licenseType,
            TrainingDate = new DateTime(2023, 1, 1),   // 取得，1 年前
            TrainingType = 1, Hours = 4, IsExpired = false,
            Creator = "s", CreateDate = "20230101", Modifier = "s", ModiDate = "20230101"
        });
        await db.SaveChangesAsync();

        var clockMock = new Mock<IClock>();
        clockMock.Setup(c => c.Today).Returns(new DateTime(2024, 6, 1));   // 週期已結束

        var calcMock = new Mock<IExpiryCalculator>();
        calcMock.Setup(c => c.IsExpired(It.IsAny<IEnumerable<TrainingDetail>>(),
                It.IsAny<LicenseMaster>(), It.IsAny<DateTime>()))
            .Returns(true);

        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(calcMock.Object);
        var sp = services.BuildServiceProvider();

        var logger  = new Mock<ILogger<ExpiryScanService>>().Object;
        var svc     = new ExpiryScanService(sp, logger, clockMock.Object);

        await svc.ScanAsync(CancellationToken.None);

        var detail = await db.TrainingDetails.FirstAsync();
        detail.IsExpired.Should().BeTrue();
    }
}
```

- [ ] **Step 4: 建立 `ExpiryScanService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCS.Core.Interfaces;
using TCS.Infrastructure.Data;

namespace TCS.Web.Services;

public class ExpiryScanService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExpiryScanService> _logger;
    private readonly IClock _clock;

    private static readonly TimeZoneInfo TaipeiZone =
        TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

    public ExpiryScanService(IServiceProvider services,
        ILogger<ExpiryScanService> logger, IClock clock)
    {
        _services = services;
        _logger   = logger;
        _clock    = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayToMidnight();
            await Task.Delay(delay, stoppingToken);
            await ScanAsync(stoppingToken);
        }
    }

    public async Task ScanAsync(CancellationToken ct)
    {
        _logger.LogInformation("ExpiryScanService starting scan at {Time}", _clock.Today);

        using var scope    = _services.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var calc = scope.ServiceProvider.GetRequiredService<IExpiryCalculator>();
        var today = _clock.Today;

        var headers = await db.TrainingHeaders.ToListAsync(ct);

        foreach (var header in headers)
        {
            var license = await db.LicenseMasters
                .FirstOrDefaultAsync(l => l.LicenseType == header.LicenseType, ct);
            if (license is null) continue;

            var details = await db.TrainingDetails
                .Where(d => d.EmployeeId == header.EmployeeId && d.LicenseType == header.LicenseType)
                .OrderBy(d => d.TrainingDate)
                .ToListAsync(ct);

            var isExpired = calc.IsExpired(details, license, today);

            foreach (var detail in details)
            {
                if (detail.IsExpired != isExpired)
                {
                    detail.IsExpired = isExpired;
                    detail.ModiDate  = today.ToString("yyyyMMdd");
                }
            }
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("ExpiryScanService completed scan");
    }

    private TimeSpan CalculateDelayToMidnight()
    {
        var now       = TimeZoneInfo.ConvertTime(_clock.Now, TaipeiZone);
        var midnight  = now.Date.AddDays(1);
        return midnight - now;
    }
}
```

更新 `IExpiryCalculator.cs` 加入 `IsExpired` 方法（Task 14 已定義，此處確認簽章一致）：

```csharp
// IExpiryCalculator 應有此方法（Task 14 中已定義）：
bool IsExpired(IEnumerable<TrainingDetail> details, LicenseMaster license, DateTime today);
```

- [ ] **Step 5: 執行測試**

```bash
dotnet test tests/TCS.Tests --filter ExpiryScanServiceTests
```
Expected: `Passed! - Failed: 0, Passed: 1`

- [ ] **Step 6: Commit**

```bash
git add src/TCS.Core/Interfaces/IClock.cs src/TCS.Infrastructure/Services/SystemClock.cs \
        src/TCS.Web/Services/ExpiryScanService.cs tests/TCS.Tests/Services/ExpiryScanServiceTests.cs
git commit -m "feat: add ExpiryScanService with IClock abstraction"
```

---

## Phase 11：UI（Program.cs + Views）

### Task 25: Program.cs + appsettings.json → §12

**Files:**
- Modify: `src/TCS.Web/Program.cs` (完整 DI、JWT、Swagger、middleware)
- Modify: `src/TCS.Web/appsettings.json`
- Create: `src/TCS.Web/appsettings.Development.json`

- [ ] **Step 1: 完整 `Program.cs`**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TCS.Core.Interfaces;
using TCS.Core.Services;
using TCS.Infrastructure.Data;
using TCS.Infrastructure.Repositories;
using TCS.Infrastructure.Services;
using TCS.Web.Middleware;
using TCS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── EF Core ────────────────────────────────────────────────────────
var useInMemory = builder.Configuration.GetValue<bool>("USE_INMEMORY_DB")
    || Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true";

if (useInMemory)
{
    builder.Services.AddDbContext<AppDbContext>(opts =>
        opts.UseInMemoryDatabase("TcsDb"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opts =>
        opts.UseSqlServer(builder.Configuration.GetConnectionString("TcsDb")));
}

// ── Repositories ───────────────────────────────────────────────────
builder.Services.AddScoped<ILicenseRepository,  LicenseRepository>();
builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IPlantRepository,    PlantRepository>();

// ── Services ───────────────────────────────────────────────────────
builder.Services.AddScoped<IExpiryCalculator, ExpiryCalculator>();
builder.Services.AddScoped<ILicenseService,   LicenseService>();
builder.Services.AddScoped<ITrainingService,  TrainingService>();
builder.Services.AddScoped<IExportService,    ExcelExportService>();
builder.Services.AddSingleton<IClock,         SystemClock>();

// ── Background Service ────────────────────────────────────────────
builder.Services.AddHostedService<ExpiryScanService>();

// ── Auth / JWT ────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSection["Issuer"],
            ValidAudience            = jwtSection["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["Key"]!))
        };
    });
builder.Services.AddAuthorization();

// ── MVC / JSON ────────────────────────────────────────────────────
builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.PropertyNamingPolicy = null;  // PascalCase
});

// ── Swagger ───────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In   = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            }, []
        }
    });
});

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────
app.UseExceptionHandlingMiddleware();  // DingxinErpTemplate pattern

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── InMemory seed ─────────────────────────────────────────────────
if (useInMemory)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.SeedAsync(db);
}

app.Run();
```

- [ ] **Step 2: 更新 `appsettings.json`**

```json
{
  "ConnectionStrings": {
    "TcsDb": "Server=.;Database=TCS;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Issuer":   "PLACEHOLDER_ISSUER",
    "Audience": "PLACEHOLDER_AUDIENCE",
    "Key":      "PLACEHOLDER_KEY_32_CHARS_MINIMUM_00"
  },
  "USE_INMEMORY_DB": false,
  "Logging": {
    "LogLevel": {
      "Default":                  "Information",
      "Microsoft.AspNetCore":     "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 3: 建立 `appsettings.Development.json`**

```json
{
  "USE_INMEMORY_DB": true,
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Web/Program.cs src/TCS.Web/appsettings*.json
git commit -m "feat: configure Program.cs, DI, JWT, Swagger"
```

---

### Task 26: License View + JS → §7

**Files:**
- Create: `src/TCS.Web/Views/License/Index.cshtml`
- Create: `src/TCS.Web/wwwroot/js/tcs-common.js`
- Create: `src/TCS.Web/wwwroot/js/license-page.js`

- [ ] **Step 1: 建立 `tcs-common.js`（JWT 共用）**

```javascript
// wwwroot/js/tcs-common.js
const TCS = {
    token: localStorage.getItem('tcs_token') ?? '',

    authHeaders() {
        return { 'Authorization': `Bearer ${this.token}`, 'Content-Type': 'application/json' };
    },

    async apiFetch(url, options = {}) {
        const resp = await fetch(url, { ...options, headers: { ...this.authHeaders(), ...options.headers } });
        if (resp.status === 401) { alert('請重新登入'); return null; }
        if (resp.status === 403) { alert('您沒有執行此操作的權限'); return null; }
        return resp;
    },

    showToast(msg, isError = false) {
        const el = document.getElementById('tcs-toast');
        if (!el) return;
        el.textContent = msg;
        el.className = `toast align-items-center text-bg-${isError ? 'danger' : 'success'} border-0 show`;
        setTimeout(() => el.classList.remove('show'), 3000);
    }
};
```

- [ ] **Step 2: 建立 `license-page.js`**

```javascript
// wwwroot/js/license-page.js
let editingType = null;

async function loadLicenses() {
    const keyword  = document.getElementById('searchKeyword').value;
    const category = document.getElementById('searchCategory').value;
    const resp = await TCS.apiFetch(`/api/license?keyword=${encodeURIComponent(keyword)}&category=${encodeURIComponent(category)}`);
    if (!resp) return;
    const json = await resp.json();
    renderTable(json.Data);
}

function renderTable(items) {
    const tbody = document.getElementById('licenseTableBody');
    tbody.innerHTML = '';
    for (const item of items) {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${item.LicenseType}</td>
            <td>${item.Description}</td>
            <td>${item.Category ?? '—'}</td>
            <td>${item.Hours ?? '—'}</td>
            <td>${item.Years ?? '—'}</td>
            <td>
                <button class="btn btn-sm btn-outline-primary" onclick="openEdit('${item.LicenseType}')">修改</button>
                <button class="btn btn-sm btn-outline-danger"  onclick="deleteLicense('${item.LicenseType}')">刪除</button>
            </td>`;
        tbody.appendChild(tr);
    }
}

async function openEdit(licenseType) {
    editingType = licenseType;
    const resp = await TCS.apiFetch(`/api/license/${licenseType}`);
    if (!resp) return;
    const json = await resp.json();
    const d = json.Data;
    document.getElementById('editLicenseType').value  = d.LicenseType;
    document.getElementById('editDescription').value  = d.Description;
    document.getElementById('editCategory').value     = d.Category ?? '';
    document.getElementById('editHours').value        = d.Hours ?? '';
    document.getElementById('editYears').value        = d.Years ?? '';
    new bootstrap.Modal(document.getElementById('editModal')).show();
}

async function saveEdit() {
    const req = {
        Description: document.getElementById('editDescription').value,
        Category:    document.getElementById('editCategory').value || null,
        Hours:       parseInt(document.getElementById('editHours').value) || null,
        Years:       parseInt(document.getElementById('editYears').value) || null
    };
    const resp = await TCS.apiFetch(`/api/license/${editingType}`, {
        method: 'PUT',
        body: JSON.stringify(req)
    });
    if (!resp) return;
    if (resp.ok) { TCS.showToast('修改成功'); bootstrap.Modal.getInstance(document.getElementById('editModal')).hide(); loadLicenses(); }
    else { const j = await resp.json(); TCS.showToast(j.Message ?? '修改失敗', true); }
}

async function deleteLicense(licenseType) {
    if (!confirm(`確定刪除 ${licenseType}？`)) return;
    const resp = await TCS.apiFetch(`/api/license/${licenseType}`, { method: 'DELETE' });
    if (!resp) return;
    if (resp.status === 204) { TCS.showToast('刪除成功'); loadLicenses(); }
    else if (resp.status === 409) { TCS.showToast('尚有受訓資料引用此證照，無法刪除', true); }
    else { TCS.showToast('刪除失敗', true); }
}

document.addEventListener('DOMContentLoaded', loadLicenses);
```

- [ ] **Step 3: 建立 `Views/License/Index.cshtml`**

```html
@{
    ViewData["Title"] = "證照資料維護";
    Layout = "~/Views/Shared/_Layout.cshtml";
}

<div class="container-fluid mt-3">
    <h4>證照資料維護</h4>

    <!-- Search Bar -->
    <div class="row g-2 mb-3">
        <div class="col-auto">
            <input id="searchKeyword" class="form-control" placeholder="關鍵字" />
        </div>
        <div class="col-auto">
            <input id="searchCategory" class="form-control" placeholder="大類" />
        </div>
        <div class="col-auto">
            <button class="btn btn-primary" onclick="loadLicenses()">查詢</button>
        </div>
        <div class="col-auto ms-auto">
            <button class="btn btn-success" data-bs-toggle="modal" data-bs-target="#createModal">新增</button>
        </div>
    </div>

    <!-- Table -->
    <table class="table table-bordered table-hover table-sm">
        <thead class="table-light">
            <tr>
                <th>證照類別</th><th>描述</th><th>大類</th>
                <th>訓練時數</th><th>效期(年)</th><th>操作</th>
            </tr>
        </thead>
        <tbody id="licenseTableBody"></tbody>
    </table>
</div>

<!-- Create Modal -->
<div class="modal fade" id="createModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header"><h5 class="modal-title">新增證照</h5></div>
            <div class="modal-body">
                <div class="mb-2"><label class="form-label">證照類別</label><input id="newLicenseType" class="form-control" /></div>
                <div class="mb-2"><label class="form-label">描述</label><input id="newDescription" class="form-control" /></div>
                <div class="mb-2"><label class="form-label">大類（選填）</label><input id="newCategory" class="form-control" /></div>
                <div class="mb-2"><label class="form-label">訓練時數</label><input id="newHours" type="number" class="form-control" /></div>
                <div class="mb-2"><label class="form-label">效期(年)</label><input id="newYears" type="number" class="form-control" /></div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">取消</button>
                <button type="button" class="btn btn-success" onclick="createLicense()">確定新增</button>
            </div>
        </div>
    </div>
</div>

<!-- Edit Modal -->
<div class="modal fade" id="editModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header"><h5 class="modal-title">修改證照</h5></div>
            <div class="modal-body">
                <div class="mb-2"><label class="form-label">證照類別</label><input id="editLicenseType" class="form-control" readonly /></div>
                <div class="mb-2"><label class="form-label">描述</label><input id="editDescription" class="form-control" /></div>
                <div class="mb-2"><label class="form-label">大類（選填）</label><input id="editCategory" class="form-control" /></div>
                <div class="mb-2"><label class="form-label">訓練時數</label><input id="editHours" type="number" class="form-control" /></div>
                <div class="mb-2"><label class="form-label">效期(年)</label><input id="editYears" type="number" class="form-control" /></div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">取消</button>
                <button type="button" class="btn btn-primary" onclick="saveEdit()">儲存</button>
            </div>
        </div>
    </div>
</div>

<!-- Toast -->
<div class="position-fixed bottom-0 end-0 p-3" style="z-index:11">
    <div id="tcs-toast" class="toast" role="alert" aria-live="assertive" aria-atomic="true"></div>
</div>

@section Scripts {
    <script src="~/js/tcs-common.js"></script>
    <script src="~/js/license-page.js"></script>
    <script>
        async function createLicense() {
            const req = {
                LicenseType: document.getElementById('newLicenseType').value,
                Description: document.getElementById('newDescription').value,
                Category:    document.getElementById('newCategory').value || null,
                Hours:       parseInt(document.getElementById('newHours').value) || null,
                Years:       parseInt(document.getElementById('newYears').value) || null
            };
            const resp = await TCS.apiFetch('/api/license', { method: 'POST', body: JSON.stringify(req) });
            if (!resp) return;
            if (resp.ok) {
                TCS.showToast('新增成功');
                bootstrap.Modal.getInstance(document.getElementById('createModal')).hide();
                loadLicenses();
            } else {
                const j = await resp.json();
                TCS.showToast(j.Message ?? '新增失敗', true);
            }
        }
    </script>
}
```

- [ ] **Step 4: Commit**

```bash
git add src/TCS.Web/Views/License/ src/TCS.Web/wwwroot/js/tcs-common.js src/TCS.Web/wwwroot/js/license-page.js
git commit -m "feat: add License index view and JS"
```

---

### Task 27: Training View + JS → §7

**Files:**
- Create: `src/TCS.Web/Views/Training/Index.cshtml`
- Create: `src/TCS.Web/wwwroot/js/training-page.js`

- [ ] **Step 1: 建立 `training-page.js`**

```javascript
// wwwroot/js/training-page.js
let selectedEmpId  = null;
let selectedLicType = null;

async function loadTrainings() {
    const empId      = document.getElementById('searchEmpId').value;
    const licType    = document.getElementById('searchLicType').value;
    const expiredOnly = document.getElementById('searchExpiredOnly').checked;

    const params = new URLSearchParams();
    if (empId)      params.append('empId',      empId);
    if (licType)    params.append('licType',     licType);
    if (expiredOnly) params.append('expiredOnly', 'true');

    const resp = await TCS.apiFetch(`/api/training?${params}`);
    if (!resp) return;
    const json = await resp.json();
    renderHeaderTable(json.Data);
}

function renderHeaderTable(items) {
    const tbody = document.getElementById('trainingTableBody');
    tbody.innerHTML = '';
    for (const h of items) {
        const expiredBadge = h.IsExpired
            ? '<span class="badge text-bg-danger">已逾期</span>'
            : '<span class="badge text-bg-success">正常</span>';
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${h.EmployeeId}</td>
            <td>${h.EmployeeName ?? '—'}</td>
            <td>${h.LicenseType}</td>
            <td>${h.LicenseDescription ?? '—'}</td>
            <td>${h.AccumulatedHours ?? 0} / ${h.RequiredHours}</td>
            <td>${expiredBadge}</td>
            <td>
                <button class="btn btn-sm btn-outline-secondary"
                    onclick="loadDetails('${h.EmployeeId}','${h.LicenseType}')">明細</button>
                <button class="btn btn-sm btn-outline-danger"
                    onclick="deleteHeader('${h.EmployeeId}','${h.LicenseType}')">刪除</button>
            </td>`;
        tbody.appendChild(tr);
    }
}

async function loadDetails(empId, licType) {
    selectedEmpId   = empId;
    selectedLicType = licType;
    document.getElementById('detailHeaderLabel').textContent = `${empId} / ${licType} 受訓明細`;

    const resp = await TCS.apiFetch(`/api/training/${empId}/${licType}/details`);
    if (!resp) return;
    const json = await resp.json();
    renderDetailTable(json.Data);

    new bootstrap.Modal(document.getElementById('detailModal')).show();
}

function renderDetailTable(items) {
    const tbody = document.getElementById('detailTableBody');
    tbody.innerHTML = '';
    for (const d of items) {
        const typeLabel = d.TrainingType === 1 ? '取得證照' : '回訓';
        const expiredBadge = d.IsExpired ? '<span class="badge text-bg-danger">逾期</span>' : '';
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${d.TrainingDate?.substring(0,10) ?? ''}</td>
            <td>${typeLabel}</td>
            <td>${d.Hours}</td>
            <td>${expiredBadge}</td>
            <td>
                <button class="btn btn-sm btn-outline-danger"
                    onclick="deleteDetail('${d.TrainingDate}')">刪除</button>
            </td>`;
        tbody.appendChild(tr);
    }
}

async function deleteHeader(empId, licType) {
    if (!confirm(`確定刪除 ${empId}/${licType} 的所有受訓資料？`)) return;
    const resp = await TCS.apiFetch(`/api/training/${empId}/${licType}`, { method: 'DELETE' });
    if (resp?.status === 204) { TCS.showToast('刪除成功'); loadTrainings(); }
    else TCS.showToast('刪除失敗', true);
}

async function deleteDetail(trainingDate) {
    if (!confirm(`確定刪除 ${trainingDate} 的受訓明細？`)) return;
    const resp = await TCS.apiFetch(
        `/api/training/${selectedEmpId}/${selectedLicType}/details/${encodeURIComponent(trainingDate)}`,
        { method: 'DELETE' });
    if (resp?.status === 204) { TCS.showToast('刪除成功'); loadDetails(selectedEmpId, selectedLicType); }
    else { const j = await resp?.json(); TCS.showToast(j?.Message ?? '刪除失敗', true); }
}

async function createDetail() {
    const req = {
        TrainingDate: document.getElementById('newDetailDate').value,
        TrainingType: parseInt(document.getElementById('newDetailType').value),
        Hours:        parseFloat(document.getElementById('newDetailHours').value)
    };
    const resp = await TCS.apiFetch(
        `/api/training/${selectedEmpId}/${selectedLicType}/details`,
        { method: 'POST', body: JSON.stringify(req) });
    if (!resp) return;
    if (resp.ok) { TCS.showToast('新增成功'); loadDetails(selectedEmpId, selectedLicType); }
    else { const j = await resp.json(); TCS.showToast(j.Message ?? '新增失敗', true); }
}

document.addEventListener('DOMContentLoaded', loadTrainings);
```

- [ ] **Step 2: 建立 `Views/Training/Index.cshtml`**

```html
@{
    ViewData["Title"] = "受訓資料管理";
    Layout = "~/Views/Shared/_Layout.cshtml";
}

<div class="container-fluid mt-3">
    <h4>受訓資料管理</h4>

    <div class="row g-2 mb-3">
        <div class="col-auto">
            <input id="searchEmpId" class="form-control" placeholder="員工編號" />
        </div>
        <div class="col-auto">
            <input id="searchLicType" class="form-control" placeholder="證照類別" />
        </div>
        <div class="col-auto d-flex align-items-center gap-1">
            <input type="checkbox" id="searchExpiredOnly" class="form-check-input" />
            <label for="searchExpiredOnly">僅顯示逾期</label>
        </div>
        <div class="col-auto">
            <button class="btn btn-primary" onclick="loadTrainings()">查詢</button>
        </div>
        <div class="col-auto ms-auto">
            <a href="/api/export/training" class="btn btn-outline-secondary">匯出 Excel</a>
        </div>
    </div>

    <table class="table table-bordered table-hover table-sm">
        <thead class="table-light">
            <tr>
                <th>員工編號</th><th>姓名</th><th>證照類別</th><th>描述</th>
                <th>時數（已/應）</th><th>狀態</th><th>操作</th>
            </tr>
        </thead>
        <tbody id="trainingTableBody"></tbody>
    </table>
</div>

<!-- Detail Modal -->
<div class="modal fade" id="detailModal" tabindex="-1">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="detailHeaderLabel">受訓明細</h5>
            </div>
            <div class="modal-body">
                <!-- Add Detail -->
                <div class="row g-2 mb-3">
                    <div class="col-auto">
                        <input type="date" id="newDetailDate" class="form-control" />
                    </div>
                    <div class="col-auto">
                        <select id="newDetailType" class="form-select">
                            <option value="1">取得證照</option>
                            <option value="2">回訓</option>
                        </select>
                    </div>
                    <div class="col-auto">
                        <input id="newDetailHours" type="number" step="0.5" class="form-control" placeholder="時數" />
                    </div>
                    <div class="col-auto">
                        <button class="btn btn-success" onclick="createDetail()">新增</button>
                    </div>
                </div>

                <table class="table table-sm">
                    <thead>
                        <tr><th>訓練日期</th><th>類型</th><th>時數</th><th>狀態</th><th>操作</th></tr>
                    </thead>
                    <tbody id="detailTableBody"></tbody>
                </table>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">關閉</button>
            </div>
        </div>
    </div>
</div>

<!-- Toast -->
<div class="position-fixed bottom-0 end-0 p-3" style="z-index:11">
    <div id="tcs-toast" class="toast" role="alert" aria-live="assertive" aria-atomic="true"></div>
</div>

@section Scripts {
    <script src="~/js/tcs-common.js"></script>
    <script src="~/js/training-page.js"></script>
}
```

- [ ] **Step 3: Commit**

```bash
git add src/TCS.Web/Views/Training/ src/TCS.Web/wwwroot/js/training-page.js
git commit -m "feat: add Training index view and JS"
```

---

## Phase 12：Seed Data

### Task 28: InMemory Seed + USE_INMEMORY_DB → §12

**Files:**
- Create: `src/TCS.Infrastructure/Data/SeedData.cs`

- [ ] **Step 1: 建立 `SeedData.cs`**

```csharp
using TCS.Core.Entities;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.LicenseMasters.Any()) return;

        // ── LicenseMaster ──────────────────────────────────────────
        var licenses = new[]
        {
            new LicenseMaster { LicenseType="1",   Description="電氣大類",    Category=null, Hours=null, Years=null, Creator="seed", CreateDate="20240101", Modifier="seed", ModiDate="20240101", Flag=0 },
            new LicenseMaster { LicenseType="1.1", Description="低壓電氣作業", Category="1",  Hours=16,  Years=3,    Creator="seed", CreateDate="20240101", Modifier="seed", ModiDate="20240101", Flag=0 },
            new LicenseMaster { LicenseType="1.2", Description="高壓電氣作業", Category="1",  Hours=24,  Years=3,    Creator="seed", CreateDate="20240101", Modifier="seed", ModiDate="20240101", Flag=0 },
        };
        db.LicenseMasters.AddRange(licenses);

        // ── Plants ────────────────────────────────────────────────
        // Plants come from CMSMB view; skip seeding (read-only)

        // ── TrainingHeaders + TrainingDetails ─────────────────────
        var headers = new[]
        {
            new TrainingHeader { EmployeeId="E001", LicenseType="1.1", RequiredHours=16, Creator="seed", CreateDate="20240101", Modifier="seed", ModiDate="20240101" },
            new TrainingHeader { EmployeeId="E002", LicenseType="1.1", RequiredHours=16, Creator="seed", CreateDate="20240101", Modifier="seed", ModiDate="20240101" },
        };
        db.TrainingHeaders.AddRange(headers);

        var details = new[]
        {
            // E001: 一個完整週期（已通過）
            new TrainingDetail { EmployeeId="E001", LicenseType="1.1", TrainingDate=new DateTime(2023,1,5),  TrainingType=1, Hours=10, IsExpired=false, Creator="seed", CreateDate="20230105", Modifier="seed", ModiDate="20230105" },
            new TrainingDetail { EmployeeId="E001", LicenseType="1.1", TrainingDate=new DateTime(2023,3,15), TrainingType=2, Hours=8,  IsExpired=false, Creator="seed", CreateDate="20230315", Modifier="seed", ModiDate="20230315" },

            // E002: 尚未完成（時數不足）
            new TrainingDetail { EmployeeId="E002", LicenseType="1.1", TrainingDate=new DateTime(2023,6,1),  TrainingType=1, Hours=4,  IsExpired=false, Creator="seed", CreateDate="20230601", Modifier="seed", ModiDate="20230601" },
        };
        db.TrainingDetails.AddRange(details);

        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/TCS.Infrastructure/Data/SeedData.cs
git commit -m "feat: add InMemory seed data"
```

---

## Phase 13：Smoke Tests

### Task 29: Build + Test + Manual Smoke Check

**Files:** (無新增，只執行指令)

- [ ] **Step 1: 完整 build**

```bash
dotnet build TCS.sln
```
Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 2: 執行所有單元測試**

```bash
dotnet test tests/TCS.Tests --verbosity normal
```
Expected: `Passed! - Failed: 0` (all tests passing)

- [ ] **Step 3: 啟動 InMemory 模式**

```bash
cd src/TCS.Web
dotnet run --environment Development
```
Expected output includes: `Now listening on: https://localhost:5001`

- [ ] **Step 4: Swagger 煙霧測試**

Open: `https://localhost:5001/swagger`

Verify:
- GET `/api/license` → 200 + seed data (LicenseType=1, 1.1, 1.2)
- GET `/api/training` → 200 + seed training headers
- GET `/api/employee/plants` → 200 (empty list in InMemory is acceptable)

- [ ] **Step 5: Auth 煙霧測試**

```bash
# 無 token 應返回 401
curl -s -o /dev/null -w "%{http_code}" https://localhost:5001/api/license
# Expected: 401

# 有效 token (使用外部系統提供的測試 token) 應返回 200
# Note: JWT Issuer/Audience 需先在 appsettings 設定正確值
```

- [ ] **Step 6: 最終 Commit**

```bash
git add .
git commit -m "chore: final smoke test verification"
```

---

*計畫完成。共 29 Tasks，13 Phases。*
