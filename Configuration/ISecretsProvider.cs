namespace BluetoothMicrophoneApp.Configuration;

/// <summary>
/// Interface for accessing application secrets securely.
///
/// SECURITY PRINCIPLE: Never hardcode secrets in source code.
/// Secrets should be loaded from secure configuration (environment variables,
/// user secrets, Key Vault, etc.) based on the current environment.
///
/// BENEFITS:
/// - Secrets never in source control
/// - Environment-specific configuration
/// - Easy key rotation
/// - Centralized secrets management
/// </summary>
public interface ISecretsProvider
{
    /// <summary>
    /// Get Google OAuth Client ID for the current environment.
    /// </summary>
    string GetGoogleOAuthClientId();

    /// <summary>
    /// Get Google OAuth Client Secret for the current environment.
    /// WARNING: Keep this secret! Never log or expose to client.
    /// </summary>
    string GetGoogleOAuthClientSecret();

    /// <summary>
    /// Get Google OAuth Redirect URI for the current environment.
    /// </summary>
    string GetGoogleOAuthRedirectUri();

    /// <summary>
    /// Get API key for premium voice pack service.
    /// </summary>
    string GetPremiumVoiceApiKey();

    /// <summary>
    /// Get analytics API key (if using external analytics).
    /// </summary>
    string? GetAnalyticsApiKey();

    /// <summary>
    /// Get current environment.
    /// </summary>
    AppEnvironment GetEnvironment();
}

/// <summary>
/// Application environment enum.
/// </summary>
public enum AppEnvironment
{
    Development,
    Staging,
    Production
}
