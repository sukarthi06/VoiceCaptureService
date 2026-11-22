using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace VoiceCaptureService.Api.Security;

public static class KeyVaultExtensions
{
    public static IHostApplicationBuilder AddGatewayKeyVault(this IHostApplicationBuilder builder)
    {
        var vaultUri = builder.Configuration["KeyVault:Uri"]
            ?? throw new InvalidOperationException("KeyVault:Uri is not configured.");

        builder.Configuration.AddAzureKeyVault(
            new Uri(vaultUri),
            new DefaultAzureCredential(),
            new AzureKeyVaultConfigurationOptions
            {
                Manager = new PrefixKeyVaultSecretManager("GatewayAuth--"),
                ReloadInterval = TimeSpan.FromMinutes(5)   // picks up secret rotation without restart
            });

        builder.Services.AddOptions<GatewayAuthOptions>()
            .Bind(builder.Configuration.GetSection(GatewayAuthOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "GatewayAuth:Secret is missing.")
            .ValidateOnStart();

        return builder;
    }

    // Load only the gateway secrets, not everything in the vault
    private sealed class PrefixKeyVaultSecretManager(string prefix) : KeyVaultSecretManager
    {
        public override bool Load(SecretProperties secret) =>
            secret.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}