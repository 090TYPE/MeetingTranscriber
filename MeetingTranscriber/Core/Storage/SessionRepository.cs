using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.Core.Storage;

public class SessionRepository(AppDbContext db)
{
    public async Task EnsureCreatedAsync() =>
        await db.Database.EnsureCreatedAsync();

    public async Task<Session> CreateSessionAsync(string title, string language)
    {
        var session = new Session { Title = title, Language = language };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task AppendLineAsync(int sessionId, int timestampSec, string text)
    {
        db.TranscriptLines.Add(new TranscriptLine
        {
            SessionId = sessionId,
            TimestampSec = timestampSec,
            Text = text
        });
        await db.SaveChangesAsync();
    }

    public async Task FinalizeSessionAsync(int sessionId, int durationSec, int wordCount)
    {
        var session = await db.Sessions.FindAsync(sessionId);
        if (session is null) return;
        session.DurationSec = durationSec;
        session.WordCount = wordCount;
        await db.SaveChangesAsync();
    }

    public async Task<List<Session>> SearchSessionsAsync(string query = "")
    {
        var q = db.Sessions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(s => s.Title.Contains(query));
        return await q.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public async Task<List<TranscriptLine>> GetLinesAsync(int sessionId) =>
        await db.TranscriptLines
            .Where(l => l.SessionId == sessionId)
            .OrderBy(l => l.TimestampSec)
            .ToListAsync();

    public async Task DeleteSessionAsync(int sessionId)
    {
        var session = await db.Sessions.FindAsync(sessionId);
        if (session is not null) db.Sessions.Remove(session);
        await db.SaveChangesAsync();
    }
}
