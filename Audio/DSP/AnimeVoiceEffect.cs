using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional Anime voice effect for TikTok-style content.
///
/// WHAT MAKES ANIME VOICE DISTINCTIVE:
/// 1. PITCH SHIFT UP: +5 semitones (bright, youthful tone)
/// 2. FORMANT SHIFT UP: +15% (smaller vocal tract, anime character)
/// 3. BRIGHTNESS BOOST: 3-8kHz (+4dB) for clarity and presence
/// 4. AIR BOOST: 8-12kHz (+3dB) for sparkle and sweetness
/// 5. COMPRESSION: 3:1 ratio for consistent anime-style delivery
///
/// WHY ANIME VOICE SOUNDS DISTINCTIVE:
/// Anime voice actors use specific vocal techniques:
/// - Higher pitch than natural speaking voice
/// - Exaggerated formant resonances (character portrayal)
/// - Bright, clear articulation with enhanced highs
/// - Consistent energy (compressed dynamics)
/// - Sweet, airy quality in upper frequencies
///
/// Our implementation recreates this digitally through DSP:
/// - Pitch shifting: Raises fundamental frequency (+5 semitones)
/// - Formant shifting: Simulates smaller vocal tract characteristics
/// - EQ shaping: Enhances brightness and air for anime aesthetic
/// - Compression: Maintains consistent, energetic delivery
///
/// IMPLEMENTATION DETAILS:
/// - Pitch shift: Time-domain SOLA algorithm (SimplePitchShifter)
/// - Formant shift: High-shelf and peaking filters (spectral warping)
/// - Brightness: Peaking EQ in presence range (3-8kHz)
/// - Air: High-shelf boost (8-12kHz)
/// - Compression: RMS envelope follower with 3:1 ratio
///
/// QUALITY SETTINGS:
/// SUBTLE (light anime):
/// - Pitch: +3 semitones
/// - Formant: +10%
/// - Brightness: +3dB @ 5kHz
/// - Air: +2dB @ 10kHz
///
/// MEDIUM (classic anime character):
/// - Pitch: +5 semitones
/// - Formant: +15%
/// - Brightness: +4dB @ 6kHz
/// - Air: +3dB @ 10kHz
///
/// EXTREME (kawaii/chibi):
/// - Pitch: +7 semitones
/// - Formant: +20%
/// - Brightness: +5dB @ 6kHz
/// - Air: +4dB @ 10kHz
/// </summary>
public class AnimeVoiceEffect : IAudioEffect
{
    private AnimeParameters _params;
    private int _sampleRate;

    // Pitch shifter
    private SimplePitchShifter _pitchShifter;

    // High-pass filter (remove low-frequency artifacts from pitch shift)
    private BiquadFilter _highPassFilter;

    // Formant shifting (spectral warping)
    private BiquadFilter _formantShift1;
    private BiquadFilter _formantShift2;

    // Brightness boost (presence range 3-8kHz)
    private BiquadFilter _brightnessBoost;

    // Air boost (high-shelf 8-12kHz)
    private BiquadFilter _airBoost;

    // Compression
    private float _rmsEnvelope;
    private float _gainEnvelope;
    private float _compAttackCoef;
    private float _compReleaseCoef;

    public bool Bypass { get; set; }

    public class AnimeParameters
    {
        /// <summary>Pitch shift in semitones (+3 to +7, typical +5)</summary>
        public float PitchSemitones { get; set; } = 5f;

        /// <summary>Formant shift percentage (0-25%, typical 15%)</summary>
        public float FormantShiftPercent { get; set; } = 15f;

        /// <summary>Brightness boost in dB (0-6, typical 4)</summary>
        public float BrightnessDb { get; set; } = 4f;

        /// <summary>Air boost in dB (0-5, typical 3)</summary>
        public float AirDb { get; set; } = 3f;

        /// <summary>Effect intensity (0-1, affects overall blend)</summary>
        public float Intensity { get; set; } = 1.0f;
    }

    public AnimeVoiceEffect()
    {
        _params = new AnimeParameters();
        _pitchShifter = new SimplePitchShifter();
        _highPassFilter = new BiquadFilter();
        _formantShift1 = new BiquadFilter();
        _formantShift2 = new BiquadFilter();
        _brightnessBoost = new BiquadFilter();
        _airBoost = new BiquadFilter();
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

        // 1. Apply pitch shifting (+5 semitones for anime tone)
        _pitchShifter.Process(buffer, offset, count);

        // 2. Apply high-pass filter (remove unnatural low-frequency artifacts)
        // Industry standard: 120-150 Hz for anime character voice
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _highPassFilter.Process(buffer[i]);
        }

        // 3. Apply formant shifting (simulate smaller vocal tract)
        // High-shelf filters emphasize upper formants
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _formantShift1.Process(buffer[i]);
            buffer[i] = _formantShift2.Process(buffer[i]);
        }

        // 3. Apply brightness boost (presence range 3-8kHz)
        // This enhances clarity and anime-style articulation
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _brightnessBoost.Process(buffer[i]);
        }

        // 4. Apply air boost (high-shelf 8-12kHz)
        // This adds the sweet, airy quality characteristic of anime voices
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _airBoost.Process(buffer[i]);
        }

        // 5. Compression for consistent, energetic delivery (3:1 ratio)
        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];

            // RMS envelope follower
            float sampleSq = sample * sample;
            float coef = sampleSq > _rmsEnvelope ? _compAttackCoef : _compReleaseCoef;
            _rmsEnvelope = _rmsEnvelope * coef + sampleSq * (1f - coef);
            float rmsLevel = MathF.Sqrt(MathF.Max(_rmsEnvelope, 1e-10f));

            // Compression (3:1 ratio, -18dB threshold for anime energy)
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
        if (parameters is AnimeParameters p)
        {
            // Clamp parameters
            p.PitchSemitones = Math.Clamp(p.PitchSemitones, 2f, 10f);
            p.FormantShiftPercent = Math.Clamp(p.FormantShiftPercent, 0f, 30f);
            p.BrightnessDb = Math.Clamp(p.BrightnessDb, 0f, 8f);
            p.AirDb = Math.Clamp(p.AirDb, 0f, 6f);
            p.Intensity = Math.Clamp(p.Intensity, 0f, 1f);

            _params = p;

            if (_sampleRate > 0)
                UpdateParameters();
        }
    }

    public void Reset()
    {
        _pitchShifter.Reset();
        _highPassFilter.Reset();
        _formantShift1.Reset();
        _formantShift2.Reset();
        _brightnessBoost.Reset();
        _airBoost.Reset();
        _rmsEnvelope = 0f;
        _gainEnvelope = 1f;
    }

    private void UpdateParameters()
    {
        // Set pitch shift (+5 semitones for anime character)
        _pitchShifter.SetPitchSemitones(_params.PitchSemitones);

        // High-pass filter to remove unnatural low frequencies from pitch shift
        // Industry standard: 120-150 Hz for anime character voice
        _highPassFilter.Design(
            BiquadFilter.FilterType.HighPass,
            135f, // 120-150 Hz range
            _sampleRate,
            q: 0.707 // Butterworth response
        );

        // Formant shift approximation using high-shelf filters
        // Shifting formants up = making vocal tract appear smaller (anime character)
        float formantShiftRatio = 1f + (_params.FormantShiftPercent / 100f);

        // First formant shifter: High-shelf to boost upper spectrum
        float formantFreq1 = 1200f * formantShiftRatio;
        _formantShift1.Design(
            BiquadFilter.FilterType.HighShelf,
            formantFreq1,
            _sampleRate,
            q: 0.707,
            gainDb: 2.5f
        );

        // Second formant shifter: Peaking filter for presence
        float formantFreq2 = 2800f * formantShiftRatio;
        _formantShift2.Design(
            BiquadFilter.FilterType.Peaking,
            formantFreq2,
            _sampleRate,
            q: 1.4,
            gainDb: 3f
        );

        // Brightness boost (presence range 3-8kHz)
        // Peaking filter centered at 6kHz for anime clarity
        _brightnessBoost.Design(
            BiquadFilter.FilterType.Peaking,
            6000f,
            _sampleRate,
            q: 1.2,
            gainDb: _params.BrightnessDb
        );

        // Air boost (high-shelf 8-12kHz)
        // High-shelf at 10kHz for sweet, airy quality
        _airBoost.Design(
            BiquadFilter.FilterType.HighShelf,
            10000f,
            _sampleRate,
            q: 0.707,
            gainDb: _params.AirDb
        );

        // Compressor time constants
        _compAttackCoef = DSPHelpers.TimeToCoefficient(8f, _sampleRate);
        _compReleaseCoef = DSPHelpers.TimeToCoefficient(100f, _sampleRate);
    }
}

/// <summary>
/// ANIME VOICE SCIENCE:
///
/// What makes anime voices distinctive:
/// 1. VOCAL TECHNIQUE:
///    - Voice actors use "character voice" techniques
///    - Higher pitch than natural speaking
///    - Exaggerated emotional expression
///    - Clear, bright articulation
///
/// 2. FREQUENCY CHARACTERISTICS:
///    - Fundamental frequency: +3 to +8 semitones above normal
///    - Formant peaks: Shifted up 10-20% (smaller vocal tract simulation)
///    - Presence boost: 3-8kHz for clarity and "anime brightness"
///    - Air frequencies: 8-12kHz for sweet, airy quality
///
/// 3. DYNAMIC CHARACTERISTICS:
///    - Consistent energy (compressed dynamics)
///    - Fast attack (energetic delivery)
///    - Minimal dynamic range (cartoon-like consistency)
///
/// OUR IMPLEMENTATION:
/// We use a combination of:
/// - Pitch shifting: Raises fundamental frequency
/// - Formant shifting: Simulates character vocal tract
/// - Presence boost: Peaking EQ at 6kHz
/// - Air boost: High-shelf at 10kHz
/// - Compression: 3:1 ratio for anime consistency
///
/// WHY THIS WORKS:
/// - Pitch shift creates the "character" base tone
/// - Formant shift adds the anime vocal tract quality
/// - Brightness boost gives anime-style clarity
/// - Air boost adds sweetness and sparkle
/// - Compression maintains consistent cartoon energy
///
/// COMPARISON TO HELIUM EFFECT:
/// - Anime: +5 semitones pitch, focused on character portrayal
/// - Helium: +5 to +8 semitones, more extreme squeaky effect
/// - Anime: More controlled, character-focused EQ
/// - Helium: More extreme brightness and formant shift
///
/// TYPICAL ANIME CHARACTER PROFILES:
///
/// SHOUNEN PROTAGONIST (energetic, determined):
/// - Pitch: +4 semitones
/// - Formant: +12%
/// - Brightness: +3dB
/// - Compression: 3:1
///
/// KAWAII CHARACTER (cute, sweet):
/// - Pitch: +6 semitones
/// - Formant: +18%
/// - Brightness: +5dB
/// - Air: +4dB
/// - Compression: 4:1
///
/// TSUNDERE CHARACTER (mixed emotions):
/// - Pitch: +4 semitones
/// - Formant: +10%
/// - Brightness: +4dB
/// - Compression: 3:1
///
/// CHIBI MODE (super-deformed):
/// - Pitch: +7 semitones
/// - Formant: +20%
/// - Brightness: +5dB
/// - Air: +4dB
/// - Compression: 4:1
/// </summary>
