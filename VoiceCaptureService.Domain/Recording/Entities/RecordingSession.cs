using VoiceCaptureService.Domain.Recording.Enums;
using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Domain.Recording.Entities;

public class RecordingSession
{
    public RecordingId RecordingId { get; set; } = RecordingId.Of(Guid.NewGuid());
    public RecordingStatus Status { get; set; } = RecordingStatus.Started;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StoppedAt { get; set; }
    public RecordingMetadata? RecordingMetadata { get; set; }
}
