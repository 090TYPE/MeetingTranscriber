using System;
using System.Collections.Generic;

namespace MeetingTranscriber.Models;

public class Session
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int DurationSec { get; set; }
    public string Language { get; set; } = "auto";
    public int WordCount { get; set; }
    public ICollection<TranscriptLine> Lines { get; set; } = new List<TranscriptLine>();
}
