using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class LicensePlantRequirementConfiguration : IEntityTypeConfiguration<LicensePlantRequirement>
{
    public void Configure(EntityTypeBuilder<LicensePlantRequirement> builder)
    {
        builder.ToTable("TRNM02");
        builder.HasKey(e => new { e.LicenseType, e.Plant });
        builder.Property(e => e.LicenseType).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Plant).HasMaxLength(10).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.RequiredCount);
        builder.Property(e => e.Creator).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.CreateDate).HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Modifier).HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.ModiDate).HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Flag).HasColumnType("decimal(1,0)");
        builder.Property(e => e.Company).HasColumnName("COMPANY").HasMaxLength(10).IsUnicode(false);
        builder.Property(e => e.UsrGroup).HasColumnName("USR_GROUP").HasMaxLength(10).IsUnicode(false);
    }
}
