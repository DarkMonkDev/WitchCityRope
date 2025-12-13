using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.CheckIn.Entities.Configuration;

/// <summary>
/// Entity Framework Core configuration for CheckInSessionTokenSession join table
/// Defines many-to-many relationship between tokens and sessions
/// </summary>
public class CheckInSessionTokenSessionConfiguration : IEntityTypeConfiguration<CheckInSessionTokenSession>
{
    public void Configure(EntityTypeBuilder<CheckInSessionTokenSession> builder)
    {
        builder.ToTable("CheckInSessionTokenSessions", "public");

        // Composite primary key
        builder.HasKey(ts => new { ts.TokenId, ts.SessionId });

        // Relationship to Token
        builder.HasOne(ts => ts.Token)
            .WithMany(t => t.TokenSessions)
            .HasForeignKey(ts => ts.TokenId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship to Session
        builder.HasOne(ts => ts.Session)
            .WithMany()
            .HasForeignKey(ts => ts.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for efficient queries by session
        builder.HasIndex(ts => ts.SessionId)
            .HasDatabaseName("IX_CheckInSessionTokenSessions_SessionId");
    }
}
