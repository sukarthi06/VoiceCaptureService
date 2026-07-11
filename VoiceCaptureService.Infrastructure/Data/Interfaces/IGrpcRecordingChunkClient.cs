using VoiceCaptureService.Domain.Recording.Entities;

namespace VoiceCaptureService.Infrastructure.Data.Interfaces;

public interface IGrpcRecordingChunkClient
{
    Task SaveRecordingChunkAsync(RecordingChunk chunk, CancellationToken cancellationToken);
}
