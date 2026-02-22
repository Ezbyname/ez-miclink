using BluetoothMicrophoneApp.Models;

namespace BluetoothMicrophoneApp.Services;

/// <summary>
/// Tracks which devices have successfully connected to the app before.
/// This allows us to show "Compatible Devices" vs "Available Devices" lists.
/// </summary>
public static class DeviceConnectionHistory
{
    private const string CONNECTED_DEVICES_KEY = "connected_device_history";
    private static HashSet<string>? _connectedDeviceAddresses;

    /// <summary>
    /// Mark a device as successfully connected to the app.
    /// </summary>
    public static void MarkDeviceAsConnected(string deviceAddress)
    {
        if (string.IsNullOrWhiteSpace(deviceAddress))
            return;

        EnsureLoaded();

        if (_connectedDeviceAddresses!.Add(deviceAddress))
        {
            SaveToPreferences();
        }
    }

    /// <summary>
    /// Check if a device has connected to the app before.
    /// </summary>
    public static bool HasConnectedBefore(string deviceAddress)
    {
        if (string.IsNullOrWhiteSpace(deviceAddress))
            return false;

        EnsureLoaded();
        return _connectedDeviceAddresses!.Contains(deviceAddress);
    }

    /// <summary>
    /// Get all device addresses that have connected before.
    /// </summary>
    public static HashSet<string> GetConnectedDeviceAddresses()
    {
        EnsureLoaded();
        return new HashSet<string>(_connectedDeviceAddresses!);
    }

    /// <summary>
    /// Split a device list into compatible (connected before) and available (new) devices.
    /// </summary>
    public static (List<BluetoothDevice> compatible, List<BluetoothDevice> available)
        SplitDeviceList(List<BluetoothDevice> allDevices)
    {
        EnsureLoaded();

        var compatible = new List<BluetoothDevice>();
        var available = new List<BluetoothDevice>();

        foreach (var device in allDevices)
        {
            if (_connectedDeviceAddresses!.Contains(device.Address))
            {
                compatible.Add(device);
            }
            else
            {
                available.Add(device);
            }
        }

        return (compatible, available);
    }

    private static void EnsureLoaded()
    {
        if (_connectedDeviceAddresses != null)
            return;

        // Load from preferences
        var savedAddresses = Preferences.Get(CONNECTED_DEVICES_KEY, string.Empty);

        if (string.IsNullOrWhiteSpace(savedAddresses))
        {
            _connectedDeviceAddresses = new HashSet<string>();
        }
        else
        {
            var addresses = savedAddresses.Split('|', StringSplitOptions.RemoveEmptyEntries);
            _connectedDeviceAddresses = new HashSet<string>(addresses);
        }
    }

    private static void SaveToPreferences()
    {
        if (_connectedDeviceAddresses == null || _connectedDeviceAddresses.Count == 0)
        {
            Preferences.Remove(CONNECTED_DEVICES_KEY);
            return;
        }

        var joined = string.Join("|", _connectedDeviceAddresses);
        Preferences.Set(CONNECTED_DEVICES_KEY, joined);
    }

    /// <summary>
    /// Clear all connection history (for testing/debugging).
    /// </summary>
    public static void ClearHistory()
    {
        _connectedDeviceAddresses?.Clear();
        Preferences.Remove(CONNECTED_DEVICES_KEY);
    }
}
