namespace BluetoothMicrophoneApp.Models;

/// <summary>
/// Represents a user account
/// </summary>
public class User
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public AuthProvider Provider { get; set; }
	public bool IsGuest { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime LastLoginAt { get; set; }
}

/// <summary>
/// Authentication provider types
/// </summary>
public enum AuthProvider
{
	Guest,
	PhoneNumber,
	Google,
	Apple
}
