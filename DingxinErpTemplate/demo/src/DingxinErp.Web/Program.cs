using DingxinErp.Core.Entities;
using DingxinErp.Core.Interfaces;
using DingxinErp.Core.Services;
using DingxinErp.Core.Validators;
using DingxinErp.Infrastructure.Data;
using DingxinErp.Infrastructure.Repositories;
using DingxinErp.Web.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Net;

// 為 SQL Server 2008 R2 (TLS 1.0) 設定安全協定（必須在任何連線前設定）
#pragma warning disable SYSLIB0014
ServicePointManager.SecurityProtocol =
    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
#pragma warning restore SYSLIB0014

var builder = WebApplication.CreateBuilder(args);

// ========== 資料庫連線 ==========
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var forceInMemory = string.Equals(Environment.GetEnvironmentVariable("USE_INMEMORY_DB"), "true", StringComparison.OrdinalIgnoreCase);
var useInMemory = forceInMemory || string.IsNullOrEmpty(connectionString) || connectionString.Contains("YOUR_SERVER");

if (useInMemory)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("DingxinErpDemo"));
    Console.WriteLine("✅ 使用 InMemory 資料庫模式");
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.UseCompatibilityLevel(100); // SQL Server 2008 R2
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
        if (builder.Environment.IsDevelopment())
            options.EnableSensitiveDataLogging();
    });
    Console.WriteLine("✅ 使用 SQL Server 資料庫模式");
}

// ========== Repository 註冊 ==========
builder.Services.AddScoped<ISampleRepository, SampleRepository>();

// ========== Service 註冊 ==========
builder.Services.AddScoped<ISampleService, SampleService>();

// ========== FluentValidation ==========
builder.Services.AddValidatorsFromAssemblyContaining<CreateSampleHeaderValidator>();
builder.Services.AddFluentValidationAutoValidation();

// ========== MVC (Controller + Views) ==========
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // 保持屬性名稱不轉換 (ERP 欄位名 TA001 不會變成 tA001)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// ========== Swagger (開發環境) ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "鼎新 ERP 作業 API",
        Version = "v1",
        Description = "鼎新 ERP 作業轉換 Web App — CRUD + 搜尋 API"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ========== 示範模式：自動建立種子資料 ==========
if (useInMemory)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedDemoData(db);
}

// ========== 中介軟體管線 ==========
app.UseExceptionHandlingMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ========== 種子資料方法 ==========
static void SeedDemoData(AppDbContext db)
{
    if (db.SampleHeaders.Any()) return;

    var today = DateTime.Now.ToString("yyyyMMdd");

    var headers = new[]
    {
        new SampleHeader
        {
            TA001 = "3301", TA002 = "20260001001", TA003 = today,
            TA004 = "A0001     ", TA005 = "台北科技公司", TA006 = "範例採購單",
            TA007 = "Y", Creator = "ADMIN", CreateDate = today, Modifier = "ADMIN", ModiDate = today, Flag = 1,
            Details = new List<SampleDetail>
            {
                new() { TB001 = "3301", TB002 = "20260001001", TB003 = "0001", TB004 = "BOM-A001", TB005 = "主機板 A1", TB006 = 10, TB007 = 1500, TB008 = 15000, TB009 = "第一批" },
                new() { TB001 = "3301", TB002 = "20260001001", TB003 = "0002", TB004 = "BOM-A002", TB005 = "記憶體 DDR5", TB006 = 20, TB007 = 800, TB008 = 16000, TB009 = "" },
                new() { TB001 = "3301", TB002 = "20260001001", TB003 = "0003", TB004 = "BOM-A003", TB005 = "SSD 固態硬碟", TB006 = 15, TB007 = 2000, TB008 = 30000, TB009 = "含安裝" },
            }
        },
        new SampleHeader
        {
            TA001 = "3301", TA002 = "20260001002", TA003 = today,
            TA004 = "B0002     ", TA005 = "新竹半導體", TA006 = "晶片採購",
            TA007 = "Y", Creator = "ADMIN", CreateDate = today, Modifier = "ADMIN", ModiDate = today, Flag = 1,
            Details = new List<SampleDetail>
            {
                new() { TB001 = "3301", TB002 = "20260001002", TB003 = "0001", TB004 = "IC-7001", TB005 = "控制晶片 IC7", TB006 = 100, TB007 = 350, TB008 = 35000, TB009 = "" },
                new() { TB001 = "3301", TB002 = "20260001002", TB003 = "0002", TB004 = "IC-7002", TB005 = "電源管理 IC", TB006 = 200, TB007 = 120, TB008 = 24000, TB009 = "替代料" },
            }
        },
        new SampleHeader
        {
            TA001 = "3302", TA002 = "20260002001", TA003 = today,
            TA004 = "C0003     ", TA005 = "高雄物流中心", TA006 = "包材採購",
            TA007 = "Y", Creator = "ADMIN", CreateDate = today, Modifier = "ADMIN", ModiDate = today, Flag = 1,
            Details = new List<SampleDetail>
            {
                new() { TB001 = "3302", TB002 = "20260002001", TB003 = "0001", TB004 = "PKG-001", TB005 = "紙箱 50x40x30", TB006 = 500, TB007 = 25, TB008 = 12500, TB009 = "" },
            }
        },
        new SampleHeader
        {
            TA001 = "3302", TA002 = "20260002002", TA003 = today,
            TA004 = "A0001     ", TA005 = "台北科技公司", TA006 = "",
            TA007 = "N", Creator = "ADMIN", CreateDate = today, Modifier = "ADMIN", ModiDate = today, Flag = 1,
            Details = new List<SampleDetail>()
        },
    };

    db.SampleHeaders.AddRange(headers);
    db.SaveChanges();
}
