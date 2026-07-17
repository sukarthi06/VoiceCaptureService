using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecordingGrpcService.Grpc.Protos;
using Serilog;
using VoiceCaptureService.Domain;
using VoiceCaptureService.Infrastructure.Data.Grpc;
using VoiceCaptureService.Infrastructure.Data.Interfaces;
using VoiceCaptureService.Infrastructure.Data.Mappers;
using VoiceCaptureService.Infrastructure.Recording.Interfaces;
using VoiceCaptureService.Infrastructure.Recording.Services;

namespace VoiceCaptureService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add infrastructure services here
        services.AddGrpcClient<RecordingService.RecordingServiceClient>(o =>
        {
            o.Address = new Uri(configuration["RecordingGrpcService:Address"]!);
        });

        services.AddGrpcClient<RecordingChunkService.RecordingChunkServiceClient>(o =>
        {
            o.Address = new Uri(configuration["RecordingGrpcService:Address"]!);
        });

        #region RabbitMQ

        services.AddSingleton<RabbitMqPublisher>(sp =>
            RabbitMqPublisher.CreateAsync(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<RabbitMqPublisher>>(),
                queueNames: [MessageQueueNames.RecordingCompletedQueue, MessageQueueNames.ChunkCompletedQueue]
            ).GetAwaiter().GetResult());
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<RabbitMqPublisher>());

        #endregion

        services.AddSingleton<RecordingMapper>();
        services.AddSingleton<RecordingChunkMapper>();

        services.AddScoped<IGrpcRecordingSessionClient, GrpcRecordingSessionClient>();
        services.AddScoped<IGrpcRecordingChunkClient, GrpcRecordingChunkClient>();


        return services;
    }

    public static void AddHostInfrastructure(
        this IHostBuilder hostBuilder,
        IConfiguration configuration)
    {
        hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });
    }
}
