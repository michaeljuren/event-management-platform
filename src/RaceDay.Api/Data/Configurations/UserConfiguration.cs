using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t =>
            t.HasCheckConstraint("CHK_Users_Gender", "Gender IN (N'Male', N'Female', N'Other')"));
        builder.HasKey(u => u.UserId).HasName("PK_Users");
        builder.Property(u => u.UserId).UseIdentityColumn();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.DateOfBirth).HasColumnType("date");
        builder.Property(u => u.Gender).HasMaxLength(10).IsRequired();
        builder.Property(u => u.EmergencyContactName).HasMaxLength(100);
        builder.Property(u => u.EmergencyContactPhone).HasMaxLength(20);
        builder.Property(u => u.CreatedAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_Users_CreatedAt");
        // ValueGeneratedNever so an explicit IsActive = false is always written rather than
        // being swallowed by the database default.
        builder.Property(u => u.IsActive).HasDefaultValue(true, "DF_Users_IsActive").ValueGeneratedNever();

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
        builder.HasIndex(u => u.ClubId).HasDatabaseName("IX_Users_ClubId");

        builder.HasOne(u => u.Club)
            .WithMany(c => c.Members)
            .HasForeignKey(u => u.ClubId)
            .HasConstraintName("FK_Users_Clubs")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
