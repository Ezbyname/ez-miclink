using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Simple gain (volume) control effect.
/// Multiplies all samples by a gain factor.
///
/// DIGITAL GAIN EXPLAINED:
/// Gain is simply a multiplier applied to each audio sample.
/// - Gain = 1.0: No change (0dB)
/// - Gain = 0.5: Half volume (-6dB)
/// - Gain = 2.0: Double volume (+6dB)
/// - Gain = 0.0: Silence (mute)
///
/// Why use this instead of AudioTrack.SetVolume()?
/// - AudioTrack volume doesn't affect Bluetooth SCO audio
/// - Digital gain works regardless of output device
/// - Can be placed anywhere in effect chain
/// - More precise control
///
/// THREAD SAFETY:
/// Gain value is read atomically (float read is atomic on most platforms).
/// SetGain() can be called from UI thread safely.
/// </summary>
public class GainEffect : IAudioEffect
{
    private float _gain;

    public bool Bypass { get; set; }

    public GainEffect()
    {
        _gain = 1.0f; // Unity gain (no change)
    }

    public void Prepare(int sampleRate)
    {
        // No preparation needed for gain
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (Bypass)
            return;

        // Read gain value (atomic read)
        float gain = _gain;

        // Apply gain to all samples
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] *= gain;
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is float gain)
        {
            SetGain(gain);
        }
    }

    public void Reset()
    {
        // No internal state to reset
    }

    /// <summary>
    /// Set gain/volume level.
    /// </summary>
    /// <param name="gain">Gain multiplier (0.0 = mute, 1.0 = unity, 2.0 = double)</param>
    public void SetGain(float gain)
    {
        _gain = Math.Clamp(gain, 0.0f, 2.0f); // Limit to 0-2x gain
    }

    /// <summary>
    /// Get current gain value.
    /// </summary>
    public float GetGain() => _gain;
}

/// <summary>
/// USAGE NOTES:
///
/// Converting UI percentage to gain:
/// - 100% = 1.0 gain (unity)
/// - 50% = 0.5 gain (-6dB)
/// - 200% = 2.0 gain (+6dB)
/// Formula: gain = percentage / 100.0
///
/// Converting dB to gain:
/// - 0dB = 1.0 gain
/// - -6dB = 0.5 gain
/// - +6dB = 2.0 gain
/// Formula: gain = 10^(dB / 20)
///
/// PLACEMENT IN EFFECT CHAIN:
/// Gain should typically be placed AFTER all effects but BEFORE the limiter.
/// This allows effects to work at consistent levels while giving user volume control.
///
/// Example chain:
/// Input → NoiseGate → EQ → Compressor → Effects → Gain → Limiter → Output
///                                                    ↑
///                                             User volume control
///
/// Why after effects?
/// - Effects work best at consistent input levels
/// - User can adjust final output volume without affecting effect behavior
///
/// Why before limiter?
/// - Limiter prevents clipping even if user turns gain too high
/// - Safety net for when user sets 200% gain
/// </summary>
