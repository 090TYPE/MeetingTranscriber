using System.Collections.Generic;
using System.Threading;

namespace MeetingTranscriber.Core.Summary;

public interface ISummaryProvider
{
    IAsyncEnumerable<string> SummarizeAsync(string transcript, CancellationToken ct = default);
    bool IsConfigured { get; }
}
