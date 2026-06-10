using System;
using System.IO;
using MeetingTranscriber.Core;
using MeetingTranscriber.Models;
using Xunit;

namespace MeetingTranscriber.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileAbsent()
    {
        var svc = new SettingsService(_dir);
        var s = svc.Load();
        Assert.Equal("medium", s.WhisperModel);
        Assert.Equal("auto", s.Language);
        Assert.Equal(15, s.ChunkSeconds);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var svc = new SettingsService(_dir);
        svc.Save(new AppSettings { WhisperModel = "small", ClaudeApiKey = "sk-test" });
        var loaded = svc.Load();
        Assert.Equal("small", loaded.WhisperModel);
        Assert.Equal("sk-test", loaded.ClaudeApiKey);
    }
}
