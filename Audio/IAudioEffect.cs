namespace BluetoothMicrophoneApp.Audio;

/// <summary>
/// Base interface for all audio effects
/// Real-time safe: no allocations in Process()
/// </summary>
public interface IAudioEffect
{
    string Name { get; }
    bool Bypass { get; set; }

    /// <summary>
    /// Prepare effect for processing
    /// </summary>
    void Prepare(int sampleRate, int channels);

    /// <summary>
    /// Reset effect state (clear buffers, envelopes, etc.)
    /// </summary>
    void Reset();

    /// <summary>
    /// Process audio buffer in-place (real-time safe)
    /// </summary>
    void Process(AudioBuffer buffer);

    /// <summary>
    /// Set parameters (thread-safe, double-buffered)
    /// </summary>
    void SetParameters(Dictionary<string, object> parameters);
}
