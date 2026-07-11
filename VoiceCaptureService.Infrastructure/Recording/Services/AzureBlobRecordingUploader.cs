using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using VoiceCaptureService.Infrastructure.Recording.Interfaces;

namespace VoiceCaptureService.Infrastructure.Recording.Services;

/*
 Phase 1 — Stage blocks (chapters arrive in any order, not yet a blob)

  Your App                        Azure Blob Storage
     │                                   │
     │── StageBlockAsync(id:"AAA", 4MB) ─► [AAA] sitting in staging area
     │── StageBlockAsync(id:"BBB", 4MB) ─► [BBB] sitting in staging area
     │── StageBlockAsync(id:"CCC", 2MB) ─► [CCC] sitting in staging area
     │                                   │
     │                            NOT a blob yet
     │                            NOT accessible yet
     │                            NOT committed yet

Phase 2 — Commit (tell Azure the order)

     │── CommitBlockListAsync(["AAA","BBB","CCC"]) ──►  Azure assembles:
     │                                                  [AAA][BBB][CCC]
     │                                                  → now a real blob ✅

Phase 3 — Access

     │◄── blob is now readable as one continuous file ──────────────────
*/

public class AzureBlobRecordingUploader(
    BlobServiceClient blobServiceClient,
    ILogger<AzureBlobRecordingUploader> logger) : IRecordingUploader
{
    //private const int BlockSizeThreshold = 4 * 1024 * 1024;  // 4 MB

    private BlobContainerClient? _containerClient;
    private BlockBlobClient? _blobClient;
    private List<string> _blockIds = [];
    private string? _captureKey;

    public async Task InitiateAsync(string captureKey, CancellationToken ct)
    {
        _captureKey = captureKey;
        _blockIds = [];

        _containerClient = blobServiceClient.GetBlobContainerClient("audio-captures");
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
        _blobClient = _containerClient.GetBlockBlobClient(captureKey);

        if(_containerClient != null && _blobClient != null)
            logger.LogInformation("Blob upload initiated: {Key}", captureKey);
    }

    public async Task UploadPartAsync(RecyclableMemoryStream staging, CancellationToken ct)
    {
        staging.Seek(0, SeekOrigin.Begin);

        var blockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        await _blobClient!.StageBlockAsync(blockId, staging, cancellationToken: ct);
        _blockIds.Add(blockId);

        //logger.LogInformation(
        //    "Block staged: {Key}, block={BlockId}, total blocks={Count}",
        //    _captureKey, blockId, _blockIds.Count);

        // Reset staging buffer — orchestrator owns it but uploader resets it
        staging.SetLength(0);
        staging.Seek(0, SeekOrigin.Begin);
    }

    public async Task FinalizeAsync(CancellationToken ct)
    {
        await _blobClient!.CommitBlockListAsync(_blockIds, cancellationToken: ct);
        logger.LogInformation("Blob committed: {Key}, blocks={Count}", _captureKey, _blockIds.Count);
    }

    public async Task AbortAsync()
    {
        // Azure staged blocks expire automatically — but log for observability
        logger.LogWarning("Upload aborted: {Key}", _captureKey);
        await Task.CompletedTask;
    }

    public async Task CommitChunkAsync(RecyclableMemoryStream chunk, string chunkKey, CancellationToken ct)
    {        
        chunk.Seek(0, SeekOrigin.Begin);

        if(_containerClient == null) return;

        var blobClient = _containerClient.GetBlobClient(chunkKey);
        if(blobClient != null)
            await blobClient.UploadAsync(chunk, cancellationToken: ct);
    }
}
