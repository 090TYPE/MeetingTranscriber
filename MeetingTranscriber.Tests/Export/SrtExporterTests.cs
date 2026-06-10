using MeetingTranscriber.Core.Export;
using MeetingTranscriber.Models;
using Xunit;

namespace MeetingTranscriber.Tests.Export;

public class SrtExporterTests
{
    [Fact]
    public async Task Export_ValidSrtFormat()
    {
        var lines = new List<TranscriptLine>
        {
            new() { TimestampSec = 0, Text = "Hello." },
            new() { TimestampSec = 15, Text = "World." }
        };
        var path = Path.GetTempFileName();
        try
        {
            await SrtExporter.ExportAsync(lines, path);
            var text = await File.ReadAllTextAsync(path);
            Assert.Contains("1", text);
            Assert.Contains("00:00:00,000 --> 00:00:15,000", text);
            Assert.Contains("Hello.", text);
            Assert.Contains("00:00:15,000 --> 00:00:30,000", text);
        }
        finally { File.Delete(path); }
    }
}
