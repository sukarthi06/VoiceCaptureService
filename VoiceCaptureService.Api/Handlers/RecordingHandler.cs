using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using VoiceCaptureService.Application.Recording.Interfaces;
using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Api.Handlers;

public class RecordingHandler(IRecordingOrchestrator recordingOrchestrator, ILogger<RecordingHandler> logger)
{
    private const int BufferSize = 1024 * 8; // 8 KB buffer size
    public async Task HandleRecordingAsync(WebSocket webSocket,
        CancellationToken cancellationToken)
    {        
        var receivedBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        var offset = 0;
        var recordingId = RecordingId.Of(Guid.NewGuid()); // Initialize with a default value
        ValueWebSocketReceiveResult result;
        try
        {
            recordingId = await recordingOrchestrator.StartRecordingAsync(cancellationToken);

            while (webSocket.State == WebSocketState.Open)
            {
                do
                {
                    // Receive data from the WebSocket
                    result = await webSocket.ReceiveAsync(
                        receivedBuffer.AsMemory(offset, BufferSize - offset),
                        cancellationToken);

                    offset += result.Count;
                    
                    if (offset > BufferSize)// Check if the received message exceeds the buffer size
                    {
                        logger.LogWarning(
                            "Received message exceeds maximum supported size of {BufferSize} bytes.",
                            BufferSize);

                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.MessageTooBig,
                            "Message exceeds maximum supported size.",
                            cancellationToken);

                        return;
                    }

                } while (!result.EndOfMessage);                
                offset = 0; // Reset offset for the next message

                switch (result.MessageType)
                {                    
                    case WebSocketMessageType.Binary:
                        //logger.LogInformation("Received binary data of length: {Length}", result.Count);
                        await recordingOrchestrator.AppendAudioChunkAsync(recordingId, 
                            receivedBuffer.AsMemory(0, result.Count), cancellationToken);                        
                        break;
                    case WebSocketMessageType.Text:

                        var message = Encoding.UTF8.GetString(receivedBuffer, 0, result.Count);

                        if (string.IsNullOrWhiteSpace(message))
                        {
                            logger.LogWarning("Received empty text message for recording ID: {RecordingId}.", recordingId);
                            continue;
                        }

                        await ProcessTextMessageAsync(webSocket, message, recordingId, cancellationToken);
                        break;
                    case WebSocketMessageType.Close:

                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            cancellationToken);
                        logger.LogInformation("WebSocket connection closed by client for recording ID: {RecordingId}.", recordingId);

                        break;
                    default:
                        logger.LogWarning("Received unexpected message type: {MessageType} for recording ID: {RecordingId}",
                            result.MessageType, recordingId);
                        break;
                }
            }
            
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("WebSocket operation cancelled.");
        }
        catch (WebSocketException ex)
        {
            await recordingOrchestrator.AbortSessionAsync(recordingId);
            logger.LogWarning(ex, "WebSocket error.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(receivedBuffer);
            logger.LogInformation("WebSocket disconnected.");
        }
    }
    
    private async Task ProcessTextMessageAsync(
        WebSocket webSocket,
        string textMessage,
        RecordingId recordingId,
        CancellationToken cancellationToken)
    {        
        try
        {            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var recordingCommand = JsonSerializer.Deserialize<RecordingCommand>(textMessage, options);            
            if(recordingCommand?.Type?.ToLowerInvariant() == "stop")
            {                
                logger.LogInformation("Received stop command for recording ID: {RecordingId}.", recordingId);
                await recordingOrchestrator.StopRecordingAsync(recordingId, cancellationToken);

                await webSocket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Done",
                    cancellationToken);

                return;
            }

            var recordingMetadata = JsonSerializer.Deserialize<RecordingMetadata>(textMessage, options);
            if (recordingMetadata is null || recordingMetadata.SampleRate <= 0 || recordingMetadata.ChannelCount <= 0 ||
                    recordingMetadata.BitsPerSample <= 0 || string.IsNullOrWhiteSpace(recordingMetadata.MimeType))
            {
                logger.LogWarning("Invalid recording metadata received.");
            }
            else
            {
                recordingOrchestrator?.UpdateMetadata(recordingId, recordingMetadata);
            }
        }
        catch (Exception ex) {            
            logger.LogError(ex, "Error processing text message.");
            throw;
        }
    }
}
