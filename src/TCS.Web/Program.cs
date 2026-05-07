using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TCS.Core.Interfaces;
using TCS.Core.Services;
using TCS.Core.Validators.License;
using TCS.Infrastructure.Data;
using TCS.Infrastructure.Persistence;
using TCS.Infrastructure.Repositories;
using TCS.Infrastructure.Services;
using TCS.Web.Filters;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// DB: InMemory fallback for development / SQL Server for production
var useInMemory = builder.Configuration["USE_INMEMORY_DB"] == "true"
    || string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection"));

if (useInMemory)
    builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TcsDb"));
else
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
builder.Services.AddScoped<IExpiryCalculator, ExpiryCalculator>();
builder.Services.AddSingleton<IAppTimeProvider, SystemTimeProvider>();
builder.Services.AddHostedService<ExpiryScanService>();

// Validators
builder.Services.AddValidatorsFromAssembly(typeof(CreateLicenseMasterValidator).Assembly);

// MVC + filter
builder.Services.AddControllersWithViews(o => o.Filters.Add<RequireActionFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

// Swagger (dev only)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
app.MapControllers();
app.MapControllerRoute("default", "{controller=License}/{action=Index}/{id?}");

// Seed on startup in InMemory mode
if (useInMemory)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();

public partial class Program { }
