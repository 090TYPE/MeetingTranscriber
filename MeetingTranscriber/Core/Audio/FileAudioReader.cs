using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.Core.Audio;

public static class FileAudioReader
{
    public static IEnumerable<AudioChunk> ReadChunks(string filePath, int chunkSeconds = 15)
    {
        using var reader = CreateReader(filePath);
        using var resampler = new MediaFoundationResampler(reader, new WaveFormat(16000, 16, 1));
        resampler.ResamplerQuality = 60;

        var bytesPerChunk = 16000 * 2 * chunkSeconds;
        var buffer = new byte[bytesPerChunk];
        int timestampSec = 0;
        int bytesRead;

        while ((bytesRead = resampler.Read(buffer, 0, bytesPerChunk)) > 0)
        {
            var data = buffer[..bytesRead];
            yield return new AudioChunk(data, timestampSec);
            // Increment by actual seconds of audio read (not always chunkSeconds for last chunk)
            timestampSec += bytesRead / (16000 * 2);
        }
    }

    private static WaveStream CreateReader(string path) =>
        Path.GetExtension(path).ToLower() switch
        {
            ".mp3" => new Mp3FileReader(path),
            ".wav" => new WaveFileReader(path),
            ".mp4" or ".m4a" => new MediaFoundationReader(path),
            _ => throw new NotSupportedException($"Format not supported: {Path.GetExtension(path)}")
        };
}
