using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional RMS compressor with soft knee and automatic makeup gain.
///
/// THEORY:
/// A compressor reduces dynamic range by attenuating loud signals.
/// When input exceeds threshold, gain reduction is applied based on ratio.
///
/// RATIO EXPLAINED:
/// Ratio = InputDb : OutputDb above threshold
/// - 1:1 = no compression
/// - 2:1 = for every 2dB input above threshold, output increases 1dB (mild)
/// - 4:1 = moderate compression (standard for voice)
/// - 8:1 = heavy compression
/// - ∞:1 = limiter (hard ceiling)
///
/// GAIN REDUCTION FORMULA:
/// When input exceeds threshold:
/// gainReductionDb = (inputDb - thresholdDb) * (1 - 1/ratio)
///
/// Example: threshold=-20dB, ratio=4:1, input=-10dB
/// gainReductionDb = (-10 - (-20)) * (1 - 1/4) = 10 * 0.75 = 7.5dB reduction
/// output = -10 - 7.5 = -17.5dB
///
/// SOFT KNEE:
/// Instead of abrupt compression at threshold, soft knee creates smooth transition.
/// Knee width defines dB range around threshold where compression gradually increases.
///
/// RMS vs PEAK DETECTION:
/// - RMS (Root Mean Square): Measures average power, more musical
/// - Peak: Measures instantaneous level, more aggressive
/// RMS is standard for voice/music compressors. Peak is used for limiters.
///
/// ATTACK/RELEASE:
/// - Attack: How fast compressor responds to loud signals (1-50ms)
/// - Release: How fast compressor releases after signal drops (50-500ms)
/// - Fast attack = more aggressive, can dull transients
/// - Slow release = smoother, more natural
///
/// MAKEUP GAIN:
/// After reducing peaks, we boost overall level to compensate.
/// Auto makeup gain = -thresholdDb * (1 - 1/ratio) * 0.5
/// This brings average level back up while maintaining headroom.
/// </summary>
public class CompressorEffect : IAudioEffect
{
    private CompressorParameters _params;
    private int _sampleRate;

    // Envelope followers
    private float _rmsEnvelope;    // Input level detection
    private float _gainEnvelope;   // Gain smoothing

    // Time coefficients
    private float _attackCoef;
    private float _releaseCoef;

    // Current gain reduction (for metering)
    private float _currentGainReduction;

    public bool Bypass { get; set; }

    public class CompressorParameters
    {
        /// <summary>Threshold in dB where compression starts (-60 to 0)</summary>
        public float ThresholdDb { get; set; } = -20f;

        /// <summary>Compression ratio (1 to 20, typical 2-8)</summary>
        public float Ratio { get; set; } = 4f;

        /// <summary>Attack time in milliseconds (0.1 to 100)</summary>
        public float AttackMs { get; set; } = 10f;

        /// <summary>Release time in milliseconds (10 to 1000)</summary>
        public float ReleaseMs { get; set; } = 100f;

        /// <summary>Knee width in dB (0 = hard knee, 10 = soft)</summary>
        public float KneeDb { get; set; } = 6f;

        /// <summary>Makeup gain in dB (0 to 20, or use auto)</summary>
        public float MakeupGainDb { get; set; } = 0f;

        /// <summary>Enable automatic makeup gain calculation</summary>
        public bool AutoMakeupGain { get; set; } = true;
    }

    public CompressorEffect()
    {
        _params = new CompressorParameters();
        _rmsEnvelope = 0f;
        _gainEnvelope = 1f;
        _currentGainReduction = 0f;
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

        // Read parameters (thread-safe)
        float thresholdDb = _params.ThresholdDb;
        float ratio = _params.Ratio;
        float kneeDb = _params.KneeDb;

        // Calculate makeup gain
        float makeupGain = _params.AutoMakeupGain
            ? DSPHelpers.DbToLinear(DSPHelpers.CalculateMakeupGain(thresholdDb, ratio))
            : DSPHelpers.DbToLinear(_params.MakeupGainDb);

        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];

            // RMS envelope follower
            // RMS = sqrt(mean(x^2))
            // We use exponential smoothing for continuous RMS estimation
            float sampleSquared = sample * sample;
            float coef = sampleSquared > _rmsEnvelope ? _attackCoef : _releaseCoef;
            _rmsEnvelope = _rmsEnvelope * coef + sampleSquared * (1f - coef);
            float rmsLevel = MathF.Sqrt(MathF.Max(_rmsEnvelope, 1e-10f));

            // Convert to dB
            float inputDb = DSPHelpers.LinearToDb(rmsLevel);

            // Calculate gain reduction with soft knee
            float gainReductionDb = CalculateGainReduction(inputDb, thresholdDb, ratio, kneeDb);

            // Convert to linear gain
            float targetGain = DSPHelpers.DbToLinear(-gainReductionDb);

            // Smooth gain changes to prevent distortion
            // Use release coefficient for gain smoothing (slower = smoother)
            _gainEnvelope = _gainEnvelope * _releaseCoef + targetGain * (1f - _releaseCoef);

            // Apply compression and makeup gain
            buffer[i] = sample * _gainEnvelope * makeupGain;

            // Store current gain reduction for metering
            _currentGainReduction = gainReductionDb;
        }
    }

    /// <summary>
    /// Calculate gain reduction in dB using soft knee compression curve.
    ///
    /// SOFT KNEE FORMULA:
    /// Define knee regions:
    /// - Below (threshold - knee/2): No compression
    /// - Within knee: Smooth quadratic transition
    /// - Above (threshold + knee/2): Full compression
    ///
    /// The quadratic knee formula ensures smooth, continuous derivative.
    /// </summary>
    private float CalculateGainReduction(float inputDb, float thresholdDb, float ratio, float kneeDb)
    {
        float knee_half = kneeDb / 2f;
        float overThreshold = inputDb - thresholdDb;

        if (overThreshold <= -knee_half)
        {
            // Below knee: no compression
            return 0f;
        }
        else if (overThreshold > -knee_half && overThreshold < knee_half)
        {
            // Within knee: smooth quadratic transition
            // This formula provides C1 continuity (smooth first derivative)
            float temp = overThreshold + knee_half;
            return (1f / ratio - 1f) * (temp * temp) / (2f * kneeDb);
        }
        else
        {
            // Above knee: full compression
            return overThreshold * (1f - 1f / ratio);
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is CompressorParameters p)
        {
            // Clamp to safe ranges
            p.ThresholdDb = Math.Clamp(p.ThresholdDb, -60f, 0f);
            p.Ratio = Math.Clamp(p.Ratio, 1f, 20f);
            p.AttackMs = Math.Clamp(p.AttackMs, 0.1f, 100f);
            p.ReleaseMs = Math.Clamp(p.ReleaseMs, 10f, 1000f);
            p.KneeDb = Math.Clamp(p.KneeDb, 0f, 20f);
            p.MakeupGainDb = Math.Clamp(p.MakeupGainDb, 0f, 20f);

            _params = p;

            if (_sampleRate > 0)
                UpdateCoefficients();
        }
    }

    public void Reset()
    {
        _rmsEnvelope = 0f;
        _gainEnvelope = 1f;
        _currentGainReduction = 0f;
    }

    private void UpdateCoefficients()
    {
        _attackCoef = DSPHelpers.TimeToCoefficient(_params.AttackMs, _sampleRate);
        _releaseCoef = DSPHelpers.TimeToCoefficient(_params.ReleaseMs, _sampleRate);
    }

    /// <summary>
    /// Get current gain reduction for metering (dB)
    /// </summary>
    public float GetGainReduction() => _currentGainReduction;
}

/// <summary>
/// TYPICAL SETTINGS:
///
/// VOICE - GENTLE (natural control):
/// - Threshold: -18dB
/// - Ratio: 3:1
/// - Attack: 15ms
/// - Release: 150ms
/// - Knee: 8dB
///
/// VOICE - BROADCAST (aggressive, consistent):
/// - Threshold: -15dB
/// - Ratio: 6:1
/// - Attack: 5ms
/// - Release: 80ms
/// - Knee: 4dB
///
/// PODCAST (smooth, professional):
/// - Threshold: -20dB
/// - Ratio: 4:1
/// - Attack: 20ms
/// - Release: 200ms
/// - Knee: 10dB
///
/// KARAOKE (forgiving, safe):
/// - Threshold: -12dB
/// - Ratio: 3:1
/// - Attack: 10ms
/// - Release: 100ms
/// - Knee: 6dB
///
/// AVOIDING "BREATHING" ARTIFACTS:
/// 1. Use slower release times (>100ms)
/// 2. Enable soft knee (6-10dB)
/// 3. Don't over-compress (ratio 3-5:1 for voice)
/// 4. Set threshold conservatively (only compress peaks)
/// 5. Use RMS detection (inherently smoother than peak)
///
/// WHY AUTO MAKEUP GAIN:
/// Manual makeup gain requires user to A/B test levels.
/// Auto makeup estimates ideal gain based on threshold and ratio:
/// - More compression = more makeup needed
/// - Conservative estimate (×0.5) prevents over-compensation
/// - User can still add extra makeup gain if needed
/// </summary>
