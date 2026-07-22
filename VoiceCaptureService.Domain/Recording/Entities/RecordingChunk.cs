using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Domain.Recording.Entities;

public class RecordingChunk
{
    public ChunkId ChunkId { get; set; } = default!;
    public RecordingId RecordingId { get; set; } = default!;
    public int SequenceNumber { get; set; } = default!;
    public string StoragePath { get; set; } = default!;
    public TimeSpan StartTime { get; set; } = TimeSpan.Zero;
    public TimeSpan EndTime { get; set; } = TimeSpan.Zero;
    public double ChunkDuration { get; set; } = 0;
}
