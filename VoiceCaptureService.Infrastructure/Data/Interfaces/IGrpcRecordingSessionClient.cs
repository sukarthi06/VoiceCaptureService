using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Infrastructure.Data.Interfaces;

public interface IGrpcRecordingSessionClient
{
    Task<RecordingSession?> GetRecordingSessionAsync(RecordingId recordingId, CancellationToken cancellationToken = default);
    Task<RecordingSession> SaveRecordingSessionAsync(RecordingSession recordingSession, CancellationToken cancellationToken = default);
    Task<bool> UpdateRecordingSessionAsync(RecordingSession recordingSession, CancellationToken cancellationToken = default);
    Task<bool> UpdateStoragePathAsync(RecordingId recordingId, string storagePath, CancellationToken cancellationToken = default);
    Task<bool> UpdateRecordingMetadataAsync(RecordingId recordingId, RecordingMetadata metadata,
        CancellationToken cancellationToken = default);    
}
