using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.Core.Export;

public static class TxtExporter
{
    public static async Task ExportAsync(IEnumerable<TranscriptLine> lines, string title, string filePath)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine(new string('─', 40));
        sb.AppendLine();
        foreach (var line in lines)
        {
            var ts = TimeSpan.FromSeconds(line.TimestampSec);
            sb.AppendLine($"[{ts:mm\\:ss}] {line.Text}");
        }
        await File.WriteAllTextAsync(filePath, sb.ToString(), System.Text.Encoding.UTF8);
    }
}
