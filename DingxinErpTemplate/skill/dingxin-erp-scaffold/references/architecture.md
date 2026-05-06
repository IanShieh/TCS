# Clean Architecture 規範 — 鼎新 ERP 作業轉換

## 三層架構

```
[英文名].Web (表現層)
  ├── Controllers/     → API Controller (ControllerBase) + MVC Controller (Controller)
  ├── Views/           → Razor Views (.cshtml)
  ├── Middleware/       → ExceptionHandlingMiddleware
  ├── wwwroot/         → 靜態檔案 (JS/CSS)
  └── Program.cs       → DI 註冊 + Middleware 管線

[英文名].Core (核心層 — 無外部依賴，僅 FluentValidation)
  ├── Common/          → CrudResult<T>, PagedResult<T>, IAuditableEntity
  ├── Entities/        → ERP 表格 Entity (屬性名 = ERP 欄位名)
  ├── DTOs/            → 資料傳輸物件 + MappingExtensions
  ├── Interfaces/      → IRepository, IService 介面
  ├── Services/        → 業務邏輯實作
  └── Validators/      → FluentValidation 驗證器

[英文名].Infrastructure (基礎設施層)
  ├── Data/            → AppDbContext
  ├── Configurations/  → IEntityTypeConfiguration<T>
  └── Repositories/    → Repository 實作
```

## 依賴方向

```
Web → Core ← Infrastructure
     ↑↑↑
  Core 不依賴任何人
```

## Namespace 命名

- `[英文名].Web.Controllers`
- `[英文名].Core.Entities`
- `[英文名].Core.DTOs`
- `[英文名].Core.Interfaces`
- `[英文名].Core.Services`
- `[英文名].Core.Validators`
- `[英文名].Core.Common`
- `[英文名].Infrastructure.Data`
- `[英文名].Infrastructure.Configurations`
- `[英文名].Infrastructure.Repositories`

## DI 註冊順序 (Program.cs)

```csharp
// 1. DbContext
builder.Services.AddDbContext<AppDbContext>(options => ...);

// 2. Repository
builder.Services.AddScoped<IXxxRepository, XxxRepository>();

// 3. Service
builder.Services.AddScoped<IXxxService, XxxService>();

// 4. FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateXxxValidator>();
builder.Services.AddFluentValidationAutoValidation();

// 5. MVC
builder.Services.AddControllersWithViews();

// 6. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(...);
```
