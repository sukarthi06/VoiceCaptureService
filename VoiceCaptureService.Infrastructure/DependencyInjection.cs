using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RecordingGrpcService.Grpc.Protos;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
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
        var otlpEndpoint = configuration["Otlp:Endpoint"] ?? "http://localhost:4317";
        var otlpProtocol = configuration["Otlp:Protocol"] ?? "grpc";
        var otlpHeaders = configuration["Otlp:Headers"];
        var serviceName = configuration["Serilog:Properties:Application"] ?? "VoiceCaptureService.Api";
        var serilogProtocol = otlpProtocol.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpProtocol.HttpProtobuf
            : OtlpProtocol.Grpc;

        hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint;
                    options.Protocol = serilogProtocol;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName
                    };
                    if (!string.IsNullOrEmpty(otlpHeaders))
                    {
                        options.Headers = otlpHeaders
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(kv => kv.Split('=', 2))
                            .ToDictionary(parts => parts[0].Trim(), parts => Uri.UnescapeDataString(parts[1].Trim()));
                    }
                });
        });
    }

    public static IServiceCollection AddObservability(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        var otlpEndpoint = configuration["Otlp:Endpoint"] ?? "http://localhost:4317";
        var otlpProtocol = configuration["Otlp:Protocol"] ?? "grpc";
        var otlpHeaders = configuration["Otlp:Headers"];
        var serviceName = configuration["Serilog:Properties:Application"] ?? "VoiceCaptureService.Api";
        var exportProtocol = otlpProtocol.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;

        void ConfigureExporter(OtlpExporterOptions otlp)
        {
            otlp.Endpoint = new Uri(otlpEndpoint);
            otlp.Protocol = exportProtocol;
            if (!string.IsNullOrEmpty(otlpHeaders))
                otlp.Headers = otlpHeaders;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = httpContext => httpContext.Request.Path != "/")
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(ConfigureExporter))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(ConfigureExporter));

        return services;
    }
}
