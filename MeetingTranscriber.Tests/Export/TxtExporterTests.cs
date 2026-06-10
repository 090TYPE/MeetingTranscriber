using MeetingTranscriber.Core.Export;
using MeetingTranscriber.Models;
using Xunit;

namespace MeetingTranscriber.Tests.Export;

public class TxtExporterTests
{
    private static List<TranscriptLine> SampleLines() =>
    [
        new() { TimestampSec = 0, Text = "Hello world." },
        new() { TimestampSec = 15, Text = "Second chunk." }
    ];

    [Fact]
    public async Task Export_WritesTimestampedLines()
    {
        var path = Path.GetTempFileName();
        try
        {
            await TxtExporter.ExportAsync(SampleLines(), "Test Meeting", path);
            var text = await File.ReadAllTextAsync(path);
            Assert.Contains("[00:00]", text);
            Assert.Contains("Hello world.", text);
            Assert.Contains("[00:15]", text);
        }
        finally { File.Delete(path); }
    }
}
