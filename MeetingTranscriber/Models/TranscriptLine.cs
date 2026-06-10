namespace MeetingTranscriber.Models;

public class TranscriptLine
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public int TimestampSec { get; set; }
    public string Text { get; set; } = string.Empty;
    public string TimestampFormatted => $"[{TimestampSec / 60:D2}:{TimestampSec % 60:D2}]";
}
