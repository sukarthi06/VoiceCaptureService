namespace VoiceCaptureService.Api.Configuration;

public class RecordingLimitOptions
{
    public const string SectionName = "Recording";

    public int MaxDurationMinutes { get; set; }
}