using MeetingTranscriber.Core.Transcription;
using MeetingTranscriber.Models;
using Xunit;

namespace MeetingTranscriber.Tests.Transcription;

public class WhisperServiceTests
{
    [Fact]
    public void Constructor_DoesNotThrow_WhenModelPathAbsent()
    {
        var ex = Record.Exception(() => new WhisperService("nonexistent.gguf", "auto"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task ProcessChunkAsync_ThrowsException_WhenModelMissing()
    {
        var svc = new WhisperService("nonexistent.gguf", "auto");
        var chunk = new AudioChunk(new byte[16000 * 2], 0);
        await Assert.ThrowsAnyAsync<Exception>(() => svc.ProcessChunkAsync(chunk));
    }
}
