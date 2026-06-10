using Microsoft.EntityFrameworkCore;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.Core.Storage;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<TranscriptLine> TranscriptLines => Set<TranscriptLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Session>().ToTable("Sessions");
        b.Entity<TranscriptLine>().ToTable("TranscriptLines")
            .HasOne(l => l.Session)
            .WithMany(s => s.Lines)
            .HasForeignKey(l => l.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
