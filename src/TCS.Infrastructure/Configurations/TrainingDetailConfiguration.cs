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
        builder.Property(e => e.EmployeeId).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.LicenseType).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.TrainingDate).HasColumnType("date");
        builder.Property(e => e.TrainingType);
        builder.Property(e => e.Hours).HasColumnType("decimal(6,1)");
        builder.Property(e => e.Creator).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnType("decimal(1,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);
    }
}
