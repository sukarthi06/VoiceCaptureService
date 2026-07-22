using RecordingGrpcService.Grpc.Protos;
using Riok.Mapperly.Abstractions;
using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Infrastructure.Data.Mappers;

[Mapper]
public partial class RecordingChunkMapper : MapperBase
{
    // ---- RecordingChunk ----
    [MapperIgnoreSource(nameof(RecordingChunkDto.WavPath))]
    [MapperIgnoreSource(nameof(RecordingChunkDto.TranscriptPath))]
    public partial RecordingChunk ToDomain(RecordingChunkDto dto);
    
    [MapperIgnoreTarget(nameof(RecordingChunkDto.WavPath))]
    [MapperIgnoreTarget(nameof(RecordingChunkDto.TranscriptPath))]
    public partial RecordingChunkDto ToDto(RecordingChunk entity);

    // ---- GetRecordingChunkResponse (repeated RecordingChunkDto) ----

    // ---- ChunkId (string <-> value object) ----
    public ChunkId MapChunkId(string id) => ChunkId.Of(ParseGuid(id));
    private string MapChunkId(ChunkId id) => id.Value.ToString();

    // ---- RecordingId (string <-> value object) ----
    public RecordingId MapRecordingId(string id) => RecordingId.Of(ParseGuid(id));
    private string MapRecordingId(RecordingId id) => id.Value.ToString();
}
