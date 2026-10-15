using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", t =>
        {
            t.HasCheckConstraint("CHK_Events_Discipline", "Discipline IN (N'Running', N'Walking', N'Cycling', N'Multisport')");
            t.HasCheckConstraint("CHK_Events_Status", "Status IN (N'Draft', N'Published', N'Cancelled', N'Completed')");
            t.HasCheckConstraint("CHK_Events_RegDates", "RegistrationOpenDate < RegistrationCloseDate");
        });
        builder.HasKey(e => e.EventId).HasName("PK_Events");
        builder.Property(e => e.EventId).UseIdentityColumn();
        builder.Property(e => e.EventName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasColumnType("nvarchar(max)");
        builder.Property(e => e.Discipline).HasMaxLength(20).IsRequired();
        builder.Property(e => e.EventDate).HasColumnType("date");
        builder.Property(e => e.StartTime).HasColumnType("time(0)");
        builder.Property(e => e.RegistrationOpenDate).HasColumnType("datetime2");
        builder.Property(e => e.RegistrationCloseDate).HasColumnType("datetime2");
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired()
            .HasDefaultValue(EventStatus.Draft, "DF_Events_Status");
        builder.Property(e => e.BannerImageUrl).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_Events_CreatedAt");
        builder.Property(e => e.UpdatedAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_Events_UpdatedAt");

        builder.HasIndex(e => e.OrganizerId).HasDatabaseName("IX_Events_OrganizerId");
        builder.HasIndex(e => e.VenueId).HasDatabaseName("IX_Events_VenueId");

        builder.HasOne(e => e.Organizer)
            .WithMany(u => u.OrganizedEvents)
            .HasForeignKey(e => e.OrganizerId)
            .HasConstraintName("FK_Events_Users_Organizer")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Venue)
            .WithMany(v => v.Events)
            .HasForeignKey(e => e.VenueId)
            .HasConstraintName("FK_Events_Venues")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
