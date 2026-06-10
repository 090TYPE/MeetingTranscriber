using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MeetingTranscriber.Core.Storage;
using Xunit;

namespace MeetingTranscriber.Tests.Storage;

public class SessionRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private SessionRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new AppDbContext(opts);
        _repo = new SessionRepository(_db);
        await _repo.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateSession_ReturnsSessionWithId()
    {
        var s = await _repo.CreateSessionAsync("Test Meeting", "ru");
        Assert.True(s.Id > 0);
        Assert.Equal("Test Meeting", s.Title);
    }

    [Fact]
    public async Task AppendLine_SavesLine()
    {
        var s = await _repo.CreateSessionAsync("M", "auto");
        await _repo.AppendLineAsync(s.Id, 0, "Hello world");
        var lines = await _repo.GetLinesAsync(s.Id);
        Assert.Single(lines);
        Assert.Equal("Hello world", lines[0].Text);
    }

    [Fact]
    public async Task SearchSessions_FiltersByTitle()
    {
        await _repo.CreateSessionAsync("Alpha meeting", "en");
        await _repo.CreateSessionAsync("Beta standup", "en");
        var results = await _repo.SearchSessionsAsync("Alpha");
        Assert.Single(results);
    }

    [Fact]
    public async Task DeleteSession_RemovesSessionAndLines()
    {
        var s = await _repo.CreateSessionAsync("Del", "en");
        await _repo.AppendLineAsync(s.Id, 0, "text");
        await _repo.DeleteSessionAsync(s.Id);
        var sessions = await _repo.SearchSessionsAsync();
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task FinalizeSession_UpdatesStats()
    {
        var s = await _repo.CreateSessionAsync("Final", "ru");
        await _repo.FinalizeSessionAsync(s.Id, 300, 150);
        var sessions = await _repo.SearchSessionsAsync();
        Assert.Equal(300, sessions[0].DurationSec);
        Assert.Equal(150, sessions[0].WordCount);
    }
}
