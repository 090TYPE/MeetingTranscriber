using System.IO;
using System.Text.Json;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.Core;

public class SettingsService(string? overrideDir = null)
{
    private string Dir => overrideDir
        ?? Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "MeetingTranscriber");

    private string FilePath => Path.Combine(Dir, "settings.json");

    public AppSettings Load()
    {
        if (!File.Exists(FilePath)) return new AppSettings();
        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
