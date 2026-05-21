using ERP.Auth.Common.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TCS.Core.Interfaces;
using TCS.Core.Services;
using TCS.Core.Validators.License;
using TCS.Infrastructure.Data;
using TCS.Infrastructure.Repositories;
using TCS.Infrastructure.Services;
using TCS.Web.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IPlantRepository, PlantRepository>();

// Services
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

// Validators
builder.Services.AddValidatorsFromAssembly(typeof(CreateLicenseMasterValidator).Assembly);

// MVC + global action filter
builder.Services.AddControllersWithViews(o => o.Filters.Add<RequireActionFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);

// ERP 共用 JWT + Cookie 認證（替換原本手動 JWT 設定）
builder.Services.AddErpAuth(builder.Configuration);

// Swagger (dev only)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UsePathBase("/tcs");        // 必須在 UseStaticFiles 之前

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// ERP 共用端點：POST /api/auth/token-login、GET /api/auth/verify、GET /session-expired
app.MapErpAuthEndpoints();

app.MapControllers();
app.MapControllerRoute("default", "{controller=License}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
