using Microsoft.IO;

namespace VoiceCaptureService.Infrastructure.Recording.Interfaces;

public interface IRecordingUploader
{
    Task InitiateAsync(string captureKey, CancellationToken ct);
    Task UploadPartAsync(RecyclableMemoryStream staging, CancellationToken ct);
    Task FinalizeAsync(CancellationToken ct);
    Task AbortAsync();
}
