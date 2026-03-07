namespace BluetoothMicrophoneApp.Services;

/// <summary>
/// Audio service interface with proper resource management.
/// Implementations MUST dispose of audio resources properly.
/// </summary>
public interface IAudioService : IDisposable
{
    Task<bool> StartAudioRoutingAsync(CancellationToken cancellationToken = default);
    Task StopAudioRoutingAsync(CancellationToken cancellationToken = default);
    void SetVolume(double volume);
    void SetEffect(string effectName);
    void SetNoiseReduction(bool enabled);
    string GetCurrentEffect();
    string[] GetAvailableEffects();
    void SetMasterEQ(float lowDb, float midDb, float highDb);
    void SetMasterDistortion(float amount);
    (float Low, float Mid, float High, float Distortion) GetMasterEQ();
    void ResetMasterEQ();
    bool IsRouting { get; }
    event EventHandler<string>? StatusChanged;
}
