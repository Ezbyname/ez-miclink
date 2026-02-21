using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional Deep/Bass voice effect.
///
/// WHAT MAKES DEEP VOICE REALISTIC:
/// 1. PITCH SHIFT DOWN: -2 to -5 semitones (lower fundamental)
/// 2. FORMANT SHIFT DOWN: -5% to -10% (larger vocal tract simulation)
/// 3. BASS BOOST: 120-250Hz (add chest resonance)
/// 4. MID CUT: Slight reduction at 3kHz (reduce nasality)
/// 5. COMPRESSION: Soft (maintain authority without pumping)
///
/// VOICE DEPTH PERCEPTION:
/// Deep voice = combination of:
/// - Lower pitch (fundamental frequency ~85-110Hz for deep male)
/// - Lower formants (larger vocal tract)
/// - More bass energy (chest resonance)
/// - Slight reduction in high frequencies
///
/// FORMANT FREQUENCIES:
/// Adult male "ah": F1=730Hz, F2=1090Hz, F3=2440Hz
/// Large male "ah": F1=650Hz, F2=980Hz, F3=2200Hz
/// Shift down ~10% to simulate larger vocal tract.
///
/// WHY NOT JUST PITCH SHIFT:
/// Simple pitch shift down creates "monster" voice (unnatural).
/// Need to:
/// 1. Lower pitch moderately (-3 to -5 semitones)
/// 2. Lower formants slightly (-5 to -10%)
/// 3. Add bass warmth (EQ boost 100-200Hz)
/// 4. Maintain clarity (don't over-boost bass)
///
/// RADIO/BROADCASTER VOICE:
/// Professional announcers often have:
/// - Natural deeper pitch
/// - Enhanced bass through proximity effect
/// - Compressed dynamics
/// - Presence boost for clarity
/// This effect simulates that character.
///
/// IMPLEMENTATION:
/// - Pitch shift: Time-domain SOLA (-3 to -5 semitones)
/// - Formant shift: Low-shelf boost + filtering
/// - Bass enhancement: Low-shelf filter
/// - Presence control: Slight cut at 3kHz
/// - Compression: Light RMS compression
/// </summary>
public class DeepVoiceEffect : IAudioEffect
{
    private DeepVoiceParameters _params;
    private int _sampleRate;

    // Pitch shifter
    private SimplePitchShifter _pitchShifter;

    // Formant shifting (downward = emphasize lows)
    private BiquadFilter _formantShift1; // Low emphasis
    private BiquadFilter _formantShift2; // Mid reduction

    // Bass enhancement
    private BiquadFilter _bassBoost;

    // Presence control (reduce nasality)
    private BiquadFilter _presenceCut;

    // Light compression
    private float _rmsEnvelope;
    private float _gainEnvelope;
    private float _compAttackCoef;
    private float _compReleaseCoef;

    public bool Bypass { get; set; }

    public class DeepVoiceParameters
    {
        /// <summary>Pitch shift in semitones (-1 to -7, typical -3 to -5)</summary>
        public float PitchSemitones { get; set; } = -4f;

        /// <summary>Formant shift percentage (-20 to 0%, typical -8%)</summary>
        public float FormantShiftPercent { get; set; } = -8f;

        /// <summary>Bass boost in dB (0-8, typical 4)</summary>
        public float BassBoostDb { get; set; } = 4f;

        /// <summary>Effect intensity (0-1, affects overall blend)</summary>
        public float Intensity { get; set; } = 1.0f;
    }

    public DeepVoiceEffect()
    {
        _params = new DeepVoiceParameters();
        _pitchShifter = new SimplePitchShifter();
        _formantShift1 = new BiquadFilter();
        _formantShift2 = new BiquadFilter();
        _bassBoost = new BiquadFilter();
        _presenceCut = new BiquadFilter();
        _rmsEnvelope = 0f;
        _gainEnvelope = 1f;
    }

    public void Prepare(int sampleRate)
    {
        _sampleRate = sampleRate;
        UpdateParameters();
        _pitchShifter.Reset();
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (Bypass)
            return;

        float intensity = Math.Clamp(_params.Intensity, 0f, 1f);

        // Store original for dry/wet blend
        float[] dry = new float[count];
        for (int i = 0; i < count; i++)
        {
            dry[i] = buffer[offset + i];
        }

        // 1. Apply pitch shifting (down)
        _pitchShifter.Process(buffer, offset, count);

        // 2. Apply formant shifting (emphasis on lows)
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _formantShift1.Process(buffer[i]);
            buffer[i] = _formantShift2.Process(buffer[i]);
        }

        // 3. Apply bass boost (chest resonance)
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _bassBoost.Process(buffer[i]);
        }

        // 4. Apply presence cut (reduce nasality)
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _presenceCut.Process(buffer[i]);
        }

        // 5. Light compression for consistent level
        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];

            // RMS envelope follower
            float sampleSq = sample * sample;
            float coef = sampleSq > _rmsEnvelope ? _compAttackCoef : _compReleaseCoef;
            _rmsEnvelope = _rmsEnvelope * coef + sampleSq * (1f - coef);
            float rmsLevel = MathF.Sqrt(MathF.Max(_rmsEnvelope, 1e-10f));

            // Light compression (3:1 ratio, -18dB threshold)
            float inputDb = DSPHelpers.LinearToDb(rmsLevel);
            float threshold = -18f;
            float ratio = 3f;
            float gainReductionDb = 0f;

            if (inputDb > threshold)
            {
                gainReductionDb = (inputDb - threshold) * (1f - 1f / ratio);
            }

            float targetGain = DSPHelpers.DbToLinear(-gainReductionDb);
            _gainEnvelope = _gainEnvelope * _compReleaseCoef + targetGain * (1f - _compReleaseCoef);

            buffer[i] = sample * _gainEnvelope;
        }

        // 6. Blend with dry signal based on intensity
        for (int i = 0; i < count; i++)
        {
            buffer[offset + i] = DSPHelpers.Lerp(dry[i], buffer[offset + i], intensity);
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is DeepVoiceParameters p)
        {
            // Clamp parameters
            p.PitchSemitones = Math.Clamp(p.PitchSemitones, -12f, -1f);
            p.FormantShiftPercent = Math.Clamp(p.FormantShiftPercent, -20f, 0f);
            p.BassBoostDb = Math.Clamp(p.BassBoostDb, 0f, 8f);
            p.Intensity = Math.Clamp(p.Intensity, 0f, 1f);

            _params = p;

            if (_sampleRate > 0)
                UpdateParameters();
        }
    }

    public void Reset()
    {
        _pitchShifter.Reset();
        _formantShift1.Reset();
        _formantShift2.Reset();
        _bassBoost.Reset();
        _presenceCut.Reset();
        _rmsEnvelope = 0f;
        _gainEnvelope = 1f;
    }

    private void UpdateParameters()
    {
        // Set pitch shift (negative = down)
        _pitchShifter.SetPitchSemitones(_params.PitchSemitones);

        // Formant shift approximation
        // Negative shift = emphasize lower frequencies (larger vocal tract)
        float formantShiftRatio = 1f + (_params.FormantShiftPercent / 100f);

        // First formant shifter: Low-shelf to boost lower spectrum
        float formantFreq1 = 800f * formantShiftRatio;
        _formantShift1.Design(
            BiquadFilter.FilterType.LowShelf,
            formantFreq1,
            _sampleRate,
            q: 0.707,
            gainDb: 3f
        );

        // Second formant shifter: Slight reduction in upper mids
        float formantFreq2 = 2000f * formantShiftRatio;
        _formantShift2.Design(
            BiquadFilter.FilterType.Peaking,
            formantFreq2,
            _sampleRate,
            q: 1.5,
            gainDb: -1.5f
        );

        // Bass boost (chest resonance, proximity effect)
        _bassBoost.Design(
            BiquadFilter.FilterType.LowShelf,
            180f,
            _sampleRate,
            q: 0.707,
            gainDb: _params.BassBoostDb
        );

        // Presence cut (reduce nasality/harshness)
        _presenceCut.Design(
            BiquadFilter.FilterType.Peaking,
            3000f,
            _sampleRate,
            q: 1.5,
            gainDb: -2f
        );

        // Compressor time constants (slow for smooth, natural compression)
        _compAttackCoef = DSPHelpers.TimeToCoefficient(15f, _sampleRate);
        _compReleaseCoef = DSPHelpers.TimeToCoefficient(150f, _sampleRate);
    }
}

/// <summary>
/// DEEP VOICE ACOUSTICS:
///
/// Voice pitch is determined by:
/// 1. Vocal fold length/mass (longer/heavier = lower pitch)
/// 2. Subglottic pressure (air pressure from lungs)
/// 3. Vocal fold tension
///
/// Average fundamental frequencies:
/// - Adult male: 85-180 Hz
/// - Deep male: 85-110 Hz
/// - Adult female: 165-255 Hz
/// - Child: 250-400 Hz
///
/// Formants indicate vocal tract size:
/// - Larger vocal tract = lower formants
/// - Adult male vocal tract: ~17cm
/// - Adult female: ~14cm
/// - Child: ~10cm
///
/// Our effect simulates larger vocal tract by:
/// 1. Lowering pitch (vocal fold vibration)
/// 2. Lowering formants (vocal tract resonances)
/// 3. Boosting bass (chest/body resonance)
///
/// BROADCASTING VOICE TECHNIQUES:
///
/// Professional announcers use:
/// 1. PROXIMITY EFFECT: Speaking close to mic boosts bass
/// 2. CHEST VOICE: Lower register, more resonance
/// 3. COMPRESSION: Maintains consistent, powerful level
/// 4. EQ: Bass boost (warmth) + presence boost (clarity)
///
/// This effect recreates that sound digitally.
///
/// TYPICAL SETTINGS:
///
/// SUBTLE DEPTH (slight bass):
/// - Pitch: -2 semitones
/// - Formant: -5%
/// - Bass: +2dB
/// - Intensity: 70%
///
/// RADIO ANNOUNCER (authoritative):
/// - Pitch: -3 semitones
/// - Formant: -8%
/// - Bass: +4dB
/// - Intensity: 90%
///
/// MOVIE TRAILER VOICE:
/// - Pitch: -5 semitones
/// - Formant: -10%
/// - Bass: +6dB
/// - Intensity: 100%
///
/// BASS MONSTER (extreme):
/// - Pitch: -7 semitones
/// - Formant: -15%
/// - Bass: +8dB
/// - Intensity: 100%
///
/// AVOIDING "CHIPMUNK IN REVERSE":
/// Don't shift pitch too far down (-7+ semitones).
/// Result sounds unnatural, robotic.
/// Sweet spot: -3 to -5 semitones with formant compensation.
///
/// GENDER CONSIDERATIONS:
/// Female → Male voice:
/// - Pitch: -5 to -7 semitones
/// - Formant: -10 to -15%
/// - More aggressive than male deepening
///
/// Male → Deeper male:
/// - Pitch: -2 to -4 semitones
/// - Formant: -5 to -8%
/// - Subtle, natural enhancement
/// </summary>
