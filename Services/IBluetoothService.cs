namespace BluetoothMicrophoneApp.Services;

public interface IBluetoothService
{
    Task<List<BluetoothDevice>> ScanForDevicesAsync();
    Task<bool> ConnectToDeviceAsync(BluetoothDevice device);
    Task DisconnectAsync();
    bool IsConnected { get; }
    BluetoothDevice? ConnectedDevice { get; }
    event EventHandler<BluetoothDevice>? DeviceConnected;
    event EventHandler? DeviceDisconnected;
}

public class BluetoothDevice
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsPaired { get; set; }
}
