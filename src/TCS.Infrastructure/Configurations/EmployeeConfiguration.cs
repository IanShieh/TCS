using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TCS.Core.Entities;

namespace TCS.Infrastructure.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        // 員工是唯讀 view，不產生 migration DDL
        builder.ToView("CMSMV");
        builder.HasNoKey();
        builder.Property(e => e.EmployeeId).HasColumnName("MV001").HasMaxLength(20).IsFixedLength(false).IsUnicode(false);
        builder.Property(e => e.Name).HasColumnName("MV002").HasMaxLength(100);
        builder.Property(e => e.Department).HasColumnName("MV004").HasMaxLength(50);
        builder.Property(e => e.HireDate).HasColumnName("MV021").HasMaxLength(8).IsFixedLength(true).IsUnicode(false);
        builder.Property(e => e.Plant).HasColumnName("MV005").HasMaxLength(10).IsFixedLength(true).IsUnicode(false);
    }
}
