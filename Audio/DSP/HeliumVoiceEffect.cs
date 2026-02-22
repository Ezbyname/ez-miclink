using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional Helium/Chipmunk voice effect.
///
/// WHAT MAKES HELIUM VOICE REALISTIC:
/// 1. PITCH SHIFT UP: +4 to +8 semitones (fundamental frequency increases)
/// 2. FORMANT SHIFT UP: +10% to +20% (vocal tract resonances shift)
/// 3. EQ BOOST: 3-8kHz (add brightness, clarity)
/// 4. COMPRESSION: Light (maintain consistent level)
///
/// WHY HELIUM SOUNDS HIGH-PITCHED:
/// Real helium changes speed of sound in vocal tract (from 343 m/s to ~900 m/s).
/// This shifts formant frequencies up by ~2.5x while pitch stays same.
/// But for fun effect, we shift BOTH pitch and formants up.
///
/// FORMANT SHIFTING:
/// Formants are vocal tract resonances (vocal color/timbre).
/// Typical formants for vowel "ah":
/// - F1: 700 Hz (tongue height)
/// - F2: 1200 Hz (tongue frontness)
/// - F3: 2500 Hz (lip rounding)
///
/// Shifting formants = moving EQ peaks to higher frequencies.
/// This simulates smaller vocal tract (child, helium, chipmunk).
///
/// IMPLEMENTATION:
/// We use spectral envelope warping via all-pass filters to shift formants.
/// Simpler than full vocoder but effective for real-time.
/// - Pitch shift: Time-domain SOLA
/// - Formant shift: All-pass filter cascade (frequency warping)
/// - EQ: Biquad high-frequency boost
/// - Compression: Built-in RMS compressor
///
/// QUALITY SETTINGS:
/// SUBTLE (mild helium):
/// - Pitch: +3 semitones
/// - Formant: +8%
/// - EQ: +2dB @ 5kHz
///
/// MEDIUM (classic chipmunk):
/// - Pitch: +5 semitones
/// - Formant: +15%
/// - EQ: +4dB @ 6kHz
///
/// EXTREME (squeaky):
/// - Pitch: +8 semitones
/// - Formant: +20%
/// - EQ: +6dB @ 7kHz
/// </summary>
public class HeliumVoiceEffect : IAudioEffect
{
    private HeliumParameters _params;
    private int _sampleRate;

    // Pitch shifter
    private SimplePitchShifter _pitchShifter;

    // High-pass filter (remove low-frequency artifacts from pitch shift)
    private BiquadFilter _highPassFilter;

    // Formant shifting (simplified via all-pass filters)
    // Note: True formant shifting requires LPC analysis.
    // We use spectral tilt + filtering as approximation.
    private BiquadFilter _formantShift1;
    private BiquadFilter _formantShift2;

    // Brightness EQ
    private BiquadFilter _brightnessBoost;

    // Light compression
    private float _rmsEnvelope;
    private float _gainEnvelope;
    private float _compAttackCoef;
    private float _compReleaseCoef;

    public bool Bypass { get; set; }

    public class HeliumParameters
    {
        /// <summary>Pitch shift in semitones (+3 to +8, typical +5)</summary>
        public float PitchSemitones { get; set; } = 5f;

        /// <summary>Formant shift percentage (0-30%, typical 15%)</summary>
        public float FormantShiftPercent { get; set; } = 15f;

        /// <summary>Brightness boost in dB (0-8, typical 4)</summary>
        public float BrightnessDb { get; set; } = 4f;

        /// <summary>Effect intensity (0-1, affects overall blend)</summary>
        public float Intensity { get; set; } = 1.0f;
    }

    public HeliumVoiceEffect()
    {
        _params = new HeliumParameters();
        _pitchShifter = new SimplePitchShifter();
        _highPassFilter = new BiquadFilter();
        _formantShift1 = new BiquadFilter();
        _formantShift2 = new BiquadFilter();
        _brightnessBoost = new BiquadFilter();
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

        // 1. Apply pitch shifting
        _pitchShifter.Process(buffer, offset, count);

        // 2. Apply high-pass filter (remove unnatural low-frequency artifacts)
        // Industry standard: 150-200 Hz for chipmunk/helium effect
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _highPassFilter.Process(buffer[i]);
        }

        // 3. Apply formant shifting (via spectral warping filters)
        // High-shelf filters simulate formant upshift
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _formantShift1.Process(buffer[i]);
            buffer[i] = _formantShift2.Process(buffer[i]);
        }

        // 3. Apply brightness boost
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = _brightnessBoost.Process(buffer[i]);
        }

        // 4. Light compression to maintain consistent level
        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];

            // RMS envelope follower
            float sampleSq = sample * sample;
            float coef = sampleSq > _rmsEnvelope ? _compAttackCoef : _compReleaseCoef;
            _rmsEnvelope = _rmsEnvelope * coef + sampleSq * (1f - coef);
            float rmsLevel = MathF.Sqrt(MathF.Max(_rmsEnvelope, 1e-10f));

            // Light compression (3:1 ratio, -15dB threshold)
            float inputDb = DSPHelpers.LinearToDb(rmsLevel);
            float threshold = -15f;
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

        // 5. Blend with dry signal based on intensity
        for (int i = 0; i < count; i++)
        {
            buffer[offset + i] = DSPHelpers.Lerp(dry[i], buffer[offset + i], intensity);
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is HeliumParameters p)
        {
            // Clamp parameters
            p.PitchSemitones = Math.Clamp(p.PitchSemitones, 2f, 12f);
            p.FormantShiftPercent = Math.Clamp(p.FormantShiftPercent, 0f, 30f);
            p.BrightnessDb = Math.Clamp(p.BrightnessDb, 0f, 8f);
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
        _rmsEnvelope = 0f;
        _gainEnvelope = 1f;
    }

    private void UpdateParameters()
    {
        // Set pitch shift
        _pitchShifter.SetPitchSemitones(_params.PitchSemitones);

        // High-pass filter to remove unnatural low frequencies from pitch shift
        // Industry standard: 150-200 Hz for helium/chipmunk effect
        _highPassFilter.Design(
            BiquadFilter.FilterType.HighPass,
            175f, // 150-200 Hz range
            _sampleRate,
            q: 0.707 // Butterworth response
        );

        // Formant shift approximation using high-shelf filters
        // Shifting formants up = making vocal tract appear smaller
        // We simulate this by emphasizing higher frequencies
        float formantShiftRatio = 1f + (_params.FormantShiftPercent / 100f);

        // First formant shifter: High-shelf to boost upper spectrum
        float formantFreq1 = 1000f * formantShiftRatio;
        _formantShift1.Design(
            BiquadFilter.FilterType.HighShelf,
            formantFreq1,
            _sampleRate,
            q: 0.707,
            gainDb: 2f
        );

        // Second formant shifter: Peaking filter for presence
        float formantFreq2 = 2500f * formantShiftRatio;
        _formantShift2.Design(
            BiquadFilter.FilterType.Peaking,
            formantFreq2,
            _sampleRate,
            q: 1.5,
            gainDb: 3f
        );

        // Brightness boost in high frequencies
        _brightnessBoost.Design(
            BiquadFilter.FilterType.HighShelf,
            5000f,
            _sampleRate,
            q: 0.707,
            gainDb: _params.BrightnessDb
        );

        // Compressor time constants
        _compAttackCoef = DSPHelpers.TimeToCoefficient(10f, _sampleRate);
        _compReleaseCoef = DSPHelpers.TimeToCoefficient(100f, _sampleRate);
    }
}

/// <summary>
/// HELIUM VOICE SCIENCE:
///
/// Real helium breathing:
/// - Doesn't change vocal cord vibration (pitch stays same)
/// - Changes speed of sound in vocal tract (343 m/s → 965 m/s)
/// - Formants shift up by factor of 2.8x
/// - Voice sounds high-pitched due to formant shift alone
///
/// Our "fun" helium effect:
/// - Shifts BOTH pitch and formants up
/// - Creates exaggerated cartoon character effect
/// - Not realistic helium, but fun and recognizable
///
/// FORMANT SHIFTING METHODS:
///
/// METHOD 1: LPC Vocoder (professional):
/// - Analyze: Extract formants via Linear Predictive Coding
/// - Modify: Shift formant peaks in frequency
/// - Synthesize: Reconstruct with new formants
/// - Quality: Excellent, natural
/// - CPU: High, complex
///
/// METHOD 2: Spectral Envelope Warping (implemented):
/// - Use all-pass/shelf filters to warp frequency response
/// - Approximate formant shifts via EQ
/// - Quality: Good for moderate shifts
/// - CPU: Low, simple
///
/// METHOD 3: Phase Vocoder with Envelope Scaling:
/// - FFT analysis
/// - Scale spectral envelope independently from pitch
/// - IFFT synthesis
/// - Quality: Very good
/// - CPU: High
///
/// We use METHOD 2 for real-time efficiency and simplicity.
/// It's not perfect formant shifting but sounds good for fun effects.
///
/// TYPICAL SETTINGS:
///
/// SUBTLE HELIUM (party trick):
/// - Pitch: +3 semitones
/// - Formant: +8%
/// - Brightness: +2dB
/// - Intensity: 80%
///
/// CHIPMUNK (Alvin):
/// - Pitch: +5 semitones
/// - Formant: +15%
/// - Brightness: +4dB
/// - Intensity: 100%
///
/// EXTREME SQUEAKY:
/// - Pitch: +7 semitones
/// - Formant: +20%
/// - Brightness: +6dB
/// - Intensity: 100%
///
/// CARTOON MOUSE:
/// - Pitch: +8 semitones
/// - Formant: +25%
/// - Brightness: +5dB
/// - Intensity: 100%
/// </summary>
