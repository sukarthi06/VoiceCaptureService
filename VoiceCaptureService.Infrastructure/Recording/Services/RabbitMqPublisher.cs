using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text.Json;
using VoiceCaptureService.Infrastructure.Recording.Interfaces;

namespace VoiceCaptureService.Infrastructure.Recording.Services;

public class RabbitMqPublisher(
        IConnection connection,
        ILogger<RabbitMqPublisher> logger) : IMessagePublisher, IAsyncDisposable
{
    private const string QueueName = "recording.completed";

    public static async Task<RabbitMqPublisher> CreateAsync(
        IConfiguration config,
        ILogger<RabbitMqPublisher> logger)
    {
        var host = config["RabbitMQ:Host"] ?? throw new InvalidOperationException("Configuration value 'RabbitMQ:Host' is missing.");
        var port = int.Parse(config["RabbitMQ:Port"] ?? "5672");

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = config["RabbitMQ:Username"] ?? throw new InvalidOperationException("Configuration value 'RabbitMQ:Username' is missing."),
                Password = config["RabbitMQ:Password"] ?? throw new InvalidOperationException("Configuration value 'RabbitMQ:Password' is missing.")
            };

            var connection = await factory.CreateConnectionAsync();

            using (var setupChannel = await connection.CreateChannelAsync())
            {
                await setupChannel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);
            }

            logger.LogInformation(
                "Connected to RabbitMQ at {Host}:{Port}, queue '{Queue}' ready",
                host, port, QueueName);

            return new RabbitMqPublisher(connection, logger);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Failed to connect to RabbitMQ at {Host}:{Port}. " +
                "Ensure RabbitMQ is running and configuration is correct",
                host, port);
            throw;
        }
    }

    public async Task PublishAsync<T>(T message, CancellationToken ct)
    {
        var messageType = typeof(T).Name;

        try
        {
            logger.LogDebug("Publishing {MessageType} to queue '{Queue}'...",
                messageType, QueueName);

            using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var props = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: QueueName,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            logger.LogInformation(
                "Published {MessageType} to queue '{Queue}' successfully. Payload: {@Message}",
                messageType, QueueName, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish {MessageType} to queue '{Queue}'",
                messageType, QueueName);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        logger.LogInformation("Closing RabbitMQ connection...");
        await connection.CloseAsync();
        logger.LogInformation("RabbitMQ connection closed");
    }
}