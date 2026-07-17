namespace VoiceCaptureService.Infrastructure.Recording.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string queueName, T message, CancellationToken ct);
}
