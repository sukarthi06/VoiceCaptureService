using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Application.Recording.Interfaces;

public interface IRecordingOrchestrator
{
    Task<RecordingId> StartRecordingAsync(CancellationToken cancellationToken);
    Task AppendAudioChunkAsync(RecordingId recordingId, ReadOnlyMemory<byte> pcmData, CancellationToken cancellationToken);
    Task StageChunkAsync(CancellationToken cancellationToken);
    Task StopRecordingAsync(RecordingId recordingId, CancellationToken cancellationToken);
    Task UpdateMetadataAsync(RecordingId recordingId, RecordingMetadata metadata, CancellationToken cancellationToken = default);
    Task FinalizeSessionAsync(RecordingId recordingId, CancellationToken cancellationToken);
    Task AbortSessionAsync(RecordingId recordingId);
}
