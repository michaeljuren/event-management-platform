using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.ToTable("Entries", t =>
        {
            t.HasCheckConstraint("CHK_Entries_Status", "Status IN (N'Pending', N'Confirmed', N'Cancelled', N'Withdrawn')");
            t.HasCheckConstraint("CHK_Entries_AmountPaid", "AmountPaid >= 0");
            t.HasCheckConstraint("CHK_Entries_PaymentStatus", "PaymentStatus IN (N'Unpaid', N'Paid', N'Refunded')");
        });
        builder.HasKey(e => e.EntryId).HasName("PK_Entries");
        builder.Property(e => e.EntryId).UseIdentityColumn();
        builder.Property(e => e.BibNumber).HasMaxLength(10);
        builder.Property(e => e.EntryDate).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_Entries_EntryDate");
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired()
            .HasDefaultValue(EntryStatus.Pending, "DF_Entries_Status");
        builder.Property(e => e.AmountPaid).HasPrecision(8, 2)
            .HasDefaultValue(0m, "DF_Entries_AmountPaid");
        builder.Property(e => e.PaymentStatus).HasMaxLength(10).IsRequired()
            .HasDefaultValue(PaymentStatus.Unpaid, "DF_Entries_PaymentStatus");

        builder.HasIndex(e => new { e.CategoryId, e.ParticipantId }).IsUnique().HasDatabaseName("UQ_Entries_Category_Participant");
        builder.HasIndex(e => e.ParticipantId).HasDatabaseName("IX_Entries_ParticipantId");

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Entries)
            .HasForeignKey(e => e.CategoryId)
            .HasConstraintName("FK_Entries_Categories")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Participant)
            .WithMany(u => u.Entries)
            .HasForeignKey(e => e.ParticipantId)
            .HasConstraintName("FK_Entries_Users_Participant")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
