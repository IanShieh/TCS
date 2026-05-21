using TCS.Core.Entities;
using TCS.Infrastructure.Data;

namespace TCS.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.LicenseMasters.Any()) return;

        // Parent categories (大類)
        db.LicenseMasters.AddRange(
            new LicenseMaster { LicenseType = "1", Description = "電氣類", Category = null, Hours = null, Years = null },
            new LicenseMaster { LicenseType = "2", Description = "機械類", Category = null, Hours = null, Years = null }
        );

        // Sub-types (小類)
        db.LicenseMasters.AddRange(
            new LicenseMaster { LicenseType = "1.1", Description = "低壓電氣作業", Category = "1", Hours = 16, Years = 2 },
            new LicenseMaster { LicenseType = "1.2", Description = "高壓電氣作業", Category = "1", Hours = 24, Years = 3 },
            new LicenseMaster { LicenseType = "2.1", Description = "堆高機操作", Category = "2", Hours = 8, Years = 1 }
        );

        await db.SaveChangesAsync();

        // License plant requirements
        db.LicensePlantRequirements.AddRange(
            new LicensePlantRequirement { LicenseType = "1.1", Plant = "TW01", RequiredCount = 2 },
            new LicensePlantRequirement { LicenseType = "1.1", Plant = "TW02", RequiredCount = 1 },
            new LicensePlantRequirement { LicenseType = "1.2", Plant = "TW01", RequiredCount = 1 },
            new LicensePlantRequirement { LicenseType = "2.1", Plant = "TW01", RequiredCount = 3 },
            new LicensePlantRequirement { LicenseType = "2.1", Plant = "TW02", RequiredCount = 2 }
        );
        await db.SaveChangesAsync();

        // Training headers
        var headers = new[]
        {
            new TrainingHeader { EmployeeId = "E001", LicenseType = "1.1", RequiredHours = 16 },
            new TrainingHeader { EmployeeId = "E001", LicenseType = "2.1", RequiredHours = 8 },
            new TrainingHeader { EmployeeId = "E002", LicenseType = "1.1", RequiredHours = 16 },
            new TrainingHeader { EmployeeId = "E002", LicenseType = "1.2", RequiredHours = 24 },
            new TrainingHeader { EmployeeId = "E003", LicenseType = "2.1", RequiredHours = 8 }
        };
        db.TrainingHeaders.AddRange(headers);
        await db.SaveChangesAsync();

        // Training details
        db.TrainingDetails.AddRange(
            new TrainingDetail { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = DateTime.Today.AddYears(-1), TrainingType = 1, Hours = 0m },
            new TrainingDetail { EmployeeId = "E001", LicenseType = "1.1", TrainingDate = DateTime.Today.AddMonths(-6), TrainingType = 2, Hours = 16m },
            new TrainingDetail { EmployeeId = "E001", LicenseType = "2.1", TrainingDate = DateTime.Today.AddMonths(-3), TrainingType = 1, Hours = 0m },
            new TrainingDetail { EmployeeId = "E002", LicenseType = "1.1", TrainingDate = DateTime.Today.AddYears(-3), TrainingType = 1, Hours = 0m },
            new TrainingDetail { EmployeeId = "E002", LicenseType = "1.1", TrainingDate = DateTime.Today.AddYears(-3).AddMonths(1), TrainingType = 2, Hours = 4m },
            new TrainingDetail { EmployeeId = "E002", LicenseType = "1.2", TrainingDate = DateTime.Today.AddYears(-1), TrainingType = 1, Hours = 0m },
            new TrainingDetail { EmployeeId = "E003", LicenseType = "2.1", TrainingDate = DateTime.Today.AddMonths(-6), TrainingType = 1, Hours = 0m }
        );
        await db.SaveChangesAsync();
    }
}
