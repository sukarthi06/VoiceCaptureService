using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecordingGrpcService.Grpc.Protos;
using Serilog;
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

        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
            return RabbitMqPublisher.CreateAsync(configuration, logger).GetAwaiter().GetResult();
        });

        services.AddSingleton<RecordingMapper>();
        services.AddScoped<IGrpcRecordingSessionClient, GrpcRecordingSessionClient>();

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
