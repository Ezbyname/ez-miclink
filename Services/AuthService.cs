using BluetoothMicrophoneApp.Models;
using System.Text.Json;

namespace BluetoothMicrophoneApp.Services;

/// <summary>
/// Authentication service for managing user login/logout
/// Note: This is a simple implementation. For production, integrate Firebase Auth or similar.
/// </summary>
public class AuthService : IAuthService
{
	private const string UserSessionKey = "user_session";
	private const string LastLoginKey = "last_login_timestamp";

	private User? _currentUser;

	public User? CurrentUser => _currentUser;
	public bool IsAuthenticated => _currentUser != null;

	public event EventHandler<User?>? AuthStateChanged;

	public AuthService()
	{
		System.Diagnostics.Debug.WriteLine("[AuthService] Initialized");
	}

	/// <summary>
	/// Login with phone number - sends verification code
	/// </summary>
	public async Task<bool> LoginWithPhoneNumberAsync(string phoneNumber)
	{
		System.Diagnostics.Debug.WriteLine($"[AuthService] Login with phone number: {phoneNumber}");

		// TODO: Integrate with Firebase Auth or SMS provider to send verification code
		// For now, simulate sending code

		await Task.Delay(500); // Simulate network call

		// In production, send SMS with verification code here
		// Return true if code sent successfully
		return true;
	}

	/// <summary>
	/// Verify phone number with code
	/// </summary>
	public async Task<User?> VerifyPhoneNumberAsync(string phoneNumber, string verificationCode)
	{
		System.Diagnostics.Debug.WriteLine($"[AuthService] Verifying phone number: {phoneNumber} with code: {verificationCode}");

		await Task.Delay(500); // Simulate network call

		// TODO: Integrate with Firebase Auth to verify code
		// For now, accept any 6-digit code for testing
		if (verificationCode.Length == 6 && verificationCode.All(char.IsDigit))
		{
			var user = new User
			{
				Id = Guid.NewGuid().ToString(),
				Name = $"User {phoneNumber.Substring(phoneNumber.Length - 4)}",
				PhoneNumber = phoneNumber,
				Provider = AuthProvider.PhoneNumber,
				IsGuest = false,
				CreatedAt = DateTime.Now,
				LastLoginAt = DateTime.Now
			};

			await SaveSessionAsync(user);
			_currentUser = user;
			AuthStateChanged?.Invoke(this, user);

			System.Diagnostics.Debug.WriteLine($"[AuthService] Phone login successful: {user.Name}");
			return user;
		}

		System.Diagnostics.Debug.WriteLine($"[AuthService] Phone verification failed");
		return null;
	}

	/// <summary>
	/// Login with Google account
	/// </summary>
	public async Task<User?> LoginWithGoogleAsync()
	{
		System.Diagnostics.Debug.WriteLine($"[AuthService] Login with Google");

		// TODO: Integrate with Firebase Auth Google Sign-In
		// or use MAUI Community Toolkit WebAuthenticator

		await Task.Delay(1000); // Simulate OAuth flow

		// For now, create mock user
		var user = new User
		{
			Id = Guid.NewGuid().ToString(),
			Name = "Google User",
			Email = "user@gmail.com",
			Provider = AuthProvider.Google,
			IsGuest = false,
			CreatedAt = DateTime.Now,
			LastLoginAt = DateTime.Now
		};

		await SaveSessionAsync(user);
		_currentUser = user;
		AuthStateChanged?.Invoke(this, user);

		System.Diagnostics.Debug.WriteLine($"[AuthService] Google login successful: {user.Name}");
		return user;
	}

	/// <summary>
	/// Login with Apple ID
	/// </summary>
	public async Task<User?> LoginWithAppleAsync()
	{
		System.Diagnostics.Debug.WriteLine($"[AuthService] Login with Apple");

		// TODO: Integrate with Firebase Auth Apple Sign-In
		// or use MAUI Community Toolkit WebAuthenticator

		await Task.Delay(1000); // Simulate OAuth flow

		// For now, create mock user
		var user = new User
		{
			Id = Guid.NewGuid().ToString(),
			Name = "Apple User",
			Email = "user@icloud.com",
			Provider = AuthProvider.Apple,
			IsGuest = false,
			CreatedAt = DateTime.Now,
			LastLoginAt = DateTime.Now
		};

		await SaveSessionAsync(user);
		_currentUser = user;
		AuthStateChanged?.Invoke(this, user);

		System.Diagnostics.Debug.WriteLine($"[AuthService] Apple login successful: {user.Name}");
		return user;
	}

	/// <summary>
	/// Continue as guest
	/// </summary>
	public async Task<User> ContinueAsGuestAsync()
	{
		System.Diagnostics.Debug.WriteLine($"[AuthService] Continue as guest");

		var user = new User
		{
			Id = "guest_" + Guid.NewGuid().ToString().Substring(0, 8),
			Name = "Guest",
			Provider = AuthProvider.Guest,
			IsGuest = true,
			CreatedAt = DateTime.Now,
			LastLoginAt = DateTime.Now
		};

		await SaveSessionAsync(user);
		_currentUser = user;
		AuthStateChanged?.Invoke(this, user);

		System.Diagnostics.Debug.WriteLine($"[AuthService] Guest session created: {user.Id}");
		return user;
	}

	/// <summary>
	/// Logout current user
	/// </summary>
	public async Task LogoutAsync()
	{
		System.Diagnostics.Debug.WriteLine($"[AuthService] Logout user: {_currentUser?.Name}");

		ClearSession();
		_currentUser = null;
		AuthStateChanged?.Invoke(this, null);

		await Task.CompletedTask;
	}

	/// <summary>
	/// Restore previous session if exists
	/// </summary>
	public async Task<User?> RestoreSessionAsync()
	{
		System.Diagnostics.Debug.WriteLine($"[AuthService] Restoring session...");

		try
		{
			var userJson = Preferences.Get(UserSessionKey, string.Empty);

			if (string.IsNullOrWhiteSpace(userJson))
			{
				System.Diagnostics.Debug.WriteLine($"[AuthService] No saved session found");
				return null;
			}

			var user = JsonSerializer.Deserialize<User>(userJson);

			if (user != null)
			{
				// Update last login time
				user.LastLoginAt = DateTime.Now;
				await SaveSessionAsync(user);

				_currentUser = user;
				AuthStateChanged?.Invoke(this, user);

				System.Diagnostics.Debug.WriteLine($"[AuthService] Session restored: {user.Name} ({user.Provider})");
				return user;
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[AuthService] Error restoring session: {ex.Message}");
		}

		return null;
	}

	private async Task SaveSessionAsync(User user)
	{
		try
		{
			var userJson = JsonSerializer.Serialize(user);
			Preferences.Set(UserSessionKey, userJson);
			Preferences.Set(LastLoginKey, DateTime.Now.ToString("o"));

			System.Diagnostics.Debug.WriteLine($"[AuthService] Session saved: {user.Name}");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[AuthService] Error saving session: {ex.Message}");
		}

		await Task.CompletedTask;
	}

	private void ClearSession()
	{
		Preferences.Remove(UserSessionKey);
		Preferences.Remove(LastLoginKey);
		System.Diagnostics.Debug.WriteLine($"[AuthService] Session cleared");
	}
}
