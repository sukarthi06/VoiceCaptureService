using RecordingGrpcService.Grpc.Protos;
using Riok.Mapperly.Abstractions;
using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Domain.Recording.ValueObjects;
using DomainRecordingStatus = VoiceCaptureService.Domain.Recording.Enums.RecordingStatus;
using ProtoRecordingStatus = RecordingGrpcService.Grpc.Protos.RecordingStatus;

namespace VoiceCaptureService.Infrastructure.Data.Mappers;

[Mapper]
public partial class RecordingMapper : MapperBase
{
    // ---- RecordingSession ----    
    [MapProperty(nameof(RecordingSessionDto.Metadata), nameof(RecordingSession.RecordingMetadata))]
    [MapperIgnoreTarget(nameof(RecordingSession.CreatedAt))]
    [MapperIgnoreTarget(nameof(RecordingSession.CreatedBy))]
    [MapperIgnoreTarget(nameof(RecordingSession.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(RecordingSession.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(RecordingSession.RecordStatus))]
    [MapperIgnoreSource(nameof(RecordingSessionDto.WavPath))]
    [MapperIgnoreSource(nameof(RecordingSessionDto.TranscriptPath))]
    public partial RecordingSession ToDomain(RecordingSessionDto dto);

    [MapProperty(nameof(RecordingSession.RecordingMetadata), nameof(RecordingSessionDto.Metadata))]
    [MapperIgnoreSource(nameof(RecordingSession.CreatedAt))]
    [MapperIgnoreSource(nameof(RecordingSession.CreatedBy))]
    [MapperIgnoreSource(nameof(RecordingSession.LastModifiedAt))]
    [MapperIgnoreSource(nameof(RecordingSession.LastModifiedBy))]
    [MapperIgnoreSource(nameof(RecordingSession.RecordStatus))]
    [MapperIgnoreTarget(nameof(RecordingSessionDto.WavPath))]
    [MapperIgnoreTarget(nameof(RecordingSessionDto.TranscriptPath))]
    public partial RecordingSessionDto ToDto(RecordingSession entity);


    // ---- RecordingMetadata (matches by property name/type, no custom code needed) ----
    public partial RecordingMetadata ToDomain(RecordingMetadataDto dto);
    public partial RecordingMetadataDto ToDto(RecordingMetadata metadata);

    // ---- RecordingId (string <-> value object) ----
    public RecordingId MapRecordingId(string id) => RecordingId.Of(ParseGuid(id));
    private string MapRecordingId(RecordingId id) => id.Value.ToString();

    // ---- RecordingStatus (explicit — proto has an UNSPECIFIED value domain doesn't) ----
    private DomainRecordingStatus MapStatus(ProtoRecordingStatus status) => status switch
    {
        ProtoRecordingStatus.Started => DomainRecordingStatus.Started,
        ProtoRecordingStatus.Recording => DomainRecordingStatus.Recording,
        ProtoRecordingStatus.Completed => DomainRecordingStatus.Completed,
        ProtoRecordingStatus.Failed => DomainRecordingStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown or unspecified recording status")
    };

    private ProtoRecordingStatus MapStatus(DomainRecordingStatus status) => status switch
    {
        DomainRecordingStatus.Started => ProtoRecordingStatus.Started,
        DomainRecordingStatus.Recording => ProtoRecordingStatus.Recording,
        DomainRecordingStatus.Completed => ProtoRecordingStatus.Completed,
        DomainRecordingStatus.Failed => ProtoRecordingStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown recording status")
    };
}

