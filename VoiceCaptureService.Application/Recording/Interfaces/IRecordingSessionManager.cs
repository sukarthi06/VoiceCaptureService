using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Application.Recording.Interfaces;

public interface IRecordingSessionManager
{
    public RecordingSession CreateSession();
    public RecordingSession?  GetSession(RecordingId recordingId);
    public bool ContainsSession(RecordingId recordingId);
    public void RemoveSession(RecordingId recordingId);
}
