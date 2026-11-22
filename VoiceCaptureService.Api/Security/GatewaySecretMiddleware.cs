using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace VoiceCaptureService.Api.Security;

public sealed class GatewaySecretMiddleware(
    RequestDelegate next,
    IOptionsMonitor<GatewayAuthOptions> options,
    ILogger<GatewaySecretMiddleware> logger)
{
    private const string HeaderName = "X-Gateway-Secret";

    // Health probes, called directly by Aspire, App Service and the CD check
    private static readonly PathString[] ExemptPrefixes = ["/health", "/alive"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        // "/" must be an exact match: StartsWithSegments("/") would match every path
        var isRoot = !path.HasValue || path.Value == "/";

        if (isRoot || ExemptPrefixes.Any(p => path.StartsWithSegments(p)))
        {
            await next(context);
            return;
        }

        var provided = context.Request.Headers[HeaderName].ToString();
        var gatewayAuth = options.CurrentValue;

        if (!IsMatch(provided, gatewayAuth.Secret) && !IsMatch(provided, gatewayAuth.NextSecret))
        {
            logger.LogWarning("Rejected request to {Path}: missing or invalid gateway secret.", path);
            await Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized")
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    }

    // Hash first so the comparison is fixed-length and constant-time
    private static bool IsMatch(string provided, string expected)
    {
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
    }
}