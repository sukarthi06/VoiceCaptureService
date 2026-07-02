using VoiceCaptureService.Domain.Abstraction;
using VoiceCaptureService.Domain.Recording.Enums;
using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Domain.Recording.Entities;

public class RecordingSession : Entity
{
    public RecordingId RecordingId { get; set; } = RecordingId.Of(Guid.NewGuid());
    public RecordingStatus Status { get; set; } = RecordingStatus.Started;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StoppedAt { get; set; }
    public string? StoragePath { get; set; }
    public RecordingMetadata? RecordingMetadata { get; set; }
}
