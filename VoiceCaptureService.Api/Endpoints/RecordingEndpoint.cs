using VoiceCaptureService.Api.Handlers;

namespace VoiceCaptureService.Api.Endpoints
{
    public static class RecordingEndpoint
    {
        public static void MapRecordingEndpoints(this WebApplication app)
        {
            app.Map("ws/recordings", async (HttpContext context, 
                                                RecordingHandler handler,
                                                CancellationToken cancellationToken) =>
            {
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(RecordingEndpoint));

                if (!context.WebSockets.IsWebSocketRequest)
                {
                    logger.LogWarning("Invalid WebSocket request");
                    context.Response.StatusCode = 400; // Bad Request
                    return;
                }
                else
                {
                    //logger.LogInformation("WebSocket request accepted");
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await handler.HandleRecordingAsync(webSocket, cancellationToken);
                }
            });
        }
    }
}
