using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.Core.Transcription;

public class ChunkQueue
{
    private readonly Channel<AudioChunk> _channel =
        Channel.CreateUnbounded<AudioChunk>(new UnboundedChannelOptions { SingleReader = true });

    public async ValueTask WriteAsync(AudioChunk chunk, CancellationToken ct = default) =>
        await _channel.Writer.WriteAsync(chunk, ct);

    public void Complete() => _channel.Writer.Complete();

    public IAsyncEnumerable<AudioChunk> ReadAllAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAllAsync(ct);
}
