namespace VoiceCaptureService.Domain.Recording.ValueObjects;

public record RecordingMetadata
(
    int SampleRate,
    int ChannelCount,// Number of audio channels (e.g., 1 for mono, 2 for stereo)
    int BitsPerSample,
    string MimeType
);
