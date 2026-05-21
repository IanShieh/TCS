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
        builder.Property(e => e.EmployeeId).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.LicenseType).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.RequiredHours);
        builder.Property(e => e.Remark).HasMaxLength(200);
        builder.Property(e => e.Plant).HasMaxLength(6).IsFixedLength(true).IsUnicode(false).IsRequired(false);
        builder.Property(e => e.Creator).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnType("decimal(1,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);

        // TrainingHeader → LicenseMaster: HasOne with nullable nav (no DB FK migration required)
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
