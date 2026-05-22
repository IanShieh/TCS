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
