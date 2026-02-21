using BluetoothMicrophoneApp.Models;

namespace BluetoothMicrophoneApp.Services;

public interface IAuthService
{
	/// <summary>
	/// Current authenticated user
	/// </summary>
	User? CurrentUser { get; }

	/// <summary>
	/// Check if a user is logged in
	/// </summary>
	bool IsAuthenticated { get; }

	/// <summary>
	/// Login with phone number (sends verification code)
	/// </summary>
	Task<bool> LoginWithPhoneNumberAsync(string phoneNumber);

	/// <summary>
	/// Verify phone number with code
	/// </summary>
	Task<User?> VerifyPhoneNumberAsync(string phoneNumber, string verificationCode);

	/// <summary>
	/// Login with Google account
	/// </summary>
	Task<User?> LoginWithGoogleAsync();

	/// <summary>
	/// Login with Apple ID
	/// </summary>
	Task<User?> LoginWithAppleAsync();

	/// <summary>
	/// Continue as guest (no authentication required)
	/// </summary>
	Task<User> ContinueAsGuestAsync();

	/// <summary>
	/// Logout current user
	/// </summary>
	Task LogoutAsync();

	/// <summary>
	/// Restore previous session if exists
	/// </summary>
	Task<User?> RestoreSessionAsync();

	/// <summary>
	/// Event fired when authentication state changes
	/// </summary>
	event EventHandler<User?>? AuthStateChanged;
}
