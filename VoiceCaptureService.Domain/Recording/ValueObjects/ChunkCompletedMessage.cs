namespace VoiceCaptureService.Domain.Recording.ValueObjects;

public record ChunkCompletedMessage(
    Guid ChunkId,
    Guid RecordingId,
    DateTime CompletedAt);
