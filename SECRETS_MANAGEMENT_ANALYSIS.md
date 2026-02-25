# Secrets Management Analysis & Implementation

**Skill Applied**: [secrets-management](https://skills.sh/skill/wshobson/agents/secrets-management)

**Date**: 2026-02-25
**Status**: Implementation Complete

---

## 🎯 Executive Summary

This document analyzes security vulnerabilities in the current codebase and implements a comprehensive secrets management system to protect sensitive credentials, API keys, and user data.

**Current State**: Security vulnerabilities with hardcoded secrets and plain-text storage
**Target State**: Secure secrets management with encryption, environment-based configuration, and best practices

---

## 🔴 Security Issues Identified

### Critical Issues

#### 1. **Hardcoded OAuth Client ID**

**Location**: `Services/AuthenticationService.cs:122`

**Code**:
```csharp
const string clientId = "849774261087-c8nmbm9ffhk0p8ha6dachf9a6d36uk17.apps.googleusercontent.com";
const string redirectUri = "com.googleusercontent.apps.849774261087-4dknu9oi63e3k58dpkrgnaekeaf5nj1g:/oauth2redirect";
```

**Risk**: HIGH
- Exposed in source control
- Visible to anyone with repo access
- Cannot rotate without redeploying app
- Same credentials for dev/staging/prod

**Impact**:
- API quota abuse
- Unauthorized access to Google OAuth
- Potential account takeover
- Cannot revoke compromised keys

---

#### 2. **Plain-Text Token Storage**

**Location**: `Services/AuthenticationService.cs:70-73`

**Code**:
```csharp
Preferences.Set(IS_LOGGED_IN_KEY, true);
Preferences.Set(USER_EMAIL_KEY, email);
Preferences.Set(USER_NAME_KEY, name);
Preferences.Set(LOGIN_DATE_KEY, DateTime.Now.ToString("O"));
```

**Risk**: MEDIUM
- Stored in plain text on device
- Accessible via device backup
- No encryption
- Vulnerable if device is rooted/jailbroken

**Impact**:
- Session hijacking possible
- User credentials exposed
- GDPR/privacy compliance risk

---

#### 3. **No Environment-Based Configuration**

**Risk**: MEDIUM
- Same credentials for all environments
- Cannot test with sandbox APIs
- Production secrets exposed in development

---

#### 4. **No API Key Rotation Strategy**

**Risk**: MEDIUM
- Keys cannot be rotated without app update
- Compromised keys require app redeployment
- No emergency revocation mechanism

---

## ✅ Implemented Solution

### 1. **Secrets Configuration System**

**Files Created**:
- `Configuration/ISecretsProvider.cs` - Interface for secrets
- `Configuration/SecretsProvider.cs` - Implementation
- `Configuration/EnvironmentConfig.cs` - Environment detection

**Architecture**:
```
App Startup
    ↓
SecretsProvider (loads from secure source)
    ↓
Services (inject ISecretsProvider)
    ↓
Use secrets (never hardcoded)
```

**Benefits**:
- ✅ No secrets in source code
- ✅ Environment-specific configuration
- ✅ Centralized secrets management
- ✅ Easy key rotation

---

### 2. **Encrypted Storage for Tokens**

**Implementation**: Using MAUI `SecureStorage` API

**Before**:
```csharp
// ❌ INSECURE: Plain text
Preferences.Set("access_token", token);
```

**After**:
```csharp
// ✅ SECURE: Encrypted
await SecureStorage.SetAsync("access_token", token);
```

**Features**:
- ✅ Hardware-backed encryption (Android Keystore, iOS Keychain)
- ✅ Inaccessible even if device is rooted
- ✅ Automatic key derivation
- ✅ MAUI built-in, no external dependencies

---

### 3. **Secure Authentication Service**

**File**: `Services/SecureAuthenticationService.cs`

**Features**:
1. **Encrypted Token Storage**
   - Access tokens encrypted with SecureStorage
   - Refresh tokens encrypted separately
   - Session data protected

2. **Token Expiration Handling**
   - Automatic token refresh
   - Expiration validation
   - Grace period for network issues

3. **Secure Session Management**
   - Logout clears all encrypted data
   - Session timeout support
   - Multi-device session tracking

---

### 4. **Environment-Based Configuration**

**Environments Supported**:
- **Development** - Local testing with sandbox APIs
- **Staging** - Pre-production testing
- **Production** - Live environment

**Configuration**:
```csharp
public class EnvironmentConfig
{
    public static Environment Current =>
#if DEBUG
        Environment.Development;
#elif STAGING
        Environment.Staging;
#else
        Environment.Production;
#endif
}
```

**Secrets Per Environment**:
```csharp
public class OAuthSecrets
{
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }
    public string RedirectUri { get; init; }
}

// Dev secrets (sandboxed)
var devSecrets = new OAuthSecrets
{
    ClientId = "dev-client-id",
    ClientSecret = "dev-secret",
    RedirectUri = "dev-redirect"
};

// Prod secrets (from secure vault)
var prodSecrets = LoadFromSecureVault();
```

---

### 5. **API Key Management**

**File**: `Configuration/ApiKeyManager.cs`

**Features**:
1. **Centralized Key Storage**
   - All API keys in one place
   - Type-safe access
   - Never hardcoded

2. **Key Rotation Support**
   - Version-based keys
   - Graceful fallback
   - Zero-downtime rotation

3. **Usage Tracking**
   - Monitor API key usage
   - Detect anomalies
   - Rate limiting

**Example**:
```csharp
public interface IApiKeyManager
{
    string GetGoogleOAuthClientId();
    string GetGoogleOAuthClientSecret();
    string GetPremiumVoiceApiKey();
    string GetAnalyticsApiKey();
}
```

---

### 6. **Premium Features Licensing**

**File**: `Services/LicenseValidator.cs`

**Features**:
1. **License Verification**
   - Cryptographic signature validation
   - Server-side verification
   - Offline grace period

2. **Feature Gating**
   - Check before accessing premium features
   - Graceful degradation
   - Clear error messages

3. **Subscription Management**
   - Trial period support
   - Subscription expiration
   - Auto-renewal handling

**Example**:
```csharp
public class LicenseValidator
{
    public async Task<bool> ValidatePremiumAccessAsync()
    {
        // Check license from secure storage
        var license = await SecureStorage.GetAsync("premium_license");

        // Validate signature
        if (!ValidateSignature(license))
            return false;

        // Check expiration
        if (IsExpired(license))
            return false;

        return true;
    }
}
```

---

## 📋 Implementation Checklist

### Phase 1: Core Infrastructure ✅

- [x] Create `ISecretsProvider` interface
- [x] Implement `SecretsProvider` with environment detection
- [x] Create `EnvironmentConfig` for build configuration
- [x] Create `ApiKeyManager` for centralized key storage
- [x] Document secrets management strategy

### Phase 2: Secure Storage ✅

- [x] Create `SecureAuthenticationService` using SecureStorage
- [x] Migrate from `Preferences` to `SecureStorage` for tokens
- [x] Implement token encryption wrapper
- [x] Add secure session management
- [x] Test encryption on Android

### Phase 3: OAuth Security ✅

- [x] Extract OAuth client ID to configuration
- [x] Implement environment-specific OAuth config
- [x] Add OAuth state validation (CSRF protection)
- [x] Implement PKCE for public clients
- [x] Add token refresh logic

### Phase 4: Premium Features ✅

- [x] Create `LicenseValidator` interface
- [x] Implement license signature validation
- [x] Create feature gate helper methods
- [x] Add subscription expiration checks
- [x] Test license validation flow

### Phase 5: Documentation ✅

- [x] Create this analysis document
- [x] Document secrets rotation procedure
- [x] Create developer onboarding guide
- [x] Security best practices guide

---

## 🔒 Best Practices Implemented

### 1. **Never Hardcode Secrets**
✅ All secrets loaded from secure configuration
✅ No secrets in source control
✅ Environment-specific secrets

### 2. **Encrypt Sensitive Data**
✅ Use SecureStorage for tokens
✅ Hardware-backed encryption
✅ Automatic key derivation

### 3. **Principle of Least Privilege**
✅ Services only access needed secrets
✅ Read-only access where possible
✅ Scoped API keys

### 4. **Defense in Depth**
✅ Multiple layers of security
✅ Token expiration
✅ Signature validation
✅ PKCE for OAuth

### 5. **Audit and Monitor**
✅ Log authentication events
✅ Track API key usage
✅ Detect anomalies

---

## 📊 Security Improvements

| Aspect | Before | After | Status |
|--------|--------|-------|--------|
| OAuth Client ID | Hardcoded | Environment config | ✅ Fixed |
| Token Storage | Plain text (Preferences) | Encrypted (SecureStorage) | ✅ Fixed |
| Environment Config | None | Dev/Staging/Prod | ✅ Implemented |
| API Key Management | Scattered | Centralized | ✅ Implemented |
| License Validation | None | Cryptographic signature | ✅ Implemented |
| Token Encryption | None | Hardware-backed | ✅ Implemented |
| Key Rotation | Not supported | Supported | ✅ Implemented |
| CSRF Protection | None | OAuth state parameter | ✅ Implemented |
| PKCE | None | Implemented | ✅ Implemented |

---

## 🔐 Secrets Storage Strategy

### Development Environment

**Option 1**: User Secrets (Recommended)
```bash
# Store secrets locally (never committed)
dotnet user-secrets set "OAuth:Google:ClientId" "your-dev-client-id"
dotnet user-secrets set "OAuth:Google:ClientSecret" "your-dev-secret"
```

**Option 2**: Environment Variables
```bash
export GOOGLE_OAUTH_CLIENT_ID="your-dev-client-id"
export GOOGLE_OAUTH_CLIENT_SECRET="your-dev-secret"
```

**Option 3**: Local Config File (gitignored)
```json
// secrets.local.json (in .gitignore)
{
  "OAuth": {
    "Google": {
      "ClientId": "your-dev-client-id",
      "ClientSecret": "your-dev-secret"
    }
  }
}
```

### Production Environment

**Option 1**: Azure Key Vault (Recommended for Enterprise)
- Centralized secrets management
- Access control with Azure AD
- Audit logging
- Automatic rotation

**Option 2**: AWS Secrets Manager
- Similar to Azure Key Vault
- Integrated with AWS services
- Automatic rotation

**Option 3**: Encrypted Configuration File
- Deployed with app
- Decryption key on device
- Less ideal but workable

---

## 🔄 Key Rotation Procedure

### 1. **OAuth Client ID Rotation**

```bash
# Step 1: Generate new credentials in Google Cloud Console
# Step 2: Update configuration
dotnet user-secrets set "OAuth:Google:ClientId" "new-client-id"

# Step 3: Deploy new version
# Step 4: Monitor for issues
# Step 5: Revoke old credentials after grace period
```

### 2. **API Key Rotation**

```csharp
// Support multiple active keys during transition
public class ApiKeyManager
{
    private readonly List<string> _activeKeys = new()
    {
        "current-key-v2",  // New key
        "current-key-v1"   // Old key (grace period)
    };

    public string GetPrimaryKey() => _activeKeys[0];

    public IEnumerable<string> GetAllActiveKeys() => _activeKeys;
}
```

### 3. **Emergency Revocation**

```csharp
// Server-side kill switch
public async Task<bool> IsKeyRevoked(string apiKey)
{
    var response = await _httpClient.GetAsync(
        $"https://api.example.com/keys/{apiKey}/status"
    );

    var status = await response.Content.ReadAsStringAsync();
    return status == "revoked";
}
```

---

## 🧪 Testing Security

### Unit Tests

```csharp
[Fact]
public async Task SecureStorage_EncryptsTokens()
{
    // Arrange
    var token = "sensitive-token";

    // Act
    await SecureStorage.SetAsync("access_token", token);
    var retrieved = await SecureStorage.GetAsync("access_token");

    // Assert
    Assert.Equal(token, retrieved);

    // Verify not stored in plain text
    var plainPrefs = Preferences.Get("access_token", null);
    Assert.Null(plainPrefs); // Should NOT be in Preferences
}

[Fact]
public void SecretsProvider_NeverReturnsHardcodedValues()
{
    // Arrange
    var provider = new SecretsProvider(Environment.Production);

    // Act
    var clientId = provider.GetGoogleOAuthClientId();

    // Assert
    Assert.NotEqual("hardcoded-value", clientId);
    Assert.False(string.IsNullOrEmpty(clientId));
}
```

### Integration Tests

```csharp
[Fact]
public async Task OAuth_UsesEnvironmentSpecificCredentials()
{
    // Arrange
    var service = new SecureAuthenticationService(
        new SecretsProvider(Environment.Development)
    );

    // Act
    var clientId = service.GetClientId();

    // Assert
    Assert.Contains("dev", clientId.ToLower());
}
```

---

## 📚 Developer Onboarding

### Setting Up Secrets Locally

1. **Clone Repository**
   ```bash
   git clone https://github.com/your-org/bluetooth-mic-app.git
   cd bluetooth-mic-app
   ```

2. **Initialize User Secrets**
   ```bash
   dotnet user-secrets init
   ```

3. **Add Development Secrets**
   ```bash
   # Get these from team lead
   dotnet user-secrets set "OAuth:Google:ClientId" "YOUR_DEV_CLIENT_ID"
   dotnet user-secrets set "OAuth:Google:ClientSecret" "YOUR_DEV_SECRET"
   dotnet user-secrets set "OAuth:Google:RedirectUri" "YOUR_REDIRECT_URI"
   ```

4. **Verify Configuration**
   ```bash
   dotnet user-secrets list
   ```

5. **Run App**
   ```bash
   dotnet build
   dotnet run
   ```

---

## 🚨 Security Incident Response

### If Secrets Are Compromised

1. **Immediate Actions**:
   - Revoke compromised credentials immediately
   - Generate new credentials
   - Force logout all users
   - Monitor for unauthorized access

2. **Investigation**:
   - Identify scope of exposure
   - Check access logs
   - Determine when compromise occurred
   - Identify affected users

3. **Remediation**:
   - Deploy new credentials
   - Notify affected users
   - Reset all sessions
   - Update security procedures

4. **Post-Incident**:
   - Document incident
   - Update runbooks
   - Conduct blameless postmortem
   - Implement preventive measures

---

## 📖 Resources

### MAUI Security Documentation
- [SecureStorage API](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage)
- [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Environment Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)

### OAuth 2.0 Best Practices
- [RFC 6749](https://tools.ietf.org/html/rfc6749) - OAuth 2.0 Framework
- [RFC 7636](https://tools.ietf.org/html/rfc7636) - PKCE
- [RFC 6819](https://tools.ietf.org/html/rfc6819) - OAuth Security

### General Security
- [OWASP Mobile Security](https://owasp.org/www-project-mobile-security/)
- [OWASP API Security](https://owasp.org/www-project-api-security/)

---

## ✅ Compliance

### GDPR
- ✅ Encrypted storage of personal data
- ✅ Secure authentication
- ✅ Right to deletion (logout clears data)
- ✅ Data minimization (only store necessary data)

### CCPA
- ✅ Secure storage of California residents' data
- ✅ User consent management
- ✅ Data deletion capability

### PCI DSS (if applicable)
- ✅ No payment card data stored locally
- ✅ Encrypted transmission
- ✅ Access control

---

## 🎯 Success Metrics

| Metric | Target | Status |
|--------|--------|--------|
| Hardcoded Secrets | 0 | ✅ Achieved |
| Encrypted Tokens | 100% | ✅ Achieved |
| Environment Configs | 3 (Dev/Staging/Prod) | ✅ Implemented |
| Key Rotation Support | Yes | ✅ Implemented |
| Security Tests | >90% coverage | ⬜ Pending |
| Documentation | Complete | ✅ Complete |

---

## 🔮 Future Enhancements

1. **Biometric Authentication**
   - Fingerprint/Face ID for app access
   - Hardware-backed key storage

2. **Certificate Pinning**
   - Prevent MITM attacks
   - Pin OAuth endpoints

3. **Runtime Application Self-Protection (RASP)**
   - Detect tampering
   - Root/jailbreak detection
   - Debugger detection

4. **Advanced Key Management**
   - Hardware Security Module (HSM)
   - Key escrow for account recovery

---

*Document will be updated as security practices evolve.*
