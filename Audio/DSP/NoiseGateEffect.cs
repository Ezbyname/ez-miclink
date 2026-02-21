using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional noise gate with smooth envelope following.
///
/// THEORY:
/// A noise gate attenuates signal below a threshold to reduce background noise.
/// Key to natural sound:
/// 1. Smooth attack/release prevents "pumping" artifacts
/// 2. RMS detection is more musical than peak detection
/// 3. Soft knee transition avoids abrupt on/off switching
///
/// ENVELOPE FOLLOWER:
/// Uses exponential smoothing with separate attack and release times.
/// Attack is typically faster (1-10ms) to catch transients.
/// Release is slower (50-500ms) to avoid choppy gating.
///
/// The envelope coefficient formula:
/// coef = exp(-1 / (timeMs * sampleRate / 1000))
///
/// This gives 63% of target after timeMs. For 99% convergence, multiply time by ~5.
/// </summary>
public class NoiseGateEffect : IAudioEffect
{
    private NoiseGateParameters _params;
    private int _sampleRate;

    // Envelope follower state
    private float _envelope;
    private float _gain;

    // Time coefficients (calculated from attack/release times)
    private float _attackCoef;
    private float _releaseCoef;

    public bool Bypass { get; set; }

    public class NoiseGateParameters
    {
        /// <summary>Threshold in dB below which gate closes (-60 to 0)</summary>
        public float ThresholdDb { get; set; } = -40f;

        /// <summary>Attack time in milliseconds (0.1 to 50)</summary>
        public float AttackMs { get; set; } = 5f;

        /// <summary>Release time in milliseconds (10 to 1000)</summary>
        public float ReleaseMs { get; set; } = 100f;

        /// <summary>Amount of attenuation when gate is closed (0 to 1, typically 0.0 = -inf dB)</summary>
        public float FloorGain { get; set; } = 0f;

        /// <summary>Knee width in dB for smooth transition (0 = hard knee, 10 = soft)</summary>
        public float KneeDb { get; set; } = 6f;
    }

    public NoiseGateEffect()
    {
        _params = new NoiseGateParameters();
        _envelope = 0f;
        _gain = 1f;
    }

    public void Prepare(int sampleRate)
    {
        _sampleRate = sampleRate;
        UpdateCoefficients();
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (Bypass)
            return;

        // Read parameters (thread-safe single read)
        var threshold = DSPHelpers.DbToLinear(_params.ThresholdDb);
        var floorGain = _params.FloorGain;
        var kneeLinear = DSPHelpers.DbToLinear(_params.KneeDb);

        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];
            float absSample = MathF.Abs(sample);

            // Envelope follower with separate attack/release
            // When input > envelope: use attack (fast)
            // When input < envelope: use release (slow)
            float coef = absSample > _envelope ? _attackCoef : _releaseCoef;
            _envelope = _envelope * coef + absSample * (1f - coef);

            // Calculate gate gain with soft knee
            // Soft knee prevents harsh on/off switching
            float targetGain;

            if (_envelope <= threshold - kneeLinear)
            {
                // Below threshold - knee: gate closed
                targetGain = floorGain;
            }
            else if (_envelope >= threshold + kneeLinear)
            {
                // Above threshold + knee: gate open
                targetGain = 1f;
            }
            else
            {
                // Within knee: smooth transition
                // Linear interpolation across knee region
                float t = (_envelope - (threshold - kneeLinear)) / (2f * kneeLinear);
                targetGain = DSPHelpers.Lerp(floorGain, 1f, t);
            }

            // Smooth gain changes to prevent clicks
            // Use release coefficient for gain smoothing
            _gain = _gain * _releaseCoef + targetGain * (1f - _releaseCoef);

            // Apply gain
            buffer[i] = sample * _gain;
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is NoiseGateParameters p)
        {
            // Clamp parameters to safe ranges
            p.ThresholdDb = Math.Clamp(p.ThresholdDb, -60f, 0f);
            p.AttackMs = Math.Clamp(p.AttackMs, 0.1f, 50f);
            p.ReleaseMs = Math.Clamp(p.ReleaseMs, 10f, 1000f);
            p.FloorGain = Math.Clamp(p.FloorGain, 0f, 1f);
            p.KneeDb = Math.Clamp(p.KneeDb, 0f, 20f);

            _params = p;

            if (_sampleRate > 0)
                UpdateCoefficients();
        }
    }

    public void Reset()
    {
        _envelope = 0f;
        _gain = 1f;
    }

    private void UpdateCoefficients()
    {
        _attackCoef = DSPHelpers.TimeToCoefficient(_params.AttackMs, _sampleRate);
        _releaseCoef = DSPHelpers.TimeToCoefficient(_params.ReleaseMs, _sampleRate);
    }
}

/// <summary>
/// USAGE NOTES:
///
/// Typical Settings:
/// - Voice: threshold=-35dB, attack=3ms, release=150ms
/// - Drums: threshold=-25dB, attack=0.5ms, release=50ms
/// - Ambient noise: threshold=-45dB, attack=5ms, release=200ms
///
/// Avoiding Pumping:
/// 1. Use longer release times (100-300ms for voice)
/// 2. Enable soft knee (6-10dB)
/// 3. Don't set threshold too high
/// 4. Use RMS detection (inherently smoother than peak)
///
/// Attack/Release Coefficients:
/// The exponential smoothing coefficient determines how quickly
/// the envelope follows the input. Lower values = faster response.
///
/// For 1ms attack at 48kHz:
/// coef = exp(-1 / (1 * 48000 / 1000)) = exp(-1/48) ≈ 0.9794
///
/// This means each sample, the envelope moves ~2% toward the target.
/// After 48 samples (1ms), it reaches ~63% of target.
/// After ~5 time constants (5ms), it reaches ~99% of target.
/// </summary>
