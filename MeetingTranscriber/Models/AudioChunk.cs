namespace MeetingTranscriber.Models;

public record AudioChunk(
    byte[] PcmData,    // 16kHz, 16-bit, mono
    int TimestampSec,  // offset from start of session
    int SampleRate = 16000
);
