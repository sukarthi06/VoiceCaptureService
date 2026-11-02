namespace VoiceCaptureService.Infrastructure.Recording.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string queueName, T message, CancellationToken ct);
    Task PublishFanoutAsync<T>(string exchangeName, T message, CancellationToken ct);
}
