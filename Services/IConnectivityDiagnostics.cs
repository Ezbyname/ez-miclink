namespace BluetoothMicrophoneApp.Services;

public interface IConnectivityDiagnostics
{
    /// <summary>
    /// Performs a comprehensive connectivity check
    /// </summary>
    Task<ConnectivityReport> PerformDiagnosticsAsync();

    /// <summary>
    /// Checks if Bluetooth is enabled and available
    /// </summary>
    bool IsBluetoothAvailable();

    /// <summary>
    /// Checks if a Bluetooth audio device is connected
    /// </summary>
    bool IsAudioDeviceConnected();

    /// <summary>
    /// Gets the current Bluetooth SCO connection state
    /// </summary>
    string GetBluetoothScoState();

    /// <summary>
    /// Event raised when connectivity issues are detected
    /// </summary>
    event EventHandler<string>? ConnectivityIssueDetected;
}

public class ConnectivityReport
{
    public bool BluetoothEnabled { get; set; }
    public bool AudioDeviceConnected { get; set; }
    public bool MicrophonePermissionGranted { get; set; }
    public bool BluetoothPermissionGranted { get; set; }
    public string BluetoothScoState { get; set; } = string.Empty;
    public List<string> ConnectedDevices { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public bool OverallHealthy => Issues.Count == 0;

    public override string ToString()
    {
        var report = "=== Connectivity Diagnostics Report ===\n";
        report += $"Bluetooth Enabled: {BluetoothEnabled}\n";
        report += $"Audio Device Connected: {AudioDeviceConnected}\n";
        report += $"Microphone Permission: {MicrophonePermissionGranted}\n";
        report += $"Bluetooth Permission: {BluetoothPermissionGranted}\n";
        report += $"Bluetooth SCO State: {BluetoothScoState}\n";
        report += $"Connected Devices: {string.Join(", ", ConnectedDevices)}\n";
        report += $"\nIssues Found: {Issues.Count}\n";
        foreach (var issue in Issues)
        {
            report += $"  ⚠️ {issue}\n";
        }
        report += $"\nRecommendations: {Recommendations.Count}\n";
        foreach (var rec in Recommendations)
        {
            report += $"  💡 {rec}\n";
        }
        report += $"\nOverall Status: {(OverallHealthy ? "✓ HEALTHY" : "⚠️ ISSUES DETECTED")}";
        return report;
    }
}
