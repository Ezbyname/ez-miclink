using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Realistic megaphone/loudspeaker simulation.
///
/// WHAT MAKES A MEGAPHONE SOUND:
/// 1. LIMITED BANDWIDTH: Real megaphones only reproduce ~300Hz-3kHz
/// 2. DISTORTION: Overdriven speakers create harmonic distortion
/// 3. COMPRESSION: Physical limits cause natural compression
/// 4. NASAL TONE: Narrow bandpass creates "honky" character
///
/// IMPLEMENTATION STRATEGY:
/// 1. High-pass filter at 400Hz (remove bass rumble)
/// 2. Low-pass filter at 3kHz (remove clarity/air)
/// 3. Soft clipping (simulate speaker overdrive)
/// 4. Light compression (simulate speaker physical limits)
/// 5. Boost midrange slightly (compensate for bandwidth loss)
///
/// FREQUENCY RESPONSE:
/// Real megaphone frequency response:
///   20Hz     |-----[flat]-----| 400Hz
///   400Hz    |    /‾‾‾‾‾\     | 1.5kHz  (bandpass peak)
///   1.5kHz   |   /       \    | 3kHz
///   3kHz     |--[rolloff]-----| 20kHz
///
/// The narrow bandwidth creates distinctive "telephone" quality.
///
/// DISTORTION CHARACTER:
/// Soft clipping creates odd harmonics (3rd, 5th, 7th).
/// This sounds more natural than hard clipping (all harmonics).
/// We use tanh() approximation for smooth, musical distortion.
/// </summary>
public class MegaphoneEffect : IAudioEffect
{
    private MegaphoneParameters _params;
    private int _sampleRate;

    // Bandpass filters
    private BiquadFilter _highPass;
    private BiquadFilter _lowPass;
    private BiquadFilter _midBoost;

    // Pre-emphasis for presence
    private BiquadFilter _presence;

    public bool Bypass { get; set; }

    public class MegaphoneParameters
    {
        /// <summary>Low cutoff frequency in Hz (200-600, typical 400)</summary>
        public float LowCutoffHz { get; set; } = 400f;

        /// <summary>High cutoff frequency in Hz (2000-4000, typical 3000)</summary>
        public float HighCutoffHz { get; set; } = 3000f;

        /// <summary>Distortion amount (0-1, typical 0.3-0.7)</summary>
        public float Distortion { get; set; } = 0.5f;

        /// <summary>Midrange boost in dB (0-6, typical 3)</summary>
        public float MidBoostDb { get; set; } = 3f;
    }

    public MegaphoneEffect()
    {
        _params = new MegaphoneParameters();
        _highPass = new BiquadFilter();
        _lowPass = new BiquadFilter();
        _midBoost = new BiquadFilter();
        _presence = new BiquadFilter();
    }

    public void Prepare(int sampleRate)
    {
        _sampleRate = sampleRate;
        UpdateFilters();
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (Bypass)
            return;

        float distortion = _params.Distortion;

        // Pre-gain to drive distortion
        float preGain = 1f + distortion * 2f;

        // Post-gain to compensate
        float postGain = 1f / preGain;

        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];

            // 1. Apply bandpass filtering
            sample = _highPass.Process(sample);
            sample = _lowPass.Process(sample);

            // 2. Boost midrange for presence
            sample = _midBoost.Process(sample);

            // 3. Apply pre-gain
            sample *= preGain;

            // 4. Soft clipping (simulates speaker overdrive)
            // Blend between clean and distorted based on distortion amount
            float clean = sample;
            float clipped = DSPHelpers.SoftClip(sample * 0.7f); // 0.7 to prevent too much squashing
            sample = DSPHelpers.Lerp(clean, clipped, distortion);

            // 5. Apply post-gain
            sample *= postGain;

            // 6. Final safety clipping
            sample = DSPHelpers.HardClip(sample, 0.95f);

            buffer[i] = sample;
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is MegaphoneParameters p)
        {
            // Clamp parameters
            p.LowCutoffHz = Math.Clamp(p.LowCutoffHz, 200f, 800f);
            p.HighCutoffHz = Math.Clamp(p.HighCutoffHz, 1500f, 5000f);
            p.Distortion = Math.Clamp(p.Distortion, 0f, 1f);
            p.MidBoostDb = Math.Clamp(p.MidBoostDb, 0f, 6f);

            _params = p;

            if (_sampleRate > 0)
                UpdateFilters();
        }
    }

    public void Reset()
    {
        _highPass.Reset();
        _lowPass.Reset();
        _midBoost.Reset();
        _presence.Reset();
    }

    private void UpdateFilters()
    {
        // High-pass: Remove bass below cutoff
        _highPass.Design(
            BiquadFilter.FilterType.HighPass,
            _params.LowCutoffHz,
            _sampleRate,
            q: 0.707 // Butterworth response
        );

        // Low-pass: Remove treble above cutoff
        _lowPass.Design(
            BiquadFilter.FilterType.LowPass,
            _params.HighCutoffHz,
            _sampleRate,
            q: 0.707
        );

        // Midrange boost: Add presence
        // Boost at geometric mean of cutoff frequencies
        float midFreq = MathF.Sqrt(_params.LowCutoffHz * _params.HighCutoffHz);
        _midBoost.Design(
            BiquadFilter.FilterType.Peaking,
            midFreq,
            _sampleRate,
            q: 1.5, // Moderate Q for natural boost
            gainDb: _params.MidBoostDb
        );
    }
}

/// <summary>
/// MEGAPHONE VARIATIONS:
///
/// CLASSIC MEGAPHONE:
/// - LowCutoff: 400Hz
/// - HighCutoff: 3000Hz
/// - Distortion: 0.5
/// - MidBoost: 3dB
///
/// TELEPHONE:
/// - LowCutoff: 300Hz
/// - HighCutoff: 3400Hz  (actual phone bandwidth)
/// - Distortion: 0.2
/// - MidBoost: 2dB
///
/// WALKIE-TALKIE:
/// - LowCutoff: 500Hz
/// - HighCutoff: 2500Hz
/// - Distortion: 0.7
/// - MidBoost: 4dB
///
/// INTERCOM:
/// - LowCutoff: 350Hz
/// - HighCutoff: 4000Hz
/// - Distortion: 0.3
/// - MidBoost: 2dB
///
/// WHY THIS SOUNDS REALISTIC:
///
/// 1. BANDWIDTH LIMITING:
/// Human voice fundamental: 85-255Hz (male), 165-255Hz (female)
/// Formants (resonances): 500-3500Hz
/// By keeping only 400-3000Hz, we preserve intelligibility
/// but remove naturalness (no bass warmth, no air/sibilance)
///
/// 2. DISTORTION:
/// Real speakers distort when overdriven.
/// Soft clipping adds odd harmonics that sound more natural
/// than digital hard clipping.
///
/// 3. MIDRANGE BOOST:
/// Compensates for perceived loudness loss from bandwidth limiting.
/// 1-2kHz region is most sensitive for human hearing (equal loudness curves).
///
/// 4. NO REVERB/DELAY:
/// Real megaphones are close-mic'd, minimal spatial effects.
/// This keeps sound dry and direct.
///
/// ALTERNATIVE IMPLEMENTATION (more complex):
/// For even more realism, could add:
/// - Resonant peak (formant) around 1-2kHz
/// - Subtle amplitude modulation (simulate mechanical vibration)
/// - DC offset/bias (simulate transformer coupling)
/// - Bit crushing (simulate low-fi electronics)
///
/// Current implementation balances realism with CPU efficiency.
/// </summary>
