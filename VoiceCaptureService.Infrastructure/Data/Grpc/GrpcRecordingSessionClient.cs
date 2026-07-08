using Microsoft.Extensions.Logging;
using RecordingGrpcService.Grpc.Protos;
using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Domain.Recording.ValueObjects;
using VoiceCaptureService.Infrastructure.Data.Interfaces;
using VoiceCaptureService.Infrastructure.Data.Mappers;

namespace VoiceCaptureService.Infrastructure.Data.Grpc;

public class GrpcRecordingSessionClient(
    RecordingService.RecordingServiceClient client,
    RecordingMapper mapper,
    ILogger<GrpcRecordingSessionClient> logger) : IGrpcRecordingSessionClient
{
    public Task<RecordingSession?> GetRecordingSessionAsync(RecordingId recordingId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching recording session for RecordingId: {RecordingId}", recordingId.Value);
        throw new NotImplementedException();
    }

    public async Task<RecordingSession> SaveRecordingSessionAsync(RecordingSession recordingSession,
        CancellationToken cancellationToken = default)
    {
        var response = await client.SaveRecordingSessionAsync(
            new RecordingSessionRequest { RecordingSession = mapper.ToDto(recordingSession) },
            cancellationToken: cancellationToken);
        return mapper.ToDomain(response.RecordingSession);
    }

    public async Task<bool> UpdateRecordingMetadataAsync(RecordingId recordingId, RecordingMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        var response = await client.UpdateRecordingMetadataAsync(
            new UpdateRecordingMetadataRequest
            {
                RecordingId = recordingId.Value.ToString(),
                Metadata = mapper.ToDto(metadata)
            }, cancellationToken: cancellationToken);
        return response.IsSuccess;
    }

    public async Task<bool> UpdateRecordingSessionAsync(RecordingSession recordingSession,
        CancellationToken cancellationToken = default)
    {
        var response = await client.UpdateRecordingSessionAsync(
            new UpdateRecordingSessionRequest { RecordingSession = mapper.ToDto(recordingSession) },
            cancellationToken: cancellationToken);
        return response.IsSuccess;
    }

    public async Task<bool> UpdateStoragePathAsync(RecordingId recordingId, string storagePath,
        CancellationToken cancellationToken = default)
    {
        var response = await client.UpdateStoragePathAsync(
            new UpdateStoragePathRequest
            {
                RecordingId = recordingId.Value.ToString(),
                StoragePath = storagePath
            }, cancellationToken: cancellationToken);
        return response.IsSuccess;
    }
}
