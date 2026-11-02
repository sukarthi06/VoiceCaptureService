namespace VoiceCaptureService.Domain
{
    internal class Common
    {
    }

    public static class MessageQueueNames
    {   
        public const string ChunkCompletedQueue = "recordingchunk.completed";
    }
    public static class MessageExchangeNames
    {
        public const string RecordingCompletedExchange = "recording.completed";
    }
}
