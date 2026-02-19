using BluetoothMicrophoneApp.Services;
using CoreBluetooth;
using Foundation;
using AppBluetoothDevice = BluetoothMicrophoneApp.Services.BluetoothDevice;

namespace BluetoothMicrophoneApp.Platforms.iOS.Services;

public class BluetoothService : IBluetoothService
{
    private CBCentralManager? _centralManager;
    private List<CBPeripheral> _discoveredPeripherals = new();
    private CBPeripheral? _connectedPeripheral;
    private AppBluetoothDevice? _connectedDevice;

    public bool IsConnected => _connectedPeripheral?.State == CBPeripheralState.Connected;
    public AppBluetoothDevice? ConnectedDevice => _connectedDevice;

    public event EventHandler<AppBluetoothDevice>? DeviceConnected;
    public event EventHandler? DeviceDisconnected;

    public BluetoothService()
    {
        _centralManager = new CBCentralManager();
    }

    public async Task<List<AppBluetoothDevice>> ScanForDevicesAsync()
    {
        var devices = new List<AppBluetoothDevice>();

        if (_centralManager?.State != CBManagerState.PoweredOn)
        {
            return devices;
        }

        _discoveredPeripherals.Clear();

        var tcs = new TaskCompletionSource<bool>();

        EventHandler<CBDiscoveredPeripheralEventArgs>? handler = null;
        handler = (sender, e) =>
        {
            if (e.Peripheral != null && !_discoveredPeripherals.Contains(e.Peripheral))
            {
                _discoveredPeripherals.Add(e.Peripheral);
            }
        };

        _centralManager.DiscoveredPeripheral += handler;
        _centralManager.ScanForPeripherals((CBUUID[]?)null);

        await Task.Delay(5000);

        _centralManager.StopScan();
        _centralManager.DiscoveredPeripheral -= handler;

        foreach (var peripheral in _discoveredPeripherals)
        {
            devices.Add(new AppBluetoothDevice
            {
                Name = peripheral.Name ?? "Unknown Device",
                Address = peripheral.Identifier.ToString(),
                IsPaired = peripheral.State == CBPeripheralState.Connected
            });
        }

        return devices;
    }

    public async Task<bool> ConnectToDeviceAsync(AppBluetoothDevice device)
    {
        try
        {
            if (_centralManager?.State != CBManagerState.PoweredOn)
                return false;

            await DisconnectAsync();

            var peripheral = _discoveredPeripherals.FirstOrDefault(p =>
                p.Identifier.ToString() == device.Address);

            if (peripheral == null)
                return false;

            var tcs = new TaskCompletionSource<bool>();

            EventHandler<CBPeripheralEventArgs>? connectedHandler = null;
            EventHandler<CBPeripheralErrorEventArgs>? failedHandler = null;

            connectedHandler = (sender, e) =>
            {
                if (e.Peripheral == peripheral)
                {
                    _connectedPeripheral = peripheral;
                    _connectedDevice = device;
                    DeviceConnected?.Invoke(this, device);
                    tcs.TrySetResult(true);
                }
            };

            failedHandler = (sender, e) =>
            {
                tcs.TrySetResult(false);
            };

            _centralManager.ConnectedPeripheral += connectedHandler;
            _centralManager.FailedToConnectPeripheral += failedHandler;

            _centralManager.ConnectPeripheral(peripheral);

            var result = await tcs.Task.ConfigureAwait(false);

            _centralManager.ConnectedPeripheral -= connectedHandler;
            _centralManager.FailedToConnectPeripheral -= failedHandler;

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bluetooth connection error: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connectedPeripheral != null && _centralManager != null)
        {
            _centralManager.CancelPeripheralConnection(_connectedPeripheral);
            _connectedPeripheral = null;
            _connectedDevice = null;
            DeviceDisconnected?.Invoke(this, EventArgs.Empty);
        }

        await Task.CompletedTask;
    }
}
