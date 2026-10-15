using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.RoleId).HasName("PK_Roles");
        builder.Property(r => r.RoleId).UseIdentityColumn();
        builder.Property(r => r.RoleName).HasMaxLength(20).IsRequired();
        builder.HasIndex(r => r.RoleName).IsUnique().HasDatabaseName("UQ_Roles_RoleName");
    }
}
