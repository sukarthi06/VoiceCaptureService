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
    public static async Task<RabbitMqPublisher> CreateAsync(
        IConfiguration config,
        ILogger<RabbitMqPublisher> logger,
        IEnumerable<string> queueNames)
    {
        var host = config["RabbitMQ:Host"] ?? throw new InvalidOperationException("Configuration value 'RabbitMQ:Host' is missing.");
        var port = int.Parse(config["RabbitMQ:Port"] ?? "5672");
        var queues = queueNames.ToArray();

        if (queues.Length == 0)
            throw new ArgumentException("At least one queue name must be provided.", nameof(queueNames));

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
                foreach (var queueName in queues)
                {
                    await setupChannel.QueueDeclareAsync(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false);
                }
            }

            logger.LogInformation(
                "Connected to RabbitMQ at {Host}:{Port}, queues [{Queues}] ready",
                host, port, string.Join(", ", queues));

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

    public async Task PublishAsync<T>(string queueName, T message, CancellationToken ct)
    {
        var messageType = typeof(T).Name;

        try
        {
            //logger.LogDebug("Publishing {MessageType} to queue '{Queue}'...",
            //    messageType, queueName);

            using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var props = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            logger.LogInformation(
                "Published {MessageType} to queue '{Queue}' successfully. Payload: {@Message}",
                messageType, queueName, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish {MessageType} to queue '{Queue}'",
                messageType, queueName);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        //logger.LogInformation("Closing RabbitMQ connection...");
        await connection.CloseAsync();
        logger.LogInformation("RabbitMQ connection closed");
    }
}