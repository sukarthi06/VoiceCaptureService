using Azure.Storage.Blobs;
using Microsoft.IO;
using VoiceCaptureService.Api;
using VoiceCaptureService.Api.Endpoints;
using VoiceCaptureService.Api.Handlers;
using VoiceCaptureService.Api.Security;
using VoiceCaptureService.Application.Recording.Interfaces;
using VoiceCaptureService.Application.Recording.Services;
using VoiceCaptureService.Infrastructure;
using VoiceCaptureService.Infrastructure.Recording.Interfaces;
using VoiceCaptureService.Infrastructure.Recording.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddGatewayKeyVault();
builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Description = """
            Real-time audio capture over a **WebSocket**. This endpoint has no REST operations, so none are listed here.

            ## Connect
            `wss://<gateway>/ws/recordings?access_token=<JWT>`

            - The gateway checks the JWT when the connection opens and returns **401** if it is missing, invalid or expired. It is not re-checked afterwards.
            - A plain HTTP request to this path (not a WebSocket upgrade) returns **400**.
            - A recording session starts as soon as the connection is accepted.

            ## Messages from the client
            | Frame | Content | Server behaviour |
            |---|---|---|
            | Text | `{"type":"start"}` | Replies with the recording ID as JSON |
            | Text | `{"type":"ping"}` | Ignored (keep-alive while paused) |
            | Text | Metadata JSON: `sampleRate`, `channelCount`, `bitsPerSample`, `mimeType` | Stored for the recording. Invalid metadata is logged and ignored |
            | Binary | One audio chunk | Appended to the recording. Each message must be 8 KB or smaller, otherwise the server closes with 1009 (message too big) |
            | Text | `{"type":"stop"}` | Finalises the recording, then closes with 1000 ("Done") |
            | Close frame | n/a | Server closes the connection |

            If the socket fails with an error, the recording session is aborted.
            """;
        return Task.CompletedTask;
    });
});

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

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<BlobServiceClient>(_ =>
    new BlobServiceClient(builder.Configuration["Azure:StorageConnectionString"]));

builder.Services.AddScoped<RecordingHandler>();
builder.Services.AddScoped<IRecordingOrchestrator, RecordingOrchestrator>();
builder.Services.AddScoped<IRecordingUploader, AzureBlobRecordingUploader>();

builder.Host.AddHostInfrastructure(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
if (!builder.Environment.IsEnvironment("Local"))
{
    builder.Services.AddObservability(builder.Configuration);
}

#endregion

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<GatewaySecretMiddleware>();

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapRecordingEndpoints();

app.Run();
