using Microsoft.Maui.Storage;

namespace BluetoothMicrophoneApp.Services;

/// <summary>
/// Manages custom device names with persistent storage.
/// Uses normalized device addresses for consistent key generation.
/// </summary>
public class DeviceNameManager
{
	private const string CustomNamePrefix = "device_name_";

	/// <summary>
	/// Normalize device address to ensure consistent key format.
	/// Removes colons, dashes, and converts to uppercase.
	/// Example: "AA:BB:CC:DD:EE:FF" → "AABBCCDDEEFF"
	/// </summary>
	private static string NormalizeAddress(string deviceAddress)
	{
		if (string.IsNullOrWhiteSpace(deviceAddress))
			return string.Empty;

		return deviceAddress
			.Replace(":", "")
			.Replace("-", "")
			.Replace(" ", "")
			.ToUpperInvariant();
	}

	/// <summary>
	/// Get the display name for a device (custom name if set, otherwise original name).
	/// </summary>
	public static string GetDisplayName(string deviceAddress, string originalName)
	{
		System.Diagnostics.Debug.WriteLine($"[DeviceNameManager] GetDisplayName called:");
		System.Diagnostics.Debug.WriteLine($"  → Device Address (raw): {deviceAddress}");
		System.Diagnostics.Debug.WriteLine($"  → Original Name: {originalName}");

		var normalizedAddress = NormalizeAddress(deviceAddress);
		System.Diagnostics.Debug.WriteLine($"  → Normalized Address: {normalizedAddress}");

		var key = CustomNamePrefix + normalizedAddress;
		System.Diagnostics.Debug.WriteLine($"  → Preferences Key: {key}");

		var hasCustomName = Preferences.ContainsKey(key);
		System.Diagnostics.Debug.WriteLine($"  → Has Custom Name: {hasCustomName}");

		var customName = Preferences.Get(key, string.Empty);
		System.Diagnostics.Debug.WriteLine($"  → Custom Name Retrieved: '{customName}'");

		var result = string.IsNullOrWhiteSpace(customName) ? originalName : customName;
		System.Diagnostics.Debug.WriteLine($"  → Final Name Returned: '{result}'");

		return result;
	}

	/// <summary>
	/// Set a custom name for a device.
	/// </summary>
	public static bool SetCustomName(string deviceAddress, string customName)
	{
		System.Diagnostics.Debug.WriteLine($"[DeviceNameManager] SetCustomName called:");
		System.Diagnostics.Debug.WriteLine($"  → Device Address (raw): {deviceAddress}");
		System.Diagnostics.Debug.WriteLine($"  → Custom Name: '{customName}'");

		var normalizedAddress = NormalizeAddress(deviceAddress);
		System.Diagnostics.Debug.WriteLine($"  → Normalized Address: {normalizedAddress}");

		var key = CustomNamePrefix + normalizedAddress;
		System.Diagnostics.Debug.WriteLine($"  → Preferences Key: {key}");

		if (string.IsNullOrWhiteSpace(customName))
		{
			// Clear custom name
			System.Diagnostics.Debug.WriteLine($"  → Action: REMOVING custom name (empty/whitespace)");
			Preferences.Remove(key);
			return true;
		}
		else
		{
			System.Diagnostics.Debug.WriteLine($"  → Action: SAVING custom name");

			try
			{
				Preferences.Set(key, customName);

				// Immediate verification
				var verified = Preferences.Get(key, string.Empty);
				var verifySuccess = verified == customName;
				var containsKey = Preferences.ContainsKey(key);

				System.Diagnostics.Debug.WriteLine($"  → Verification: Value saved = '{verified}'");
				System.Diagnostics.Debug.WriteLine($"  → Verification: Match = {verifySuccess}");
				System.Diagnostics.Debug.WriteLine($"  → Verification: ContainsKey = {containsKey}");

				if (!verifySuccess || !containsKey)
				{
					System.Diagnostics.Debug.WriteLine($"  → ERROR: Verification FAILED! Preferences.Set() did not persist the value!");
					return false;
				}

				System.Diagnostics.Debug.WriteLine($"  → SUCCESS: Custom name saved and verified");
				return true;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"  → EXCEPTION: Failed to save custom name: {ex.Message}");
				return false;
			}
		}
	}

	/// <summary>
	/// Remove custom name for a device.
	/// </summary>
	public static void RemoveCustomName(string deviceAddress)
	{
		System.Diagnostics.Debug.WriteLine($"[DeviceNameManager] RemoveCustomName called:");
		System.Diagnostics.Debug.WriteLine($"  → Device Address (raw): {deviceAddress}");

		var normalizedAddress = NormalizeAddress(deviceAddress);
		System.Diagnostics.Debug.WriteLine($"  → Normalized Address: {normalizedAddress}");

		var key = CustomNamePrefix + normalizedAddress;
		System.Diagnostics.Debug.WriteLine($"  → Preferences Key: {key}");

		Preferences.Remove(key);

		// Verify removal
		System.Diagnostics.Debug.WriteLine($"  → Verification: ContainsKey = {Preferences.ContainsKey(key)}");
	}

	/// <summary>
	/// Check if a device has a custom name.
	/// </summary>
	public static bool HasCustomName(string deviceAddress)
	{
		System.Diagnostics.Debug.WriteLine($"[DeviceNameManager] HasCustomName called:");
		System.Diagnostics.Debug.WriteLine($"  → Device Address (raw): {deviceAddress}");

		var normalizedAddress = NormalizeAddress(deviceAddress);
		System.Diagnostics.Debug.WriteLine($"  → Normalized Address: {normalizedAddress}");

		var key = CustomNamePrefix + normalizedAddress;
		var hasCustomName = Preferences.ContainsKey(key);

		System.Diagnostics.Debug.WriteLine($"  → Preferences Key: {key}");
		System.Diagnostics.Debug.WriteLine($"  → Has Custom Name: {hasCustomName}");

		return hasCustomName;
	}

	/// <summary>
	/// Diagnostic method to list all saved custom device names.
	/// Useful for debugging persistence issues.
	/// </summary>
	public static void DiagnosticListAllCustomNames()
	{
		System.Diagnostics.Debug.WriteLine($"[DeviceNameManager] ===== DIAGNOSTIC: All Saved Custom Names =====");

		try
		{
			// MAUI Preferences doesn't have a direct way to list all keys
			// This is a workaround for debugging
			System.Diagnostics.Debug.WriteLine($"  Note: MAUI Preferences API doesn't provide key enumeration");
			System.Diagnostics.Debug.WriteLine($"  To verify persistence, check SetCustomName verification logs");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"  → ERROR: {ex.Message}");
		}

		System.Diagnostics.Debug.WriteLine($"[DeviceNameManager] ===== END DIAGNOSTIC =====");
	}
}
