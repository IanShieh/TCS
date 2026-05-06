using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        // 廠別是唯讀 view，不產生 migration DDL
        builder.ToView("CMSMB");
        builder.HasNoKey();
        builder.Property(e => e.PlantCode).HasColumnName("MB001").HasMaxLength(10).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.PlantName).HasColumnName("MB002").HasMaxLength(100); // §15 #6 長度未知，先用100
    }
}
