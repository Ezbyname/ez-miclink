using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.Platforms.Android.Services;

public class ConnectivityDiagnostics : IConnectivityDiagnostics
{
    private readonly BluetoothAdapter? _bluetoothAdapter;
    private readonly AudioManager? _audioManager;
    private readonly Context? _context;

    public event EventHandler<string>? ConnectivityIssueDetected;

    public ConnectivityDiagnostics()
    {
        _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
        _context = Platform.CurrentActivity;
        if (_context != null)
        {
            _audioManager = (AudioManager?)_context.GetSystemService(Context.AudioService);
        }
    }

    public async Task<ConnectivityReport> PerformDiagnosticsAsync()
    {
        var report = new ConnectivityReport();

        try
        {
            // Check Bluetooth availability
            report.BluetoothEnabled = IsBluetoothAvailable();
            if (!report.BluetoothEnabled)
            {
                report.Issues.Add("Bluetooth is not enabled or not available");
                report.Recommendations.Add("Enable Bluetooth in device settings");
            }

            // Check permissions
            report.MicrophonePermissionGranted = await CheckMicrophonePermissionAsync();
            if (!report.MicrophonePermissionGranted)
            {
                report.Issues.Add("Microphone permission not granted");
                report.Recommendations.Add("Grant microphone permission to the app");
            }

            report.BluetoothPermissionGranted = await CheckBluetoothPermissionAsync();
            if (!report.BluetoothPermissionGranted)
            {
                report.Issues.Add("Bluetooth permission not granted");
                report.Recommendations.Add("Grant Bluetooth permission to the app");
            }

            // Check audio device connection
            report.AudioDeviceConnected = IsAudioDeviceConnected();
            if (!report.AudioDeviceConnected)
            {
                report.Issues.Add("No Bluetooth audio device is actively connected");
                report.Recommendations.Add("Ensure Bluetooth device is paired and connected in system settings");
            }

            // Get SCO state
            report.BluetoothScoState = GetBluetoothScoState();

            // List connected devices
            if (_bluetoothAdapter != null && _bluetoothAdapter.IsEnabled)
            {
                var bondedDevices = _bluetoothAdapter.BondedDevices;
                if (bondedDevices != null)
                {
                    foreach (var device in bondedDevices)
                    {
                        if (device?.Name != null)
                        {
                            var deviceInfo = $"{device.Name} ({device.BluetoothClass?.MajorDeviceClass})";
                            report.ConnectedDevices.Add(deviceInfo);

                            // Check if it's an audio device
                            if (device.BluetoothClass?.MajorDeviceClass == MajorDeviceClass.AudioVideo)
                            {
                                System.Diagnostics.Debug.WriteLine($"Found audio device: {device.Name}");
                            }
                        }
                    }
                }

                if (report.ConnectedDevices.Count == 0)
                {
                    report.Issues.Add("No paired Bluetooth devices found");
                    report.Recommendations.Add("Pair your Bluetooth audio device in system settings first");
                }
            }

            // Check audio manager state
            if (_audioManager != null)
            {
                try
                {
                    var isBluetoothScoAvailable = _audioManager.IsBluetoothScoAvailableOffCall;
                    System.Diagnostics.Debug.WriteLine($"Bluetooth SCO Available: {isBluetoothScoAvailable}");

                    if (!isBluetoothScoAvailable)
                    {
                        report.Issues.Add("Bluetooth SCO not available");
                        report.Recommendations.Add("Ensure device supports Bluetooth voice/audio communication");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not check SCO availability: {ex.Message}");
                }
            }

            // Notify if issues detected
            if (!report.OverallHealthy)
            {
                ConnectivityIssueDetected?.Invoke(this, $"Found {report.Issues.Count} connectivity issue(s)");
            }
        }
        catch (Exception ex)
        {
            report.Issues.Add($"Diagnostic error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Diagnostics error: {ex}");
        }

        return await Task.FromResult(report);
    }

    public bool IsBluetoothAvailable()
    {
        return _bluetoothAdapter != null && _bluetoothAdapter.IsEnabled;
    }

    public bool IsAudioDeviceConnected()
    {
        if (_audioManager == null)
            return false;

        try
        {
            // Check if Bluetooth audio is being used
            // For newer Android versions, check connected devices
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.M)
            {
                var devices = _audioManager.GetDevices(GetDevicesTargets.Outputs);
                if (devices != null)
                {
                    foreach (var device in devices)
                    {
                        if (device.Type == AudioDeviceType.BluetoothA2dp ||
                            device.Type == AudioDeviceType.BluetoothSco)
                        {
                            System.Diagnostics.Debug.WriteLine($"Found Bluetooth audio device: {device.ProductName}");
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking audio device: {ex.Message}");
        }

        return false;
    }

    public string GetBluetoothScoState()
    {
        if (_audioManager == null)
            return "AudioManager not available";

        try
        {
            var isBluetoothScoAvailable = _audioManager.IsBluetoothScoAvailableOffCall;
            var audioMode = _audioManager.Mode;

            return $"Available: {isBluetoothScoAvailable}, Mode: {audioMode}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private async Task<bool> CheckMicrophonePermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            return status == PermissionStatus.Granted;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckBluetoothPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            return status == PermissionStatus.Granted;
        }
        catch
        {
            return false;
        }
    }
}
