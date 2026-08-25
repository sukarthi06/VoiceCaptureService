using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System.Text.Json;
using VoiceCaptureService.Application.Recording.Interfaces;
using VoiceCaptureService.Domain;
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
    RecordingSession recordingSession { get; set; } = new();    
    // Field — lives for the lifetime of the orchestrator instance
    private RecyclableMemoryStream _staging = streamManager.GetStream("pcm-staging");
    private const int StagingThreshold = 4 * 1024 * 1024;  // 4 MB
    private string _chunkKey = string.Empty;
    private int _chunkCount = 0;
    private ChunkId? _chunkId;

    private List<RecordingChunk> _chunks = [];

    public async Task<RecordingId> StartRecordingAsync(CancellationToken cancellationToken)
    {       
        
        var captureKey = $"captures/{DateTime.UtcNow:yyyy-MM-dd}/{recordingSession.RecordingId.Value}.raw";
        
        recordingSession = new RecordingSession
        {
            RecordingId = RecordingId.Of(Guid.NewGuid()),
            StartedAt = DateTime.UtcNow,
            Status = RecordingStatus.Started,
            StoragePath = captureKey
        };
        logger.LogInformation("Started new Recording Session: {RecordingSession}",
            JsonSerializer.Serialize(recordingSession));

        await recordingUploader.InitiateAsync(captureKey, cancellationToken);
        await grpcRecordingSessionClient.SaveRecordingSessionAsync(recordingSession, cancellationToken);

        return recordingSession.RecordingId;
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
    }

    public async Task StageChunkAsync(CancellationToken cancellationToken)
    {
        if (_staging.Length >= StagingThreshold)
        {
            var chunkToProcess = _staging;
            _staging = streamManager.GetStream();
            chunkToProcess.Position = 0;

            _chunkId = ChunkId.Of(Guid.NewGuid());
            _chunkKey = $"captures/{DateTime.UtcNow:yyyy-MM-dd}/{_chunkId.Value}.raw";
            _chunkCount++;

            await recordingUploader.CommitChunkAsync(chunkToProcess, _chunkKey, cancellationToken);            
            var recordingChunk = GetRecordingChunk(chunkToProcess);
            await grpcRecordingChunkClient.SaveRecordingChunkAsync(recordingChunk, cancellationToken);
            
            await recordingUploader.UploadPartAsync(chunkToProcess, cancellationToken);
            await PublishChunkCompletedMessage(cancellationToken);
        }
    }

    public async Task StopRecordingAsync(RecordingId recordingId, CancellationToken cancellationToken) 
    {
        if (_staging.Length > 0)
        {
            _chunkId = ChunkId.Of(Guid.NewGuid());
            _chunkKey = $"captures/{DateTime.UtcNow:yyyy-MM-dd}/{_chunkId.Value}.raw";
            _chunkCount++;

            await recordingUploader.CommitChunkAsync(_staging, _chunkKey, cancellationToken);            
            var recordingChunk = GetRecordingChunk(_staging);
            await grpcRecordingChunkClient.SaveRecordingChunkAsync(recordingChunk, cancellationToken);

            await recordingUploader.UploadPartAsync(_staging, cancellationToken);
            await PublishChunkCompletedMessage(cancellationToken);
        }           

        await recordingUploader.FinalizeAsync(cancellationToken);

        await publisher.PublishAsync(MessageQueueNames.RecordingCompletedQueue,
            new RecordingCompletedMessage
            {
                RecordingId = recordingId.Value,
                CompletedAt = DateTime.UtcNow
            }, cancellationToken);

        recordingSession?.Status = RecordingStatus.Completed;
        recordingSession?.StoppedAt = DateTime.UtcNow;

        await grpcRecordingSessionClient.UpdateRecordingSessionAsync(recordingSession!, cancellationToken);

        //logger.LogInformation("Stopping recording for ID: {RecordingId}", recordingId);
        logger.LogInformation("Recording session stopped: {RecordingSession}",
            JsonSerializer.Serialize(recordingSession));
    }

    public async Task UpdateMetadataAsync(RecordingId recordingId, RecordingMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        recordingSession.RecordingMetadata = metadata;
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

    private RecordingChunk GetRecordingChunk(RecyclableMemoryStream chunk)
    {
        double durationSeconds = 0;
        if (recordingSession.RecordingMetadata is not null)
        {
            long bytesPerSample = recordingSession.RecordingMetadata.BitsPerSample / 8;
            long blockAlign = bytesPerSample * recordingSession.RecordingMetadata.ChannelCount;
            long bytesPerSecond = recordingSession.RecordingMetadata.SampleRate * blockAlign;

            durationSeconds = (double)chunk.Length / bytesPerSecond;
        }

        var previousChunk = _chunks.FirstOrDefault(rc => rc.SequenceNumber == _chunkCount - 1);
        TimeSpan startTime = (previousChunk is not null) ? previousChunk.EndTime.Add(TimeSpan.FromSeconds(1)) : TimeSpan.Zero;

        var recordingChunk = new RecordingChunk
        {
            ChunkId = _chunkId!,
            RecordingId = recordingSession.RecordingId,
            SequenceNumber = _chunkCount,
            StoragePath = _chunkKey,
            StartTime = startTime,
            EndTime = startTime.Add(TimeSpan.FromSeconds(durationSeconds)),
            ChunkDuration = durationSeconds
        };

        _chunks.Add(recordingChunk);

        return recordingChunk;
    }

    private async Task PublishChunkCompletedMessage(CancellationToken cancellationToken)
    {
        await publisher.PublishAsync(MessageQueueNames.ChunkCompletedQueue,
            new ChunkCompletedMessage(
                    ChunkId: _chunkId!.Value,
                    RecordingId: recordingSession.RecordingId.Value,
                    CompletedAt: DateTime.UtcNow
                ), cancellationToken);
    }
}
