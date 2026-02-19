using Android.Bluetooth;
using Android.Content;
using BluetoothMicrophoneApp.Services;
using Java.Util;
using AppBluetoothDevice = BluetoothMicrophoneApp.Services.BluetoothDevice;
using static BluetoothMicrophoneApp.Services.DebugLogger;

namespace BluetoothMicrophoneApp.Platforms.Android.Services;

public class BluetoothService : IBluetoothService
{
    private BluetoothAdapter? _bluetoothAdapter;
    private BluetoothSocket? _socket;
    private AppBluetoothDevice? _connectedDevice;

    public bool IsConnected => _connectedDevice != null;
    public AppBluetoothDevice? ConnectedDevice => _connectedDevice;

    public event EventHandler<AppBluetoothDevice>? DeviceConnected;
    public event EventHandler? DeviceDisconnected;

    public BluetoothService()
    {
        _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
    }

    public async Task<List<AppBluetoothDevice>> ScanForDevicesAsync()
    {
        var devices = new List<AppBluetoothDevice>();

        if (_bluetoothAdapter == null || !_bluetoothAdapter.IsEnabled)
        {
            return devices;
        }

        var bondedDevices = _bluetoothAdapter.BondedDevices;
        if (bondedDevices != null)
        {
            foreach (var device in bondedDevices)
            {
                if (device?.Name != null)
                {
                    devices.Add(new AppBluetoothDevice
                    {
                        Name = device.Name,
                        Address = device.Address ?? string.Empty,
                        IsPaired = true
                    });
                }
            }
        }

        return await Task.FromResult(devices);
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

            if (bluetoothDevice.BondState != Bond.Bonded)
            {
                Log($"ERROR: Device {device.Name} is not bonded. Bond state: {bluetoothDevice.BondState}");
                Log("SOLUTION: Please pair the device in Android Settings > Bluetooth first");
                return false;
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
}
