namespace VoiceCaptureService.Domain.Recording.ValueObjects;

public record RecordingCompletedMessage
{
    public Guid RecordingId { get; init; }
    public DateTime CompletedAt { get; init; }
}
