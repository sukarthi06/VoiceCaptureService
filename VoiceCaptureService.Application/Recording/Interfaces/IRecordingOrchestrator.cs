using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Application.Recording.Interfaces;

public interface IRecordingOrchestrator
{
    Task<RecordingId> StartRecordingAsync(CancellationToken cancellationToken);
    Task AppendAudioChunkAsync(RecordingId recordingId, ReadOnlyMemory<byte> pcmData, CancellationToken cancellationToken);
    Task StopRecordingAsync(RecordingId recordingId, CancellationToken cancellationToken);
    void UpdateMetadata(RecordingId recordingId, RecordingMetadata metadata);
    Task FinalizeSessionAsync(RecordingId recordingId, CancellationToken cancellationToken);
    Task AbortSessionAsync(RecordingId recordingId);
}
