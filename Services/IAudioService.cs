namespace BluetoothMicrophoneApp.Services;

public interface IAudioService
{
    Task<bool> StartAudioRoutingAsync();
    Task StopAudioRoutingAsync();
    void SetVolume(double volume);
    void SetEffect(string effectName);
    string GetCurrentEffect();
    string[] GetAvailableEffects();
    bool IsRouting { get; }
    event EventHandler<string>? StatusChanged;
}
