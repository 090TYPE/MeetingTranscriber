using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.Core.Transcription;

public class WhisperService(string modelPath, string language) : IDisposable
{
    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;

    public static string ModelPath(string modelName) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MeetingTranscriber", "models", $"ggml-{modelName}.bin");

    public static async Task DownloadModelAsync(string modelName, IProgress<double>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be empty", nameof(modelName));

        var dest = ModelPath(modelName);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        if (File.Exists(dest)) return;

        // Map model names to GgmlType safely
        var type = modelName.ToLowerInvariant() switch
        {
            "tiny" => GgmlType.Tiny,
            "base" => GgmlType.Base,
            "small" => GgmlType.Small,
            "medium" => GgmlType.Medium,
            "large-v3" => GgmlType.LargeV3,
            _ => throw new ArgumentException($"Unknown model: {modelName}. Valid: tiny, base, small, medium, large-v3")
        };

        try
        {
            await using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(type);
            await using var fs = File.Create(dest);
            await modelStream.CopyToAsync(fs);
        }
        catch
        {
            if (File.Exists(dest)) File.Delete(dest);
            throw;
        }
    }

    private void EnsureLoaded()
    {
        if (_processor is not null) return;
        _factory = WhisperFactory.FromPath(modelPath);
        var builder = _factory.CreateBuilder().WithLanguage(language == "auto" ? "auto" : language);
        _processor = builder.Build();
    }

    public async Task<TranscriptLine> ProcessChunkAsync(AudioChunk chunk)
    {
        EnsureLoaded();
        var samples = new float[chunk.PcmData.Length / 2];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = BitConverter.ToInt16(chunk.PcmData, i * 2) / 32768f;

        var sb = new StringBuilder();
        await foreach (var seg in _processor!.ProcessAsync(samples))
            sb.Append(seg.Text);

        return new TranscriptLine
        {
            TimestampSec = chunk.TimestampSec,
            Text = sb.ToString().Trim()
        };
    }

    public void Dispose()
    {
        _processor?.Dispose();
        _factory?.Dispose();
    }
}
