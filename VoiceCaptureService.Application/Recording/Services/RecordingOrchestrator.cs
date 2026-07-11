using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System.Text.Json;
using VoiceCaptureService.Application.Recording.Interfaces;
using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Domain.Recording.Enums;
using VoiceCaptureService.Domain.Recording.ValueObjects;
using VoiceCaptureService.Infrastructure.Data.Interfaces;
using VoiceCaptureService.Infrastructure.Recording.Interfaces;

namespace VoiceCaptureService.Application.Recording.Services;

public class RecordingOrchestrator(
    RecyclableMemoryStreamManager streamManager,
    IRecordingUploader recordingUploader,
    IMessagePublisher publisher,
    IGrpcRecordingSessionClient grpcRecordingSessionClient,
    IGrpcRecordingChunkClient grpcRecordingChunkClient,
    ILogger<RecordingOrchestrator> logger) : IRecordingOrchestrator, IAsyncDisposable
{
    RecordingSession RecordingSession { get; set; } = new();    
    // Field — lives for the lifetime of the orchestrator instance
    private readonly RecyclableMemoryStream _staging = streamManager.GetStream("pcm-staging");
    private const int StagingThreshold = 4 * 1024 * 1024;  // 4 MB
    private string _chunkKey = string.Empty;
    private int _chunkCount = 0;
    private ChunkId? _chunkId;

    public async Task<RecordingId> StartRecordingAsync(CancellationToken cancellationToken)
    {       
        
        var captureKey = $"captures/{DateTime.UtcNow:yyyy-MM-dd}/{RecordingSession.RecordingId}.raw";
        
        RecordingSession = new RecordingSession
        {
            RecordingId = RecordingId.Of(Guid.NewGuid()),
            StartedAt = DateTime.UtcNow,
            Status = RecordingStatus.Started,
            StoragePath = captureKey
        };
        logger.LogInformation("Started new Recording Session: {RecordingSession}",
            JsonSerializer.Serialize(RecordingSession));

        await recordingUploader.InitiateAsync(captureKey, cancellationToken);
        await grpcRecordingSessionClient.SaveRecordingSessionAsync(RecordingSession, cancellationToken);

        return RecordingSession.RecordingId;
    }
    public async Task AppendAudioChunkAsync(
        RecordingId recordingId, 
        ReadOnlyMemory<byte> pcmData,
        CancellationToken cancellationToken) 
    {
        //logger.LogInformation("Received audio chunk for recording ID: {RecordingId}, Chunk size: {ChunkSize} bytes",
        //    recordingId, pcmData.Length);
        // Appends to the same buffer every call — no overwrite, no reset
        await _staging.WriteAsync(pcmData, cancellationToken);

        if (_staging.Length >= StagingThreshold)
        {
            _chunkId = ChunkId.Of(Guid.NewGuid());
            _chunkKey = $"captures/{DateTime.UtcNow:yyyy-MM-dd}/{_chunkId}.raw";
            _chunkCount++;
            await recordingUploader.UploadPartAsync(_staging, cancellationToken);
            await recordingUploader.CommitChunkAsync(_staging, _chunkKey, cancellationToken);            
            await grpcRecordingChunkClient.SaveRecordingChunkAsync(GetRecordingChunk(), cancellationToken);
        }            
    }

    public async Task StopRecordingAsync(RecordingId recordingId, CancellationToken cancellationToken) 
    {
        if (_staging.Length > 0)
        {
            _chunkId = ChunkId.Of(Guid.NewGuid());
            _chunkKey = $"captures/{DateTime.UtcNow:yyyy-MM-dd}/{_chunkId}.raw";
            _chunkCount++;
            await recordingUploader.CommitChunkAsync(_staging, _chunkKey, cancellationToken);
            await recordingUploader.UploadPartAsync(_staging, cancellationToken);
            await grpcRecordingChunkClient.SaveRecordingChunkAsync(GetRecordingChunk(), cancellationToken);
        }           

        await recordingUploader.FinalizeAsync(cancellationToken);

        await publisher.PublishAsync(new RecordingCompletedMessage
        {
            RecordingId = recordingId.Value,
            CompletedAt = DateTime.UtcNow
        }, cancellationToken);

        RecordingSession?.Status = RecordingStatus.Completed;
        RecordingSession?.StoppedAt = DateTime.UtcNow;

        await grpcRecordingSessionClient.UpdateRecordingSessionAsync(RecordingSession!, cancellationToken);

        //logger.LogInformation("Stopping recording for ID: {RecordingId}", recordingId);
        logger.LogInformation("Recording session stopped: {RecordingSession}",
            JsonSerializer.Serialize(RecordingSession));
    }

    public async Task UpdateMetadataAsync(RecordingId recordingId, RecordingMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        RecordingSession.RecordingMetadata = metadata;
        await grpcRecordingSessionClient.UpdateRecordingMetadataAsync(recordingId, metadata, cancellationToken);

        //logger.LogInformation("Updated metadata for recording ID: {RecordingId}, Metadata: {Metadata}",
        //    recordingId, JsonSerializer.Serialize(metadata));
    }
    // Called when DI scope ends (i.e. WebSocket connection closes)
    public async ValueTask DisposeAsync()
    {
        await _staging.DisposeAsync();        
    }

    public async Task AbortSessionAsync(RecordingId recordingId)
    {
        await recordingUploader.AbortAsync();
        logger.LogWarning("Session aborted: {RecordingId}", recordingId);
    }

    public async Task FinalizeSessionAsync(RecordingId recordingId, CancellationToken cancellationToken)
    {
        if (_staging.Length > 0)
            await recordingUploader.UploadPartAsync(_staging, cancellationToken);

        await recordingUploader.FinalizeAsync(cancellationToken);
        //logger.LogInformation("Session finalized: {RecordingId}", recordingId);
    }

    private RecordingChunk GetRecordingChunk()
    {
        return new RecordingChunk
        {
            ChunkId = _chunkId!,
            RecordingId = RecordingSession.RecordingId,
            SequenceNumber = _chunkCount,
            StoragePath = _chunkKey
        };
    }
}
