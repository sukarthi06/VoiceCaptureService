using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System.Text.Json;
using VoiceCaptureService.Application.Recording.Interfaces;
using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Domain.Recording.Enums;
using VoiceCaptureService.Domain.Recording.ValueObjects;
using VoiceCaptureService.Infrastructure.Recording.Interfaces;

namespace VoiceCaptureService.Application.Recording.Services;

public class RecordingOrchestrator(
    RecyclableMemoryStreamManager streamManager,
    IRecordingUploader recordingUploader,
    ILogger<RecordingOrchestrator> logger) : IRecordingOrchestrator, IAsyncDisposable
{
    RecordingSession RecordingSession { get; set; } = new();
    // Field — lives for the lifetime of the orchestrator instance
    private readonly RecyclableMemoryStream _staging = streamManager.GetStream("pcm-staging");
    private const int StagingThreshold = 4 * 1024 * 1024;  // 4 MB

    public async Task<RecordingId> StartRecordingAsync(CancellationToken cancellationToken)
    {
        RecordingSession = new RecordingSession { 
            RecordingId = RecordingId.Of(Guid.NewGuid()), 
            StartedAt = DateTime.UtcNow, 
            Status = RecordingStatus.Started
        };
        logger.LogInformation("Started new Recording Session: {RecordingSession}", 
            JsonSerializer.Serialize(RecordingSession));
        
        var captureKey = $"captures/{DateTime.UtcNow:yyyy-MM-dd}/{RecordingSession.RecordingId}.raw";
        await recordingUploader.InitiateAsync(captureKey, cancellationToken);

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
            await recordingUploader.UploadPartAsync(_staging, cancellationToken);
    }

    public async Task StopRecordingAsync(RecordingId recordingId, CancellationToken cancellationToken) 
    {
        if (_staging.Length > 0)
            await recordingUploader.UploadPartAsync(_staging, cancellationToken);

        await recordingUploader.FinalizeAsync(cancellationToken);

        RecordingSession?.Status = RecordingStatus.Completed;
        RecordingSession?.StoppedAt = DateTime.UtcNow;

        logger.LogInformation("Stopping recording for ID: {RecordingId}", recordingId);
        logger.LogInformation("Recording session completed: {RecordingSession}",
            JsonSerializer.Serialize(RecordingSession));
    }

    public void UpdateMetadata(RecordingId recordingId, RecordingMetadata metadata)
    {
        RecordingSession.RecordingMetadata = metadata;
        logger.LogInformation("Updated metadata for recording ID: {RecordingId}, Metadata: {Metadata}",
            recordingId, JsonSerializer.Serialize(metadata));
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
        logger.LogInformation("Session finalized: {RecordingId}", recordingId);
    }
}
