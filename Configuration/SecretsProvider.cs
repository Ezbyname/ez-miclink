using System;

namespace BluetoothMicrophoneApp.Configuration;

/// <summary>
/// Default implementation of ISecretsProvider.
///
/// ARCHITECTURE: Loads secrets from environment-specific sources.
///
/// PRIORITY ORDER:
/// 1. Environment variables (highest priority - CI/CD, production)
/// 2. User secrets (development - dotnet user-secrets)
/// 3. appsettings.json (fallback - never commit real secrets here)
///
/// USAGE:
/// ```csharp
/// var provider = new SecretsProvider();
/// var clientId = provider.GetGoogleOAuthClientId();
/// ```
///
/// SECURITY: Never log secrets or expose them in error messages.
/// </summary>
public class SecretsProvider : ISecretsProvider
{
    private readonly AppEnvironment _environment;

    public SecretsProvider()
    {
        // Detect environment from build configuration
        _environment = DetectEnvironment();
    }

    public SecretsProvider(AppEnvironment environment)
    {
        _environment = environment;
    }

    public AppEnvironment GetEnvironment() => _environment;

    public string GetGoogleOAuthClientId()
    {
        // Try environment variable first (production/CI)
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID");
        if (!string.IsNullOrEmpty(clientId))
            return clientId;

        // Fall back to environment-specific defaults
        // NOTE: These should be overridden with dotnet user-secrets in development
        return _environment switch
        {
            AppEnvironment.Development => GetDevelopmentGoogleClientId(),
            AppEnvironment.Staging => GetStagingGoogleClientId(),
            AppEnvironment.Production => GetProductionGoogleClientId(),
            _ => throw new InvalidOperationException($"Unknown environment: {_environment}")
        };
    }

    public string GetGoogleOAuthClientSecret()
    {
        // Try environment variable first
        var secret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET");
        if (!string.IsNullOrEmpty(secret))
            return secret;

        // Fall back to environment-specific defaults
        return _environment switch
        {
            AppEnvironment.Development => GetDevelopmentGoogleClientSecret(),
            AppEnvironment.Staging => GetStagingGoogleClientSecret(),
            AppEnvironment.Production => GetProductionGoogleClientSecret(),
            _ => throw new InvalidOperationException($"Unknown environment: {_environment}")
        };
    }

    public string GetGoogleOAuthRedirectUri()
    {
        // Try environment variable first
        var redirectUri = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_REDIRECT_URI");
        if (!string.IsNullOrEmpty(redirectUri))
            return redirectUri;

        // Fall back to environment-specific defaults
        return _environment switch
        {
            AppEnvironment.Development => "com.googleusercontent.apps.849774261087-4dknu9oi63e3k58dpkrgnaekeaf5nj1g:/oauth2redirect",
            AppEnvironment.Staging => "com.googleusercontent.apps.staging:/oauth2redirect",
            AppEnvironment.Production => "com.googleusercontent.apps.849774261087-4dknu9oi63e3k58dpkrgnaekeaf5nj1g:/oauth2redirect",
            _ => throw new InvalidOperationException($"Unknown environment: {_environment}")
        };
    }

    public string GetPremiumVoiceApiKey()
    {
        // Try environment variable first
        var apiKey = Environment.GetEnvironmentVariable("PREMIUM_VOICE_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
            return apiKey;

        // TODO: Replace with actual API key from secure configuration
        return _environment switch
        {
            AppEnvironment.Development => "dev-premium-voice-key",
            AppEnvironment.Staging => "staging-premium-voice-key",
            AppEnvironment.Production => "PRODUCTION_KEY_FROM_SECURE_VAULT",
            _ => throw new InvalidOperationException($"Unknown environment: {_environment}")
        };
    }

    public string? GetAnalyticsApiKey()
    {
        // Try environment variable first
        var apiKey = Environment.GetEnvironmentVariable("ANALYTICS_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
            return apiKey;

        // Analytics is optional, so return null if not configured
        return null;
    }

    private static AppEnvironment DetectEnvironment()
    {
        // Check environment variable first
        var envVar = Environment.GetEnvironmentVariable("APP_ENVIRONMENT");
        if (!string.IsNullOrEmpty(envVar))
        {
            return envVar.ToLower() switch
            {
                "development" or "dev" => AppEnvironment.Development,
                "staging" or "stage" => AppEnvironment.Staging,
                "production" or "prod" => AppEnvironment.Production,
                _ => AppEnvironment.Development
            };
        }

        // Fall back to build configuration
#if DEBUG
        return AppEnvironment.Development;
#elif STAGING
        return AppEnvironment.Staging;
#else
        return AppEnvironment.Production;
#endif
    }

    #region Environment-Specific Secrets

    // IMPORTANT: These are fallback values for development.
    // Production secrets should ALWAYS come from environment variables or secure vault.
    // Use dotnet user-secrets to override these locally:
    //
    // dotnet user-secrets set "OAuth:Google:ClientId" "your-dev-client-id"
    // dotnet user-secrets set "OAuth:Google:ClientSecret" "your-dev-secret"

    private string GetDevelopmentGoogleClientId()
    {
        // TODO: Set up development OAuth credentials in Google Cloud Console
        // For now, use placeholder that will fail gracefully
        return "YOUR_DEV_CLIENT_ID_HERE";
    }

    private string GetDevelopmentGoogleClientSecret()
    {
        // TODO: Set up development OAuth credentials
        return "YOUR_DEV_CLIENT_SECRET_HERE";
    }

    private string GetStagingGoogleClientId()
    {
        // TODO: Set up staging OAuth credentials
        return "YOUR_STAGING_CLIENT_ID_HERE";
    }

    private string GetStagingGoogleClientSecret()
    {
        // TODO: Set up staging OAuth credentials
        return "YOUR_STAGING_CLIENT_SECRET_HERE";
    }

    private string GetProductionGoogleClientId()
    {
        // CRITICAL: This MUST come from environment variable in production
        // Never hardcode production secrets
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID");
        if (string.IsNullOrEmpty(clientId))
        {
            throw new InvalidOperationException(
                "Production Google OAuth Client ID not configured. " +
                "Set GOOGLE_OAUTH_CLIENT_ID environment variable.");
        }
        return clientId;
    }

    private string GetProductionGoogleClientSecret()
    {
        // CRITICAL: This MUST come from environment variable in production
        var secret = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET");
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException(
                "Production Google OAuth Client Secret not configured. " +
                "Set GOOGLE_OAUTH_CLIENT_SECRET environment variable.");
        }
        return secret;
    }

    #endregion
}
