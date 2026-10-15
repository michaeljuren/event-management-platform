using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t =>
        {
            t.HasCheckConstraint("CHK_Categories_DistanceKm", "DistanceKm > 0");
            t.HasCheckConstraint("CHK_Categories_MinAge", "MinAge IS NULL OR MinAge >= 0");
            t.HasCheckConstraint("CHK_Categories_GenderRestriction", "GenderRestriction IN (N'Open', N'Male', N'Female')");
            t.HasCheckConstraint("CHK_Categories_EntryFee", "EntryFee >= 0");
            t.HasCheckConstraint("CHK_Categories_MaxParticipants", "MaxParticipants IS NULL OR MaxParticipants > 0");
        });
        builder.HasKey(c => c.CategoryId).HasName("PK_Categories");
        builder.Property(c => c.CategoryId).UseIdentityColumn();
        builder.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.DistanceKm).HasPrecision(6, 2);
        builder.Property(c => c.GenderRestriction).HasMaxLength(10).IsRequired()
            .HasDefaultValue(Gender.Open, "DF_Categories_GenderRestriction");
        builder.Property(c => c.EntryFee).HasPrecision(8, 2)
            .HasDefaultValue(0m, "DF_Categories_EntryFee");

        builder.HasIndex(c => new { c.EventId, c.CategoryName }).IsUnique().HasDatabaseName("UQ_Categories_Event_Name");
        builder.HasIndex(c => c.EventId).HasDatabaseName("IX_Categories_EventId");
        builder.HasIndex(c => c.RouteId).HasDatabaseName("IX_Categories_RouteId");

        builder.HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .HasConstraintName("FK_Categories_Events")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Route)
            .WithMany(r => r.Categories)
            .HasForeignKey(c => c.RouteId)
            .HasConstraintName("FK_Categories_Routes")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
