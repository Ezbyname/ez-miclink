namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Interface for real-time audio effects.
/// All implementations must be thread-safe for parameter updates and zero-allocation during processing.
/// </summary>
public interface IAudioEffect
{
    /// <summary>
    /// Prepare the effect for processing at the given sample rate.
    /// Called once before processing starts or when sample rate changes.
    /// </summary>
    void Prepare(int sampleRate);

    /// <summary>
    /// Process audio samples in-place. Must be zero-allocation and thread-safe.
    /// </summary>
    /// <param name="buffer">Audio buffer containing samples in range [-1.0, 1.0]</param>
    /// <param name="offset">Starting offset in buffer</param>
    /// <param name="count">Number of samples to process</param>
    void Process(float[] buffer, int offset, int count);

    /// <summary>
    /// Update effect parameters atomically. Must be thread-safe.
    /// </summary>
    void SetParameters(object parameters);

    /// <summary>
    /// Reset internal state (clear delays, envelopes, etc.)
    /// </summary>
    void Reset();

    /// <summary>
    /// Bypass state - if true, effect passes audio through unmodified
    /// </summary>
    bool Bypass { get; set; }
}
