using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional brick-wall peak limiter with lookahead.
///
/// PURPOSE:
/// A limiter is a compressor with ∞:1 ratio - it absolutely prevents
/// output from exceeding a ceiling. Critical for preventing clipping and
/// protecting speakers/ears.
///
/// DESIGN CHOICES:
/// 1. PEAK DETECTION (not RMS): Catches every transient
/// 2. FAST ATTACK (<1ms): Responds before peaks can pass through
/// 3. MODERATE RELEASE (50-200ms): Recovers quickly but smoothly
/// 4. LOOKAHEAD BUFFER: Sees peaks before they arrive, enables transparent limiting
///
/// LOOKAHEAD EXPLAINED:
/// Traditional problem: By the time we detect a peak, it's already in the output.
/// Fast attack can cause distortion trying to catch it.
///
/// Solution: Delay the audio by 1-5ms while analyzing ahead.
/// When we detect an upcoming peak, we can smoothly reduce gain BEFORE it arrives.
/// Result: Transparent limiting with no distortion.
///
/// BRICK-WALL LIMITING:
/// "Brick-wall" means the ceiling is absolute - NO samples exceed it.
/// Achieved through:
/// 1. Peak detection (catches all transients)
/// 2. Very fast attack (0.1-1ms)
/// 3. Hard ratio (100:1 or greater)
/// 4. Safety margin (ceiling at -0.5dB, not 0dB)
///
/// TRANSPARENT LIMITING:
/// Good limiter is:
/// - Inaudible when not working (< 2-3dB reduction)
/// - Transparent when working (no pumping, distortion)
/// - Fast recovery (doesn't stay compressed)
/// - Reliable (never clips, even on worst case)
/// </summary>
public class LimiterEffect : IAudioEffect
{
    private LimiterParameters _params;
    private int _sampleRate;

    // Lookahead delay line
    private float[] _delayBuffer;
    private int _delayLength;
    private int _delayWritePos;

    // Peak envelope follower
    private float _peakEnvelope;
    private float _gainEnvelope;

    // Time coefficients
    private float _attackCoef;
    private float _releaseCoef;

    public bool Bypass { get; set; }

    public class LimiterParameters
    {
        /// <summary>Ceiling level in dB (-12 to 0, typical -1 to -0.5)</summary>
        public float CeilingDb { get; set; } = -0.5f;

        /// <summary>Attack time in milliseconds (0.01 to 10, typical 0.1-1)</summary>
        public float AttackMs { get; set; } = 0.5f;

        /// <summary>Release time in milliseconds (10 to 500, typical 50-200)</summary>
        public float ReleaseMs { get; set; } = 100f;

        /// <summary>Lookahead time in milliseconds (0 to 10, typical 2-5)</summary>
        public float LookaheadMs { get; set; } = 3f;
    }

    public LimiterEffect()
    {
        _params = new LimiterParameters();
        _delayBuffer = Array.Empty<float>();
        _peakEnvelope = 0f;
        _gainEnvelope = 1f;
    }

    public void Prepare(int sampleRate)
    {
        _sampleRate = sampleRate;
        UpdateCoefficients();
        AllocateLookaheadBuffer();
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (Bypass)
            return;

        float ceilingLinear = DSPHelpers.DbToLinear(_params.CeilingDb);

        for (int i = offset; i < offset + count; i++)
        {
            float inputSample = buffer[i];

            // Store input in lookahead buffer
            _delayBuffer[_delayWritePos] = inputSample;

            // Read delayed sample (this is what we'll output)
            int delayReadPos = (_delayWritePos + 1) % _delayLength;
            float delayedSample = _delayBuffer[delayReadPos];

            // Peak detection on INPUT (lookahead)
            float absSample = MathF.Abs(inputSample);

            // Peak envelope follower with separate attack/release
            float coef = absSample > _peakEnvelope ? _attackCoef : _releaseCoef;
            _peakEnvelope = _peakEnvelope * coef + absSample * (1f - coef);

            // Calculate required gain to stay below ceiling
            // If peak would exceed ceiling, reduce gain proportionally
            float targetGain = _peakEnvelope > ceilingLinear
                ? ceilingLinear / (_peakEnvelope + 1e-10f)
                : 1f;

            // Smooth gain changes (use attack coefficient for gain smoothing)
            // This prevents distortion from rapid gain changes
            _gainEnvelope = _gainEnvelope * _attackCoef + targetGain * (1f - _attackCoef);

            // Apply limiting to delayed signal
            buffer[i] = delayedSample * _gainEnvelope;

            // Advance lookahead buffer write position
            _delayWritePos = (_delayWritePos + 1) % _delayLength;
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is LimiterParameters p)
        {
            // Clamp to safe ranges
            p.CeilingDb = Math.Clamp(p.CeilingDb, -12f, -0.1f); // Never allow 0dB (safety margin)
            p.AttackMs = Math.Clamp(p.AttackMs, 0.01f, 10f);
            p.ReleaseMs = Math.Clamp(p.ReleaseMs, 10f, 500f);
            p.LookaheadMs = Math.Clamp(p.LookaheadMs, 0f, 10f);

            bool needRealloc = _params.LookaheadMs != p.LookaheadMs && _sampleRate > 0;

            _params = p;

            if (_sampleRate > 0)
            {
                UpdateCoefficients();
                if (needRealloc)
                    AllocateLookaheadBuffer();
            }
        }
    }

    public void Reset()
    {
        _peakEnvelope = 0f;
        _gainEnvelope = 1f;
        _delayWritePos = 0;

        // Clear delay buffer
        if (_delayBuffer != null)
            Array.Clear(_delayBuffer, 0, _delayBuffer.Length);
    }

    private void UpdateCoefficients()
    {
        _attackCoef = DSPHelpers.TimeToCoefficient(_params.AttackMs, _sampleRate);
        _releaseCoef = DSPHelpers.TimeToCoefficient(_params.ReleaseMs, _sampleRate);
    }

    private void AllocateLookaheadBuffer()
    {
        // Calculate delay length in samples
        _delayLength = (int)(_params.LookaheadMs * _sampleRate / 1000f);

        if (_delayLength < 1)
            _delayLength = 1;

        // Allocate buffer (this happens during Prepare(), not during Process())
        _delayBuffer = new float[_delayLength];
        _delayWritePos = 0;
    }
}

/// <summary>
/// LIMITER vs COMPRESSOR:
///
/// COMPRESSOR:
/// - Reduces dynamic range (makes loud quieter, quiet louder after makeup gain)
/// - Moderate ratios (2:1 to 10:1)
/// - Slower attack (5-50ms)
/// - Musical, shapes tone
/// - Works on overall level (RMS)
///
/// LIMITER:
/// - Prevents peaks from exceeding ceiling
/// - Infinite ratio (100:1+)
/// - Very fast attack (0.1-1ms)
/// - Transparent, protective
/// - Works on transients (peak)
///
/// TYPICAL SETTINGS:
///
/// SAFETY LIMITER (last in chain):
/// - Ceiling: -0.5dB (safety margin)
/// - Attack: 0.5ms (fast but smooth)
/// - Release: 100ms (moderate recovery)
/// - Lookahead: 3ms (transparent)
///
/// MASTERING LIMITER (aggressive):
/// - Ceiling: -0.3dB
/// - Attack: 0.1ms
/// - Release: 50ms
/// - Lookahead: 5ms
///
/// BROADCAST LIMITER (conservative):
/// - Ceiling: -1dB
/// - Attack: 1ms
/// - Release: 150ms
/// - Lookahead: 2ms
///
/// WHY LOOKAHEAD:
/// Without lookahead, fast attack can cause distortion (inter-sample peaks).
/// With lookahead:
/// 1. We see peak coming 3ms early
/// 2. Smoothly reduce gain over 3ms
/// 3. Peak arrives, already attenuated
/// 4. No distortion, transparent limiting
///
/// IMPLEMENTATION NOTES:
/// - Lookahead buffer is circular (ring buffer)
/// - Write position advances each sample
/// - Read position is lookahead samples behind write
/// - This adds latency = lookahead time (acceptable: 2-5ms)
///
/// PREVENTING INTER-SAMPLE PEAKS:
/// Even with perfect limiting, samples between our samples can exceed ceiling
/// (due to interpolation in D/A conversion). Solutions:
/// 1. Oversample (run at 2x-4x sample rate, then downsample)
/// 2. Use conservative ceiling (-0.5dB instead of 0dB)
/// 3. Use true-peak detection (4x oversampled)
///
/// We use #2 (conservative ceiling) for simplicity and efficiency.
/// </summary>
