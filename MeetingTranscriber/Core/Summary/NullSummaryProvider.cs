using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MeetingTranscriber.Core.Summary;

public class NullSummaryProvider : ISummaryProvider
{
    public bool IsConfigured => false;

    public async IAsyncEnumerable<string> SummarizeAsync(string transcript,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return "AI summary is disabled. Configure a provider in Settings.";
        await Task.CompletedTask;
    }
}
