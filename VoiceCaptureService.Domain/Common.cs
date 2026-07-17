namespace VoiceCaptureService.Domain
{
    internal class Common
    {
    }

    public static class MessageQueueNames
    {
        public const string RecordingCompletedQueue = "recording.completed";
        public const string ChunkCompletedQueue = "recordingchunk.completed";
    }
}
