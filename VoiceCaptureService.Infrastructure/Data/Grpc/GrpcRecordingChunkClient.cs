using Microsoft.Extensions.Logging;
using RecordingGrpcService.Grpc.Protos;
using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Infrastructure.Data.Interfaces;
using VoiceCaptureService.Infrastructure.Data.Mappers;

namespace VoiceCaptureService.Infrastructure.Data.Grpc;

public class GrpcRecordingChunkClient(
    RecordingChunkService.RecordingChunkServiceClient client,
    RecordingChunkMapper mapper,
    ILogger<GrpcRecordingChunkClient> logger) : IGrpcRecordingChunkClient
{
    public async Task SaveRecordingChunkAsync(RecordingChunk chunk, CancellationToken cancellationToken)
    {
        var request = mapper.ToDto(chunk);        
        await client.SaveRecordingChunkAsync(
            request: new RecordingChunkRequest { RecordingChunk = request },
            cancellationToken: cancellationToken);
        logger.LogInformation("Recording chunk saved via gRPC");
    }
}
