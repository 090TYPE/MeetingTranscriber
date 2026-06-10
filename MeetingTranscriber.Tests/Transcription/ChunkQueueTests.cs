using System.Threading.Tasks;
using MeetingTranscriber.Core.Transcription;
using MeetingTranscriber.Models;
using Xunit;

namespace MeetingTranscriber.Tests.Transcription;

public class ChunkQueueTests
{
    [Fact]
    public async Task WriteAndRead_PreservesChunk()
    {
        var q = new ChunkQueue();
        var chunk = new AudioChunk(new byte[] { 1, 2, 3 }, 0);
        await q.WriteAsync(chunk);
        q.Complete();

        AudioChunk? result = null;
        await foreach (var item in q.ReadAllAsync()) { result = item; break; }

        Assert.NotNull(result);
        Assert.Equal(chunk.PcmData, result.PcmData);
    }

    [Fact]
    public async Task Complete_EndsEnumeration()
    {
        var q = new ChunkQueue();
        q.Complete();
        var count = 0;
        await foreach (var _ in q.ReadAllAsync()) count++;
        Assert.Equal(0, count);
    }
}
