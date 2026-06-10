namespace MeetingTranscriber.Models;

public class AppSettings
{
    public string WhisperModel { get; set; } = "tiny";
    public string Language { get; set; } = "auto";
    public int ChunkSeconds { get; set; } = 8;
    public string AiProvider { get; set; } = "disabled"; // "claude" | "openai" | "disabled"
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string OpenAiApiKey { get; set; } = string.Empty;

    public static readonly string[] WhisperModels = ["tiny", "base", "small", "medium", "large-v3"];
    public static readonly string[] Languages = ["auto", "ru", "en", "de", "fr", "es", "zh", "ja"];
}
