using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class ClubConfiguration : IEntityTypeConfiguration<Club>
{
    public void Configure(EntityTypeBuilder<Club> builder)
    {
        builder.ToTable("Clubs", t =>
            t.HasCheckConstraint("CHK_Clubs_Province", $"Province IN ({Province.SqlInList})"));
        builder.HasKey(c => c.ClubId).HasName("PK_Clubs");
        builder.Property(c => c.ClubId).UseIdentityColumn();
        builder.Property(c => c.ClubName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Province).HasMaxLength(20).IsRequired();
        builder.Property(c => c.AffiliationNumber).HasMaxLength(30);
        builder.Property(c => c.ContactEmail).HasMaxLength(255);
        builder.HasIndex(c => c.ClubName).IsUnique().HasDatabaseName("UQ_Clubs_ClubName");
    }
}
