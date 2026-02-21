using Microsoft.Maui.Storage;

namespace BluetoothMicrophoneApp.Services;

/// <summary>
/// Manages custom device names with persistent storage.
/// </summary>
public class DeviceNameManager
{
	private const string CustomNamePrefix = "device_name_";

	/// <summary>
	/// Get the display name for a device (custom name if set, otherwise original name).
	/// </summary>
	public static string GetDisplayName(string deviceAddress, string originalName)
	{
		System.Diagnostics.Debug.WriteLine($"[DeviceNameManager] GetDisplayName called:");
		System.Diagnostics.Debug.WriteLine($"  → Device Address: {deviceAddress}");
		System.Diagnostics.Debug.WriteLine($"  → Original Name: {originalName}");

		var key = CustomNamePrefix + deviceAddress;
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
	public static void SetCustomName(string deviceAddress, string customName)
	{
		System.Diagnostics.Debug.WriteLine($"[DeviceNameManager] SetCustomName called:");
		System.Diagnostics.Debug.WriteLine($"  → Device Address: {deviceAddress}");
		System.Diagnostics.Debug.WriteLine($"  → Custom Name: '{customName}'");

		var key = CustomNamePrefix + deviceAddress;
		System.Diagnostics.Debug.WriteLine($"  → Preferences Key: {key}");

		if (string.IsNullOrWhiteSpace(customName))
		{
			// Clear custom name
			System.Diagnostics.Debug.WriteLine($"  → Action: REMOVING custom name (empty/whitespace)");
			Preferences.Remove(key);
		}
		else
		{
			System.Diagnostics.Debug.WriteLine($"  → Action: SAVING custom name");
			Preferences.Set(key, customName);

			// Verify save
			var verified = Preferences.Get(key, string.Empty);
			System.Diagnostics.Debug.WriteLine($"  → Verification: Value saved = '{verified}'");
			System.Diagnostics.Debug.WriteLine($"  → Verification: ContainsKey = {Preferences.ContainsKey(key)}");
		}
	}

	/// <summary>
	/// Remove custom name for a device.
	/// </summary>
	public static void RemoveCustomName(string deviceAddress)
	{
		System.Diagnostics.Debug.WriteLine($"[DeviceNameManager] RemoveCustomName called:");
		System.Diagnostics.Debug.WriteLine($"  → Device Address: {deviceAddress}");

		var key = CustomNamePrefix + deviceAddress;
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
		System.Diagnostics.Debug.WriteLine($"  → Device Address: {deviceAddress}");

		var key = CustomNamePrefix + deviceAddress;
		var hasCustomName = Preferences.ContainsKey(key);

		System.Diagnostics.Debug.WriteLine($"  → Preferences Key: {key}");
		System.Diagnostics.Debug.WriteLine($"  → Has Custom Name: {hasCustomName}");

		return hasCustomName;
	}
}
