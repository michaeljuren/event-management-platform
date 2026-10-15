using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.ToTable("Results", t =>
        {
            t.HasCheckConstraint("CHK_Results_OverallPosition", "OverallPosition IS NULL OR OverallPosition > 0");
            t.HasCheckConstraint("CHK_Results_CategoryPosition", "CategoryPosition IS NULL OR CategoryPosition > 0");
            t.HasCheckConstraint("CHK_Results_GenderPosition", "GenderPosition IS NULL OR GenderPosition > 0");
            t.HasCheckConstraint("CHK_Results_FinishStatus", "FinishStatus IN (N'Finished', N'DNF', N'DNS', N'DSQ')");
        });
        builder.HasKey(r => r.ResultId).HasName("PK_Results");
        builder.Property(r => r.ResultId).UseIdentityColumn();
        builder.Property(r => r.FinishTime).HasColumnType("time(3)");
        builder.Property(r => r.GunTime).HasColumnType("time(3)");
        builder.Property(r => r.ChipTime).HasColumnType("time(3)");
        builder.Property(r => r.FinishStatus).HasMaxLength(10).IsRequired()
            .HasDefaultValue(FinishStatus.Finished, "DF_Results_FinishStatus");
        builder.Property(r => r.RecordedAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_Results_RecordedAt");

        builder.HasIndex(r => r.EntryId).IsUnique().HasDatabaseName("UQ_Results_EntryId");
        builder.HasIndex(r => r.RecordedByUserId).HasDatabaseName("IX_Results_RecordedByUserId");

        builder.HasOne(r => r.Entry)
            .WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EntryId)
            .HasConstraintName("FK_Results_Entries")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RecordedBy)
            .WithMany(u => u.RecordedResults)
            .HasForeignKey(r => r.RecordedByUserId)
            .HasConstraintName("FK_Results_Users_RecordedBy")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
