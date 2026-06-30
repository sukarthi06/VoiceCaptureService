namespace VoiceCaptureService.Infrastructure.Recording.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct);
}
