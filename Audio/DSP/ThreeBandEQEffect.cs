using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional 3-Band Parametric EQ using biquad filters.
///
/// DESIGN:
/// - Low shelf: Boost/cut low frequencies
/// - Mid peak: Parametric bell filter for vocal presence
/// - High shelf: Boost/cut high frequencies
///
/// FREQUENCY RANGES:
/// - Low: 20-250 Hz (bass, fundamental)
/// - Mid: 500-4000 Hz (presence, clarity, intelligibility)
/// - High: 4000-20000 Hz (air, brilliance, sibilance)
///
/// WHY SHELVING FILTERS:
/// Shelves are more musical than highpass/lowpass for EQ.
/// They provide smooth, gradual boost/cut instead of complete removal.
///
/// TYPICAL VOICE EQ:
/// - Low shelf at 100Hz: -3dB (reduce rumble/pops)
/// - Mid peak at 2-3kHz: +2 to +4dB (increase presence and clarity)
/// - High shelf at 8kHz: +1 to +3dB (add air and sparkle)
/// </summary>
public class ThreeBandEQEffect : IAudioEffect
{
    private ThreeBandEQParameters _params;
    private int _sampleRate;

    // Three biquad filters
    private BiquadFilter _lowShelf;
    private BiquadFilter _midPeak;
    private BiquadFilter _highShelf;

    public bool Bypass { get; set; }

    public class ThreeBandEQParameters
    {
        // Low shelf
        public float LowFreq { get; set; } = 100f;
        public float LowGainDb { get; set; } = 0f;

        // Mid peak (parametric)
        public float MidFreq { get; set; } = 2500f;
        public float MidGainDb { get; set; } = 0f;
        public float MidQ { get; set; } = 1.0f; // Bandwidth: lower = wider

        // High shelf
        public float HighFreq { get; set; } = 8000f;
        public float HighGainDb { get; set; } = 0f;
    }

    public ThreeBandEQEffect()
    {
        _params = new ThreeBandEQParameters();
        _lowShelf = new BiquadFilter();
        _midPeak = new BiquadFilter();
        _highShelf = new BiquadFilter();
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

        // Process through filter chain: Low -> Mid -> High
        // Order doesn't matter mathematically, but this is conventional
        _lowShelf.ProcessBuffer(buffer, offset, count);
        _midPeak.ProcessBuffer(buffer, offset, count);
        _highShelf.ProcessBuffer(buffer, offset, count);
    }

    public void SetParameters(object parameters)
    {
        if (parameters is ThreeBandEQParameters p)
        {
            // Clamp parameters to safe ranges
            p.LowFreq = Math.Clamp(p.LowFreq, 20f, 500f);
            p.LowGainDb = Math.Clamp(p.LowGainDb, -18f, 18f);

            p.MidFreq = Math.Clamp(p.MidFreq, 200f, 8000f);
            p.MidGainDb = Math.Clamp(p.MidGainDb, -18f, 18f);
            p.MidQ = Math.Clamp(p.MidQ, 0.3f, 10f);

            p.HighFreq = Math.Clamp(p.HighFreq, 2000f, 20000f);
            p.HighGainDb = Math.Clamp(p.HighGainDb, -18f, 18f);

            _params = p;

            if (_sampleRate > 0)
                UpdateFilters();
        }
    }

    public void Reset()
    {
        _lowShelf.Reset();
        _midPeak.Reset();
        _highShelf.Reset();
    }

    private void UpdateFilters()
    {
        // Design each filter
        // Q = 0.707 for shelves (Butterworth response)
        _lowShelf.Design(
            BiquadFilter.FilterType.LowShelf,
            _params.LowFreq,
            _sampleRate,
            q: 0.707,
            gainDb: _params.LowGainDb
        );

        _midPeak.Design(
            BiquadFilter.FilterType.Peaking,
            _params.MidFreq,
            _sampleRate,
            q: _params.MidQ,
            gainDb: _params.MidGainDb
        );

        _highShelf.Design(
            BiquadFilter.FilterType.HighShelf,
            _params.HighFreq,
            _sampleRate,
            q: 0.707,
            gainDb: _params.HighGainDb
        );
    }
}

/// <summary>
/// EQ PRESETS FOR VOICE:
///
/// 1. PODCAST (warm, intimate):
///    Low: 80Hz, -3dB (reduce rumble)
///    Mid: 3000Hz, +3dB, Q=1.5 (presence boost)
///    High: 10000Hz, +2dB (add air)
///
/// 2. STAGE MC (loud, clear):
///    Low: 120Hz, -6dB (cut bass for clarity)
///    Mid: 2500Hz, +5dB, Q=2.0 (cut through mix)
///    High: 8000Hz, +3dB (brightness)
///
/// 3. KARAOKE (smooth, polished):
///    Low: 100Hz, +2dB (warmth)
///    Mid: 2000Hz, +2dB, Q=1.0 (gentle presence)
///    High: 12000Hz, +1dB (subtle sparkle)
///
/// 4. RADIO ANNOUNCER (authoritative):
///    Low: 100Hz, +3dB (chest resonance)
///    Mid: 3500Hz, +4dB, Q=1.5 (clarity)
///    High: 10000Hz, 0dB (natural)
///
/// Q VALUES EXPLAINED:
/// - Q = 0.5: Very wide, gentle slope (2 octaves)
/// - Q = 1.0: Standard width (1 octave)
/// - Q = 2.0: Narrow, focused (0.5 octaves)
/// - Q = 5.0: Very narrow, surgical cut/boost
///
/// For voice, Q between 1.0 and 2.0 is most musical.
/// Lower Q = more natural, higher Q = more specific/corrective.
/// </summary>
