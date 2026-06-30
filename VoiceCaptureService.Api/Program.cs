using Azure.Storage.Blobs;
using Microsoft.IO;
using VoiceCaptureService.Api.Endpoints;
using VoiceCaptureService.Api.Handlers;
using VoiceCaptureService.Application.Recording.Interfaces;
using VoiceCaptureService.Application.Recording.Services;
using VoiceCaptureService.Infrastructure;
using VoiceCaptureService.Infrastructure.Recording.Interfaces;
using VoiceCaptureService.Infrastructure.Recording.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();



#region RecyclableMemoryStreamManager

builder.Services.AddSingleton<RecyclableMemoryStreamManager>(_ =>
    new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options
    {
        BlockSize = 128 * 1024,
        LargeBufferMultiple = 1024 * 1024,
        MaximumBufferSize = 4 * 1024 * 1024,
        AggressiveBufferReturn = true
    }));

#endregion

#region DI

builder.Services.AddSingleton<BlobServiceClient>(_ =>
    new BlobServiceClient(builder.Configuration["Azure:StorageConnectionString"]));

builder.Services.AddScoped<RecordingHandler>();
builder.Services.AddScoped<IRecordingOrchestrator, RecordingOrchestrator>();
builder.Services.AddScoped<IRecordingUploader, AzureBlobRecordingUploader>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

builder.Host.AddHostInfrastructure(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

#endregion

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapRecordingEndpoints();

app.Run();
