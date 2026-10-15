using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(s => s.SessionId).HasName("PK_UserSessions");
        builder.Property(s => s.SessionId).ValueGeneratedNever();
        builder.Property(s => s.RefreshTokenHash).HasMaxLength(64).IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_UserSessions_CreatedAt");
        builder.Property(s => s.LastRefreshedAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_UserSessions_LastRefreshedAt");
        builder.Property(s => s.ExpiresAt).HasColumnType("datetime2");
        builder.Property(s => s.RevokedAt).HasColumnType("datetime2");

        builder.HasIndex(s => s.RefreshTokenHash).IsUnique().HasDatabaseName("UQ_UserSessions_RefreshTokenHash");
        builder.HasIndex(s => s.UserId).HasDatabaseName("IX_UserSessions_UserId");

        builder.HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .HasConstraintName("FK_UserSessions_Users")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
