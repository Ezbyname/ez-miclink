namespace BluetoothMicrophoneApp.Services;

public interface IAudioService
{
    Task<bool> StartAudioRoutingAsync();
    Task StopAudioRoutingAsync();
    void SetVolume(double volume);
    bool IsRouting { get; }
    event EventHandler<string>? StatusChanged;
}
