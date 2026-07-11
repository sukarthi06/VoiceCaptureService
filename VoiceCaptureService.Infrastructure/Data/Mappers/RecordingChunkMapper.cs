using RecordingGrpcService.Grpc.Protos;
using Riok.Mapperly.Abstractions;
using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Infrastructure.Data.Mappers;

[Mapper]
public partial class RecordingChunkMapper : MapperBase
{
    // ---- RecordingChunk ----    
    public partial RecordingChunk ToDomain(RecordingChunkDto dto);    
    public partial RecordingChunkDto ToDto(RecordingChunk entity);

    // ---- GetRecordingChunkResponse (repeated RecordingChunkDto) ----
    public GetRecordingChunkResponse ToGetRecordingChunkResponse(IEnumerable<RecordingChunk> chunks)
    {
        var resp = new GetRecordingChunkResponse();
        if (chunks == null) return resp;
        foreach (var c in chunks)
        {
            resp.RecordingChunk.Add(ToDto(c));
        }
        return resp;
    }

    public IEnumerable<RecordingChunk> ToDomain(GetRecordingChunkResponse response)
    {
        if (response == null) return Enumerable.Empty<RecordingChunk>();
        return response.RecordingChunk.Select(ToDomain);
    }

    // ---- ChunkId (string <-> value object) ----
    public ChunkId MapChunkId(string id) => ChunkId.Of(ParseGuid(id));
    private string MapChunkId(ChunkId id) => id.Value.ToString();

    // ---- RecordingId (string <-> value object) ----
    public RecordingId MapRecordingId(string id) => RecordingId.Of(ParseGuid(id));
    private string MapRecordingId(RecordingId id) => id.Value.ToString();
}
