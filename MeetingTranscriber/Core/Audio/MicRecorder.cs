using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.Core.Audio;

public class MicRecorder : IDisposable
{
    private WaveInEvent? _capture;
    private readonly List<byte> _buffer = new();
    private int _elapsedSec;
    private readonly int _chunkSeconds;
    private readonly object _lock = new();

    public event Action<AudioChunk>? ChunkReady;
    public event Action<float[]>? WaveformSamples;

    public bool IsRecording { get; private set; }

    public MicRecorder(int chunkSeconds = 15) => _chunkSeconds = chunkSeconds;

    public void Start()
    {
        _capture = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 100
        };
        _capture.DataAvailable += OnDataAvailable;
        _buffer.Clear();
        _elapsedSec = 0;
        IsRecording = true;
        _capture.StartRecording();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var samples = new float[e.BytesRecorded / 2];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;
        WaveformSamples?.Invoke(samples);

        lock (_lock)
        {
            _buffer.AddRange(e.Buffer[..e.BytesRecorded]);
            var bytesPerChunk = 16000 * 2 * _chunkSeconds;
            while (_buffer.Count >= bytesPerChunk)
            {
                var data = _buffer.GetRange(0, bytesPerChunk).ToArray();
                _buffer.RemoveRange(0, bytesPerChunk);
                ChunkReady?.Invoke(new AudioChunk(data, _elapsedSec));
                _elapsedSec += _chunkSeconds;
            }
        }
    }

    public AudioChunk? StopAndFlush()
    {
        IsRecording = false;
        _capture?.StopRecording();
        _capture?.Dispose();
        _capture = null;
        lock (_lock)
        {
            if (_buffer.Count > 0)
            {
                var data = _buffer.ToArray();
                _buffer.Clear();
                return new AudioChunk(data, _elapsedSec);
            }
        }
        return null;
    }

    public void Dispose()
    {
        _capture?.StopRecording();
        _capture?.Dispose();
    }
}
