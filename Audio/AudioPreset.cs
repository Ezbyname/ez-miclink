namespace BluetoothMicrophoneApp.Audio;

/// <summary>
/// Audio effect preset configuration
/// </summary>
public class AudioPreset
{
    public string Name { get; set; } = string.Empty;
    public List<string> Chain { get; set; } = new();
    public Dictionary<string, Dictionary<string, object>>? Params { get; set; }
}
