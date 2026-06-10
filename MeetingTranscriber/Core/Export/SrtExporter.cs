using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.Core.Export;

public static class SrtExporter
{
    private static string ToSrtTime(int sec) =>
        $"{sec / 3600:D2}:{(sec % 3600) / 60:D2}:{sec % 60:D2},000";

    public static async Task ExportAsync(IEnumerable<TranscriptLine> lines, string filePath)
    {
        var sb = new System.Text.StringBuilder();
        var list = lines.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            var start = list[i].TimestampSec;
            var end = i + 1 < list.Count ? list[i + 1].TimestampSec : start + 15;
            sb.AppendLine($"{i + 1}");
            sb.AppendLine($"{ToSrtTime(start)} --> {ToSrtTime(end)}");
            sb.AppendLine(list[i].Text);
            sb.AppendLine();
        }
        await File.WriteAllTextAsync(filePath, sb.ToString(), System.Text.Encoding.UTF8);
    }
}
