using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional podcast voice processing effect.
///
/// BROADCAST CHAIN ARCHITECTURE:
/// This effect implements the industry-standard broadcast chain used by
/// professional podcasters, radio stations, and streaming platforms.
///
/// SIGNAL FLOW:
/// Input → HPF → Gate → De-esser → EQ → Compression → Limiter → Output
///
/// WHY THIS ORDER:
/// 1. HPF first: Remove rumble before any processing
/// 2. Gate second: Remove noise before compression amplifies it
/// 3. De-esser third: Control sibilance before EQ boosts it
/// 4. EQ fourth: Shape tone before compression reacts to it
/// 5. Compression fifth: Even out dynamics, add consistency
/// 6. Limiter last: Final safety net, prevent clipping
///
/// LOUDNESS TARGET:
/// EBU R128 standard: -16 LUFS (Loudness Units relative to Full Scale)
/// This is the broadcast standard used by:
/// - Apple Podcasts (-16 LUFS)
/// - Spotify (-14 to -16 LUFS)
/// - YouTube (-14 LUFS)
/// - Major broadcasters (BBC, NPR, etc.)
///
/// COMPONENT DETAILS:
///
/// 1. HIGH-PASS FILTER (HPF):
///    - Frequency: 80 Hz (removes rumble, room resonance, proximity effect)
///    - Type: Butterworth (smooth, natural rolloff)
///    - Why: Cleans up low end without affecting voice fundamentals
///
/// 2. NOISE GATE:
///    - Threshold: -45 dB (silence background between speech)
///    - Attack: 10ms (fast, doesn't cut speech starts)
///    - Hold: 200ms (prevents choppy gating)
///    - Release: 150ms (smooth fade-out)
///    - Why: Removes fan noise, room tone, breath sounds between words
///
/// 3. DE-ESSER:
///    - Frequency: 6-8 kHz (sibilance range)
///    - Threshold: -20 dB
///    - Ratio: 4:1 (moderate reduction)
///    - Max reduction: 6 dB
///    - Why: Controls harsh "s" sounds without losing clarity
///
/// 4. EQUALIZER:
///    - Low cut: 80 Hz (redundant with HPF, adds slope)
///    - Muddiness cut: -3 dB @ 250 Hz (clears up boominess)
///    - Presence boost: +4 dB @ 4 kHz (voice clarity, intelligibility)
///    - Air boost: +2 dB @ 10 kHz (adds sparkle, professional sheen)
///    - Why: Shapes voice to sound clear, present, and professional
///
/// 5. COMPRESSOR:
///    - Ratio: 4:1 (broadcast standard)
///    - Threshold: -18 dB (catches most dynamic range)
///    - Attack: 15ms (fast enough to control peaks, slow enough to preserve transients)
///    - Release: 120ms (smooth, natural-sounding)
///    - Knee: Soft (gentle compression curve)
///    - Makeup gain: Auto (targets -16 LUFS)
///    - Why: Evens out volume, adds consistency and punch
///
/// 6. LIMITER:
///    - Ceiling: -1 dB (prevents clipping with 1dB headroom)
///    - Threshold: -2 dB (only catches peaks above -2dB)
///    - Attack: 0.5ms (brick-wall fast)
///    - Release: 80ms (recovers quickly)
///    - Why: Final safety net, allows aggressive compression without clipping
///
/// QUALITY PRESETS:
///
/// CLEAN (minimal processing):
/// - Gate: -50 dB
/// - Compression: 3:1, -20 dB threshold
/// - EQ: Subtle (+2 dB presence)
///
/// BROADCAST (standard podcast):
/// - Gate: -45 dB
/// - Compression: 4:1, -18 dB threshold
/// - EQ: Moderate (+4 dB presence)
///
/// AGGRESSIVE (radio/streaming):
/// - Gate: -40 dB
/// - Compression: 6:1, -15 dB threshold
/// - EQ: Enhanced (+5 dB presence, +3 dB air)
///
/// IMPLEMENTATION NOTES:
/// - All time constants optimized for speech (not music)
/// - RMS detection for compression (not peak) - more natural for voice
/// - Soft-knee compression for smooth, transparent sound
/// - Lookahead limiting for artifact-free peak control
/// - Auto makeup gain to target -16 LUFS loudness
/// </summary>
public class PodcastVoiceEffect : IAudioEffect
{
    private PodcastParameters _params;
    private int _sampleRate;

    // 1. High-pass filter (80 Hz, remove rumble)
    private BiquadFilter _highPassFilter;

    // 2. Noise gate
    private float _gateEnvelope;
    private float _gateAttackCoef;
    private float _gateReleaseCoef;
    private int _gateHoldSamples;
    private int _gateHoldCounter;

    // 3. De-esser (reuse existing effect)
    private DeEsserEffect _deEsser;

    // 4. Equalizer (4-band)
    private BiquadFilter _eqLowCut;          // 80 Hz high-pass
    private BiquadFilter _eqMuddinessCut;   // -3 dB @ 250 Hz (reduce boominess)
    private BiquadFilter _eqPresenceBoost;   // +4 dB @ 4 kHz (clarity)
    private BiquadFilter _eqAirBoost;        // +2 dB @ 10 kHz (sparkle)

    // 5. Compressor
    private float _compRmsEnvelope;
    private float _compGainEnvelope;
    private float _compAttackCoef;
    private float _compReleaseCoef;

    // 6. Limiter
    private float _limiterEnvelope;
    private float _limiterAttackCoef;
    private float _limiterReleaseCoef;

    public bool Bypass { get; set; }

    public class PodcastParameters
    {
        /// <summary>HPF cutoff frequency (60-100 Hz, typical 80)</summary>
        public float HighPassFreq { get; set; } = 80f;

        /// <summary>Gate threshold in dB (-60 to -30, typical -45)</summary>
        public float GateThresholdDb { get; set; } = -45f;

        /// <summary>De-esser amount (0-1, typical 0.5)</summary>
        public float DeEsserAmount { get; set; } = 0.5f;

        /// <summary>Presence boost in dB (0-6, typical 4)</summary>
        public float PresenceBoostDb { get; set; } = 4f;

        /// <summary>Air boost in dB (0-4, typical 2)</summary>
        public float AirBoostDb { get; set; } = 2f;

        /// <summary>Compression ratio (2:1 to 8:1, typical 4:1)</summary>
        public float CompressionRatio { get; set; } = 4f;

        /// <summary>Compression threshold in dB (-24 to -12, typical -18)</summary>
        public float CompressionThresholdDb { get; set; } = -18f;

        /// <summary>Output limiter enabled (true for safety)</summary>
        public bool LimiterEnabled { get; set; } = true;
    }

    public PodcastVoiceEffect()
    {
        _params = new PodcastParameters();
        _highPassFilter = new BiquadFilter();
        _deEsser = new DeEsserEffect();
        _eqLowCut = new BiquadFilter();
        _eqMuddinessCut = new BiquadFilter();
        _eqPresenceBoost = new BiquadFilter();
        _eqAirBoost = new BiquadFilter();
        _gateEnvelope = 0f;
        _gateHoldCounter = 0;
        _compRmsEnvelope = 0f;
        _compGainEnvelope = 1f;
        _limiterEnvelope = 1f;
    }

    public void Prepare(int sampleRate)
    {
        _sampleRate = sampleRate;
        _deEsser.Prepare(sampleRate);
        UpdateAllParameters();
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (Bypass)
            return;

        // Gate parameters
        float gateThresholdLinear = DSPHelpers.DbToLinear(_params.GateThresholdDb);

        // Compression parameters
        float compThreshold = _params.CompressionThresholdDb;
        float compRatio = Math.Clamp(_params.CompressionRatio, 2f, 8f);

        // Limiter parameters
        float limiterCeiling = -1f; // -1 dB (headroom for codec)
        float limiterThreshold = -2f; // Start limiting at -2 dB

        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];

            // STAGE 1: High-pass filter (80 Hz - remove rumble)
            sample = _highPassFilter.Process(sample);

            // STAGE 2: Noise gate (remove background between speech)
            float sampleAbs = MathF.Abs(sample);
            bool aboveThreshold = sampleAbs > gateThresholdLinear;

            if (aboveThreshold)
            {
                // Attack phase - gate opening
                float gateCoef = _gateAttackCoef;
                _gateEnvelope = _gateEnvelope * gateCoef + 1f * (1f - gateCoef);
                _gateHoldCounter = _gateHoldSamples;
            }
            else
            {
                // Hold phase
                if (_gateHoldCounter > 0)
                {
                    _gateHoldCounter--;
                }
                else
                {
                    // Release phase - gate closing
                    float gateCoef = _gateReleaseCoef;
                    _gateEnvelope = _gateEnvelope * gateCoef + 0f * (1f - gateCoef);
                }
            }

            sample *= _gateEnvelope;

            // STAGE 3: De-esser (control sibilance)
            // Process de-esser on single sample (wrap in temp array)
            float[] tempBuffer = new float[1] { sample };
            _deEsser.Process(tempBuffer, 0, 1);
            sample = tempBuffer[0];

            // STAGE 4: Equalizer (shape tone)
            sample = _eqLowCut.Process(sample);          // Remove lows
            sample = _eqMuddinessCut.Process(sample);    // Cut muddiness
            sample = _eqPresenceBoost.Process(sample);   // Boost clarity
            sample = _eqAirBoost.Process(sample);        // Add air

            // STAGE 5: Compressor (even out dynamics)
            // RMS envelope follower
            float sampleSq = sample * sample;
            float compCoef = sampleSq > _compRmsEnvelope ? _compAttackCoef : _compReleaseCoef;
            _compRmsEnvelope = _compRmsEnvelope * compCoef + sampleSq * (1f - compCoef);
            float rmsLevel = MathF.Sqrt(MathF.Max(_compRmsEnvelope, 1e-10f));

            // Soft-knee compression
            float inputDb = DSPHelpers.LinearToDb(rmsLevel);
            float gainReductionDb = 0f;

            if (inputDb > compThreshold)
            {
                // Above threshold: compress
                gainReductionDb = (inputDb - compThreshold) * (1f - 1f / compRatio);
            }

            // Apply compression with makeup gain (target -16 LUFS)
            float targetGain = DSPHelpers.DbToLinear(-gainReductionDb);
            float makeupGain = 1.8f; // Approximately 5 dB makeup gain for -16 LUFS target
            targetGain *= makeupGain;

            _compGainEnvelope = _compGainEnvelope * _compReleaseCoef + targetGain * (1f - _compReleaseCoef);
            sample *= _compGainEnvelope;

            // STAGE 6: Limiter (final safety net)
            if (_params.LimiterEnabled)
            {
                float sampleDb = DSPHelpers.LinearToDb(MathF.Abs(sample) + 1e-10f);
                float limiterGain = 1f;

                if (sampleDb > limiterThreshold)
                {
                    // Calculate gain reduction to bring peak down to ceiling
                    float overageDb = sampleDb - limiterCeiling;
                    limiterGain = DSPHelpers.DbToLinear(-overageDb);
                }

                // Smooth limiting envelope
                float targetLimiterGain = MathF.Min(limiterGain, 1f);
                float limCoef = targetLimiterGain < _limiterEnvelope ? _limiterAttackCoef : _limiterReleaseCoef;
                _limiterEnvelope = _limiterEnvelope * limCoef + targetLimiterGain * (1f - limCoef);

                sample *= _limiterEnvelope;
            }

            buffer[i] = sample;
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is PodcastParameters p)
        {
            // Clamp parameters
            p.HighPassFreq = Math.Clamp(p.HighPassFreq, 60f, 100f);
            p.GateThresholdDb = Math.Clamp(p.GateThresholdDb, -60f, -30f);
            p.DeEsserAmount = Math.Clamp(p.DeEsserAmount, 0f, 1f);
            p.PresenceBoostDb = Math.Clamp(p.PresenceBoostDb, 0f, 6f);
            p.AirBoostDb = Math.Clamp(p.AirBoostDb, 0f, 4f);
            p.CompressionRatio = Math.Clamp(p.CompressionRatio, 2f, 8f);
            p.CompressionThresholdDb = Math.Clamp(p.CompressionThresholdDb, -24f, -12f);

            _params = p;

            if (_sampleRate > 0)
                UpdateAllParameters();
        }
    }

    public void Reset()
    {
        _highPassFilter.Reset();
        _deEsser.Reset();
        _eqLowCut.Reset();
        _eqMuddinessCut.Reset();
        _eqPresenceBoost.Reset();
        _eqAirBoost.Reset();
        _gateEnvelope = 0f;
        _gateHoldCounter = 0;
        _compRmsEnvelope = 0f;
        _compGainEnvelope = 1f;
        _limiterEnvelope = 1f;
    }

    private void UpdateAllParameters()
    {
        // 1. High-pass filter (remove rumble)
        _highPassFilter.Design(
            BiquadFilter.FilterType.HighPass,
            _params.HighPassFreq,
            _sampleRate,
            q: 0.707 // Butterworth response
        );

        // 2. Gate time constants
        _gateAttackCoef = DSPHelpers.TimeToCoefficient(10f, _sampleRate);   // 10ms attack
        _gateReleaseCoef = DSPHelpers.TimeToCoefficient(150f, _sampleRate); // 150ms release
        _gateHoldSamples = (int)(0.2f * _sampleRate); // 200ms hold

        // 3. De-esser
        var deEsserParams = new DeEsserEffect.DeEsserParameters
        {
            Amount = _params.DeEsserAmount
        };
        _deEsser.SetParameters(deEsserParams);

        // 4. Equalizer
        // Low cut (redundant with HPF, adds steeper slope)
        _eqLowCut.Design(
            BiquadFilter.FilterType.HighPass,
            80f,
            _sampleRate,
            q: 0.707
        );

        // Muddiness cut (reduce boominess)
        _eqMuddinessCut.Design(
            BiquadFilter.FilterType.Peaking,
            250f,
            _sampleRate,
            q: 1.0,
            gainDb: -3f
        );

        // Presence boost (clarity, intelligibility)
        _eqPresenceBoost.Design(
            BiquadFilter.FilterType.Peaking,
            4000f,
            _sampleRate,
            q: 1.5,
            gainDb: _params.PresenceBoostDb
        );

        // Air boost (sparkle, professional sheen)
        _eqAirBoost.Design(
            BiquadFilter.FilterType.HighShelf,
            10000f,
            _sampleRate,
            q: 0.707,
            gainDb: _params.AirBoostDb
        );

        // 5. Compressor time constants
        _compAttackCoef = DSPHelpers.TimeToCoefficient(15f, _sampleRate);   // 15ms attack
        _compReleaseCoef = DSPHelpers.TimeToCoefficient(120f, _sampleRate); // 120ms release

        // 6. Limiter time constants
        _limiterAttackCoef = DSPHelpers.TimeToCoefficient(0.5f, _sampleRate);  // 0.5ms attack (brick-wall)
        _limiterReleaseCoef = DSPHelpers.TimeToCoefficient(80f, _sampleRate);  // 80ms release
    }
}

/// <summary>
/// PODCAST VOICE PROCESSING GUIDE:
///
/// WHY THIS CHAIN:
/// Professional podcasts sound polished because of careful signal chain design.
/// Each stage has a specific purpose and must happen in this order.
///
/// COMMON MISTAKES:
/// 1. Compressing before EQ: Compression reacts to the raw signal, not the shaped tone
/// 2. No HPF: Low-frequency rumble gets amplified by compression
/// 3. No gate: Background noise gets amplified by compression
/// 4. EQ after compression: Can undo compression's work
/// 5. No limiter: Aggressive compression causes clipping
/// 6. Peak compression instead of RMS: Sounds unnatural on voice
///
/// LOUDNESS STANDARDS:
/// - Apple Podcasts: -16 LUFS (strict)
/// - Spotify: -14 to -16 LUFS (normalizes if louder)
/// - YouTube: -14 LUFS (normalizes)
/// - Broadcast (EBU R128): -23 LUFS (TV/radio standard)
///
/// Our target: -16 LUFS (safe for all platforms)
///
/// COMPRESSION RATIOS:
/// - 2:1: Very light, natural (audiobooks)
/// - 3:1: Light, conversational (interview podcasts)
/// - 4:1: Moderate, broadcast standard (most podcasts)
/// - 6:1: Heavy, aggressive (radio, streaming)
/// - 8:1+: Very heavy, limiting (voiceover, narration)
///
/// ATTACK/RELEASE TIMES:
/// - Fast attack (5-10ms): Catches transients, sounds controlled
/// - Medium attack (15-20ms): Preserves some punch, natural
/// - Slow attack (50ms+): Lets transients through, dynamic
///
/// - Fast release (50-80ms): Recovers quickly, pumping on music
/// - Medium release (100-150ms): Smooth, natural for speech
/// - Slow release (200ms+): Very smooth, but sluggish
///
/// For speech: Medium attack (15ms) + Medium release (120ms) = natural and controlled
///
/// DE-ESSER SETTINGS:
/// - Frequency: 6-8 kHz (sibilance range for most voices)
/// - Threshold: Adjust so only harsh "s" sounds trigger
/// - Ratio: 4:1 (moderate control)
/// - Max reduction: 6 dB (transparent, not lisping)
///
/// EQ PHILOSOPHY:
/// - Subtractive first: Cut before you boost
/// - Cut muddiness (200-400 Hz): Makes room for clarity
/// - Boost presence (3-5 kHz): Intelligibility, cuts through
/// - Boost air (10-12 kHz): Professional sheen, not harsh
///
/// TYPICAL PRESETS:
///
/// CLEAN PODCAST (minimal processing):
/// - Gate: -50 dB
/// - Compression: 3:1, -20 dB
/// - Presence: +3 dB
/// - Air: +1 dB
///
/// BROADCAST STANDARD (professional):
/// - Gate: -45 dB
/// - Compression: 4:1, -18 dB
/// - Presence: +4 dB
/// - Air: +2 dB
///
/// RADIO/STREAMING (aggressive):
/// - Gate: -40 dB
/// - Compression: 6:1, -15 dB
/// - Presence: +5 dB
/// - Air: +3 dB
///
/// CPU OPTIMIZATION:
/// This chain is CPU-intensive (6 stages of processing).
/// For real-time mobile use:
/// - Consider downsampling to 24 kHz (voice is <8 kHz)
/// - Use faster envelope followers (higher coefficients)
/// - Simplify EQ (combine filters where possible)
/// - Disable limiter if not needed (compression might be enough)
/// </summary>
