namespace VoiceCaptureService.Api.Security;

public sealed class GatewayAuthOptions
{
    public const string SectionName = "GatewayAuth";

    public string Secret { get; set; } = string.Empty;
    public string NextSecret { get; set; } = string.Empty;
}