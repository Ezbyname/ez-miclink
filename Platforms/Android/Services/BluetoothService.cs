using Android.Bluetooth;
using Android.Content;
using BluetoothMicrophoneApp.Services;
using Java.Util;
using System.Linq;
using AppBluetoothDevice = BluetoothMicrophoneApp.Services.BluetoothDevice;
using static BluetoothMicrophoneApp.Services.DebugLogger;

namespace BluetoothMicrophoneApp.Platforms.Android.Services;

public class BluetoothService : IBluetoothService
{
    private BluetoothAdapter? _bluetoothAdapter;
    private BluetoothSocket? _socket;
    private AppBluetoothDevice? _connectedDevice;
    private BluetoothDiscoveryReceiver? _discoveryReceiver;
    private List<AppBluetoothDevice> _discoveredDevices;

    public bool IsConnected => _connectedDevice != null;
    public AppBluetoothDevice? ConnectedDevice => _connectedDevice;

    public event EventHandler<AppBluetoothDevice>? DeviceConnected;
    public event EventHandler? DeviceDisconnected;

    public BluetoothService()
    {
        _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
        _discoveredDevices = new List<AppBluetoothDevice>();
    }

    public bool IsBluetoothEnabled()
    {
        if (_bluetoothAdapter == null)
        {
            Log("ERROR: BluetoothAdapter is null");
            return false;
        }

        return _bluetoothAdapter.IsEnabled;
    }

    public async Task<bool> RequestEnableBluetoothAsync()
    {
        try
        {
            Log("=== RequestEnableBluetoothAsync START ===");

            if (_bluetoothAdapter == null)
            {
                Log("ERROR: BluetoothAdapter is null");
                return false;
            }

            if (_bluetoothAdapter.IsEnabled)
            {
                Log("Bluetooth is already enabled");
                return true;
            }

            Log("Attempting to enable Bluetooth...");

            // Android 12+ (API 31+) requires using Intent, not Enable()
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.S)
            {
                Log("Using Intent method for Android 12+");

                try
                {
                    var intent = new global::Android.Content.Intent(global::Android.Bluetooth.BluetoothAdapter.ActionRequestEnable);
                    intent.SetFlags(global::Android.Content.ActivityFlags.NewTask);
                    Platform.CurrentActivity?.StartActivity(intent);

                    Log("Bluetooth enable Intent sent (system dialog will appear)");

                    // Wait for Bluetooth to turn on (up to 10 seconds - user needs to tap button)
                    int waitTime = 0;
                    int maxWaitTime = 10000; // 10 seconds

                    while (!_bluetoothAdapter.IsEnabled && waitTime < maxWaitTime)
                    {
                        await Task.Delay(500);
                        waitTime += 500;
                        Log($"Waiting for Bluetooth... ({waitTime}ms)");
                    }

                    if (_bluetoothAdapter.IsEnabled)
                    {
                        Log("SUCCESS: Bluetooth enabled");
                        return true;
                    }
                    else
                    {
                        Log("TIMEOUT: User may have declined or Bluetooth did not enable");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log($"ERROR with Intent method: {ex.Message}");
                    return false;
                }
            }
            else
            {
                // Android 11 and below - use direct Enable() method
                Log("Using Enable() method for Android 11 and below");

                bool success = _bluetoothAdapter.Enable();

                if (success)
                {
                    Log("Bluetooth enable request sent");

                    // Wait for Bluetooth to turn on (up to 5 seconds)
                    int waitTime = 0;
                    int maxWaitTime = 5000; // 5 seconds

                    while (!_bluetoothAdapter.IsEnabled && waitTime < maxWaitTime)
                    {
                        await Task.Delay(500);
                        waitTime += 500;
                        Log($"Waiting for Bluetooth... ({waitTime}ms)");
                    }

                    if (_bluetoothAdapter.IsEnabled)
                    {
                        Log("SUCCESS: Bluetooth enabled");
                        return true;
                    }
                    else
                    {
                        Log("TIMEOUT: Bluetooth did not enable in time");
                        return false;
                    }
                }
                else
                {
                    Log("ERROR: Failed to send Bluetooth enable request");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"FATAL ERROR in RequestEnableBluetoothAsync: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<List<AppBluetoothDevice>> ScanForDevicesAsync()
    {
        Log("=== ScanForDevicesAsync START ===");
        _discoveredDevices.Clear();

        if (_bluetoothAdapter == null)
        {
            Log("ERROR: BluetoothAdapter is null");
            return _discoveredDevices;
        }

        if (!_bluetoothAdapter.IsEnabled)
        {
            Log("ERROR: Bluetooth is not enabled");
            return _discoveredDevices;
        }

        try
        {
            // Step 1: Add already paired devices
            Log("Step 1: Getting bonded devices...");
            var bondedDevices = _bluetoothAdapter.BondedDevices;
            if (bondedDevices != null)
            {
                foreach (var device in bondedDevices)
                {
                    if (device?.Name != null && !string.IsNullOrWhiteSpace(device.Address))
                    {
                        var appDevice = new AppBluetoothDevice
                        {
                            Name = device.Name,
                            Address = device.Address,
                            IsPaired = true
                        };
                        _discoveredDevices.Add(appDevice);
                        Log($"  → Bonded: {device.Name} ({device.Address})");
                    }
                }
            }
            Log($"Found {_discoveredDevices.Count} bonded devices");

            // Step 2: Cancel any ongoing discovery
            if (_bluetoothAdapter.IsDiscovering)
            {
                Log("Canceling previous discovery...");
                _bluetoothAdapter.CancelDiscovery();
                await Task.Delay(500); // Give it time to cancel
            }

            // Step 3: Register receiver for discovery
            Log("Step 2: Starting discovery for nearby devices...");
            _discoveryReceiver = new BluetoothDiscoveryReceiver(_discoveredDevices);

            var filter = new IntentFilter();
            filter.AddAction(global::Android.Bluetooth.BluetoothDevice.ActionFound);
            filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);

            Platform.CurrentActivity?.RegisterReceiver(_discoveryReceiver, filter);
            Log("Discovery receiver registered");

            // Step 4: Start discovery
            bool discoveryStarted = _bluetoothAdapter.StartDiscovery();
            Log($"Discovery started: {discoveryStarted}");

            if (!discoveryStarted)
            {
                Log("WARNING: Could not start discovery");
                Platform.CurrentActivity?.UnregisterReceiver(_discoveryReceiver);
                return _discoveredDevices;
            }

            // Step 5: Wait for discovery to complete (max 12 seconds)
            Log("Waiting for discovery to complete (max 12 seconds)...");
            var discoveryComplete = await _discoveryReceiver.WaitForDiscoveryComplete(12000);

            if (discoveryComplete)
            {
                Log($"Discovery completed successfully");
            }
            else
            {
                Log("WARNING: Discovery timed out");
                _bluetoothAdapter.CancelDiscovery();
            }

            // Unregister receiver
            try
            {
                Platform.CurrentActivity?.UnregisterReceiver(_discoveryReceiver);
                Log("Discovery receiver unregistered");
            }
            catch (Exception ex)
            {
                Log($"Error unregistering receiver: {ex.Message}");
            }

            Log($"=== ScanForDevicesAsync END: Found {_discoveredDevices.Count} total devices ===");
            return _discoveredDevices;
        }
        catch (Exception ex)
        {
            Log($"FATAL ERROR in ScanForDevicesAsync: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");

            // Clean up on error
            try
            {
                if (_bluetoothAdapter?.IsDiscovering == true)
                    _bluetoothAdapter.CancelDiscovery();

                if (_discoveryReceiver != null)
                    Platform.CurrentActivity?.UnregisterReceiver(_discoveryReceiver);
            }
            catch { }

            return _discoveredDevices;
        }
    }

    public async Task<bool> ConnectToDeviceAsync(AppBluetoothDevice device)
    {
        try
        {
            Log("=== ConnectToDeviceAsync START ===");
            Log($"Device Name: {device.Name}");
            Log($"Device Address: {device.Address}");
            Log($"Device IsPaired: {device.IsPaired}");

            if (_bluetoothAdapter == null)
            {
                Log("ERROR: BluetoothAdapter is null");
                return false;
            }

            Log($"BluetoothAdapter State: {_bluetoothAdapter.State}");
            Log($"BluetoothAdapter IsEnabled: {_bluetoothAdapter.IsEnabled}");

            var bluetoothDevice = _bluetoothAdapter.GetRemoteDevice(device.Address);
            if (bluetoothDevice == null)
            {
                Log("ERROR: Could not get remote device");
                return false;
            }

            Log($"Remote device retrieved: {bluetoothDevice.Name}");
            Log($"Remote device type: {bluetoothDevice.Type}");
            Log($"Remote device bond state: {bluetoothDevice.BondState}");

            // Disconnect any existing connection
            await DisconnectAsync();

            // For audio devices (headphones/speakers), we don't create a socket connection.
            // The AudioManager will handle the actual audio routing.
            // We just verify the device is paired and bonded.

            // Check if device needs pairing
            if (bluetoothDevice.BondState != Bond.Bonded)
            {
                Log($"Device {device.Name} is not paired. Bond state: {bluetoothDevice.BondState}");
                Log("Initiating pairing...");

                // Attempt to pair with the device
                bool pairingStarted = bluetoothDevice.CreateBond();
                Log($"Pairing initiated: {pairingStarted}");

                if (!pairingStarted)
                {
                    Log("ERROR: Could not start pairing process");
                    return false;
                }

                // Wait for pairing to complete (user needs to confirm on both devices)
                Log("Waiting for user to confirm pairing...");
                int waitTime = 0;
                int maxWaitTime = 30000; // 30 seconds

                while (bluetoothDevice.BondState != Bond.Bonded && waitTime < maxWaitTime)
                {
                    await Task.Delay(500);
                    waitTime += 500;

                    var currentState = bluetoothDevice.BondState;
                    if (currentState == Bond.Bonded)
                    {
                        Log("Pairing successful!");
                        break;
                    }
                    else if (currentState == Bond.None)
                    {
                        Log("ERROR: Pairing was rejected or failed");
                        return false;
                    }
                }

                if (bluetoothDevice.BondState != Bond.Bonded)
                {
                    Log("ERROR: Pairing timeout - user did not confirm");
                    return false;
                }

                Log($"Device {device.Name} is now paired");
            }
            else
            {
                Log($"Device {device.Name} is already paired");
            }

            // Check if device has audio capability by checking its class
            bool isAudioDevice = false;
            var deviceClass = bluetoothDevice.BluetoothClass;
            if (deviceClass != null)
            {
                var majorClass = deviceClass.MajorDeviceClass;
                var deviceClassValue = deviceClass.DeviceClass;
                isAudioDevice = majorClass == MajorDeviceClass.AudioVideo;
                Log($"Device class - Major: {majorClass}, Device: {deviceClassValue}, IsAudio: {isAudioDevice}");
            }
            else
            {
                Log("WARNING: Device class is null");
            }

            // Check if device is currently connected to system
            try
            {
                var connectionState = bluetoothDevice.BondState;
                Log($"Device connection state: {connectionState}");
            }
            catch (Exception ex)
            {
                Log($"Could not check connection state: {ex.Message}");
            }

            // Mark device as connected (logically selected for audio routing)
            _connectedDevice = new AppBluetoothDevice
            {
                Name = device.Name,
                Address = device.Address,
                IsPaired = device.IsPaired
            };

            DeviceConnected?.Invoke(this, _connectedDevice);

            Log($"SUCCESS: Device selected for audio routing: {device.Name}");
            Log("=== ConnectToDeviceAsync END ===");
            return true;
        }
        catch (Exception ex)
        {
            Log($"FATAL ERROR in ConnectToDeviceAsync: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_connectedDevice != null)
            {
                System.Diagnostics.Debug.WriteLine($"Disconnecting from device: {_connectedDevice.Name}");
                _connectedDevice = null;
                DeviceDisconnected?.Invoke(this, EventArgs.Empty);
            }

            if (_socket != null)
            {
                _socket.Close();
                _socket.Dispose();
                _socket = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Disconnect error: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    public async Task<bool> UnpairDeviceAsync(AppBluetoothDevice device)
    {
        try
        {
            Log($"=== UnpairDeviceAsync START: {device.Name} ===");

            if (_bluetoothAdapter == null)
            {
                Log("ERROR: BluetoothAdapter is null");
                return false;
            }

            var bluetoothDevice = _bluetoothAdapter.GetRemoteDevice(device.Address);
            if (bluetoothDevice == null)
            {
                Log("ERROR: Could not get remote device");
                return false;
            }

            Log($"Device bond state: {bluetoothDevice.BondState}");

            // If connected to this device, disconnect first
            if (_connectedDevice != null && _connectedDevice.Address == device.Address)
            {
                Log("Disconnecting from device first...");
                await DisconnectAsync();
            }

            // Attempt to remove the bond (unpair)
            // Note: This uses reflection as RemoveBond is a hidden API
            if (bluetoothDevice.BondState == Bond.Bonded)
            {
                try
                {
                    var method = bluetoothDevice.Class?.GetMethod("removeBond");
                    if (method != null)
                    {
                        var result = method.Invoke(bluetoothDevice, null);
                        bool success = result is Java.Lang.Boolean javaBoolean && javaBoolean.BooleanValue();

                        if (success)
                        {
                            Log($"Successfully unpaired device: {device.Name}");

                            // Wait a bit for the unpair to complete
                            await Task.Delay(1000);
                            return true;
                        }
                        else
                        {
                            Log("RemoveBond returned false");
                            return false;
                        }
                    }
                    else
                    {
                        Log("WARNING: removeBond method not found (API limitation)");
                        // On some Android versions, unpairing programmatically is not allowed
                        // User must unpair manually from system settings
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error invoking removeBond: {ex.Message}");
                    return false;
                }
            }
            else
            {
                Log("Device is not bonded, nothing to unpair");
                return true;
            }
        }
        catch (Exception ex)
        {
            Log($"FATAL ERROR in UnpairDeviceAsync: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// BroadcastReceiver to discover nearby Bluetooth devices.
    /// </summary>
    private class BluetoothDiscoveryReceiver : BroadcastReceiver
    {
        private readonly List<AppBluetoothDevice> _devices;
        private TaskCompletionSource<bool>? _discoveryCompletionSource;
        private readonly object _lock = new object();

        public BluetoothDiscoveryReceiver(List<AppBluetoothDevice> devices)
        {
            _devices = devices;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action == null)
                return;

            try
            {
                if (intent.Action == global::Android.Bluetooth.BluetoothDevice.ActionFound)
                {
                    // Device found during discovery
                    var device = intent.GetParcelableExtra(global::Android.Bluetooth.BluetoothDevice.ExtraDevice) as global::Android.Bluetooth.BluetoothDevice;

                    if (device != null)
                    {
                        string name = device.Name ?? "Unknown Device";
                        string address = device.Address ?? "";

                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            lock (_lock)
                            {
                                // Check if device already in list
                                bool exists = _devices.Any(d => d.Address == address);

                                if (!exists)
                                {
                                    bool isPaired = device.BondState == Bond.Bonded;

                                    var appDevice = new AppBluetoothDevice
                                    {
                                        Name = name,
                                        Address = address,
                                        IsPaired = isPaired
                                    };

                                    _devices.Add(appDevice);
                                    Log($"  → Discovered: {name} ({address}) [Paired: {isPaired}]");
                                }
                            }
                        }
                    }
                }
                else if (intent.Action == BluetoothAdapter.ActionDiscoveryFinished)
                {
                    // Discovery finished
                    Log("Discovery finished");

                    lock (_lock)
                    {
                        _discoveryCompletionSource?.TrySetResult(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error in discovery receiver: {ex.Message}");
            }
        }

        public Task<bool> WaitForDiscoveryComplete(int timeoutMs)
        {
            lock (_lock)
            {
                _discoveryCompletionSource = new TaskCompletionSource<bool>();

                // Set timeout
                Task.Delay(timeoutMs).ContinueWith(_ =>
                {
                    lock (_lock)
                    {
                        _discoveryCompletionSource?.TrySetResult(false);
                    }
                });

                return _discoveryCompletionSource.Task;
            }
        }
    }
}
