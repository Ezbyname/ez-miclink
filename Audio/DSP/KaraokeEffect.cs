using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Karaoke vocal effect combining reverb, compression, and presence boost.
///
/// KARAOKE EFFECT GOALS:
/// 1. Add space/ambience (reverb) - makes voice sound fuller
/// 2. Smooth dynamics (compression) - maintains consistent level
/// 3. Boost presence (EQ) - voice cuts through music
/// 4. Maintain intelligibility - not drowning in reverb
///
/// REVERB THEORY:
/// Natural room acoustics create thousands of echoes that blend into reverb.
/// We simulate this using:
/// - Comb filters: Create early reflections
/// - Allpass filters: Create dense late reflections
/// - Damping: High frequencies decay faster (like real rooms)
///
/// SCHROEDER REVERB STRUCTURE:
/// Input → [Parallel Comb Filters] → [Series Allpass Filters] → Output
///
/// Why this structure?
/// - Comb filters create resonances (room modes)
/// - Allpass filters diffuse sound (scatter reflections)
/// - Parallel combs = early reflections
/// - Series allpass = late reverb density
///
/// COMB FILTER:
/// Adds delayed copy of signal back to itself with feedback.
/// Creates peaks at frequencies = sampleRate / delay
/// Result: metallic ringing (room resonance)
///
/// ALLPASS FILTER:
/// Scatters reflections without coloring frequency response.
/// Maintains all frequencies (flat response) but changes phase.
/// Result: dense, diffuse reverb tail
///
/// KARAOKE-SPECIFIC TUNING:
/// - SHORT reverb time (0.5-1.2 seconds) - keeps clarity
/// - MODERATE room size - not stadium, not closet
/// - HIGH damping - remove harsh high-frequency reverb
/// - 30-40% wet mix - enough space, still clear
/// - Slight compression - smooth out volume inconsistencies
/// - Presence boost at 3-5kHz - voice cuts through
///
/// REVERB PARAMETERS:
/// - Room size: Controls delay times (larger = longer reverb)
/// - Decay time: How long reverb lasts
/// - Damping: High-frequency absorption
/// - Wet/Dry: Balance between original and reverb
/// </summary>
public class KaraokeEffect : IAudioEffect
{
    private KaraokeParameters _params;
    private int _sampleRate;

    // Reverb components (Schroeder structure)
    private CombFilter[] _combFilters;
    private AllpassFilter[] _allpassFilters;

    // Built-in compression
    private float _rmsEnvelope;
    private float _gainEnvelope;
    private float _compressorAttackCoef;
    private float _compressorReleaseCoef;

    // High-pass filter (clean up low end before reverb)
    private BiquadFilter _highPassFilter;

    // Presence EQ
    private BiquadFilter _presenceBoost;

    public bool Bypass { get; set; }

    public class KaraokeParameters
    {
        /// <summary>Room size (0.3=small, 0.7=medium, 1.0=large)</summary>
        public float RoomSize { get; set; } = 0.7f;

        /// <summary>Reverb decay time (0.3 to 4.5 seconds, typical 0.8 for karaoke, 2.5-4.5 for stadium)</summary>
        public float DecayTime { get; set; } = 0.8f;

        /// <summary>High-frequency damping (0-1, typical 0.6)</summary>
        public float Damping { get; set; } = 0.6f;

        /// <summary>Wet/dry mix (0=dry, 1=wet, typical 0.35)</summary>
        public float Mix { get; set; } = 0.35f;

        /// <summary>Built-in compression threshold in dB (-30 to -10, typical -18)</summary>
        public float CompressionThreshold { get; set; } = -18f;

        /// <summary>Presence boost in dB (0-6, typical 3)</summary>
        public float PresenceBoost { get; set; } = 3f;
    }

    public KaraokeEffect()
    {
        _params = new KaraokeParameters();
        _combFilters = Array.Empty<CombFilter>();
        _allpassFilters = Array.Empty<AllpassFilter>();
        _highPassFilter = new BiquadFilter();
        _presenceBoost = new BiquadFilter();
        _rmsEnvelope = 0f;
        _gainEnvelope = 1f;
    }

    public void Prepare(int sampleRate)
    {
        _sampleRate = sampleRate;
        InitializeReverb();
        UpdateCompressorCoefficients();
        UpdateHighPassFilter();
        UpdatePresenceEQ();
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (Bypass)
            return;

        float mix = Math.Clamp(_params.Mix, 0f, 1f);
        float dryGain = 1f - mix;
        float wetGain = mix;

        // Compression parameters
        float threshold = _params.CompressionThreshold;
        float ratio = 3f; // Fixed 3:1 ratio for karaoke

        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];

            // 0. Apply high-pass filter (clean up low end before reverb)
            // Industry standard: 80-100 Hz for karaoke/vocal processing
            float filtered = _highPassFilter.Process(sample);

            // 1. Apply presence boost to input (helps voice cut through)
            float boosted = _presenceBoost.Process(filtered);

            // 2. Light compression for consistent level
            // RMS envelope follower
            float sampleSquared = boosted * boosted;
            float coef = sampleSquared > _rmsEnvelope ? _compressorAttackCoef : _compressorReleaseCoef;
            _rmsEnvelope = _rmsEnvelope * coef + sampleSquared * (1f - coef);
            float rmsLevel = MathF.Sqrt(MathF.Max(_rmsEnvelope, 1e-10f));

            // Calculate compression
            float inputDb = DSPHelpers.LinearToDb(rmsLevel);
            float gainReductionDb = 0f;
            if (inputDb > threshold)
            {
                gainReductionDb = (inputDb - threshold) * (1f - 1f / ratio);
            }
            float targetGain = DSPHelpers.DbToLinear(-gainReductionDb);
            _gainEnvelope = _gainEnvelope * _compressorReleaseCoef + targetGain * (1f - _compressorReleaseCoef);

            float compressed = boosted * _gainEnvelope;

            // 3. Generate reverb
            // Pass through parallel comb filters (early reflections)
            float combSum = 0f;
            for (int c = 0; c < _combFilters.Length; c++)
            {
                combSum += _combFilters[c].Process(compressed);
            }
            float combOutput = combSum / _combFilters.Length;

            // Pass through series allpass filters (dense reverb)
            float reverbOutput = combOutput;
            for (int a = 0; a < _allpassFilters.Length; a++)
            {
                reverbOutput = _allpassFilters[a].Process(reverbOutput);
            }

            // 4. Mix dry and wet
            buffer[i] = compressed * dryGain + reverbOutput * wetGain;
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is KaraokeParameters p)
        {
            // Clamp parameters
            p.RoomSize = Math.Clamp(p.RoomSize, 0.3f, 1.0f);
            p.DecayTime = Math.Clamp(p.DecayTime, 0.3f, 4.5f); // Extended for stadium reverb
            p.Damping = Math.Clamp(p.Damping, 0f, 1f);
            p.Mix = Math.Clamp(p.Mix, 0f, 1f);
            p.CompressionThreshold = Math.Clamp(p.CompressionThreshold, -30f, -10f);
            p.PresenceBoost = Math.Clamp(p.PresenceBoost, 0f, 6f);

            _params = p;

            if (_sampleRate > 0)
            {
                InitializeReverb();
                UpdateCompressorCoefficients();
                UpdatePresenceEQ();
            }
        }
    }

    public void Reset()
    {
        foreach (var comb in _combFilters)
            comb.Reset();
        foreach (var allpass in _allpassFilters)
            allpass.Reset();
        _highPassFilter.Reset();
        _presenceBoost.Reset();
        _rmsEnvelope = 0f;
        _gainEnvelope = 1f;
    }

    private void InitializeReverb()
    {
        // COMB FILTER DELAY TIMES (Schroeder's original ratios, scaled by room size)
        // These are chosen to avoid common harmonics (creates dense reverb)
        // Original delays for 25kHz sample rate, scaled to current rate
        float scaleFactor = _sampleRate / 25000f * _params.RoomSize;
        int[] combDelays = new int[]
        {
            (int)(1116 * scaleFactor), // ~44ms at 48kHz
            (int)(1188 * scaleFactor), // ~47ms
            (int)(1277 * scaleFactor), // ~51ms
            (int)(1356 * scaleFactor), // ~54ms
        };

        // ALLPASS FILTER DELAY TIMES
        int[] allpassDelays = new int[]
        {
            (int)(556 * scaleFactor),  // ~22ms at 48kHz
            (int)(441 * scaleFactor),  // ~18ms
            (int)(341 * scaleFactor),  // ~14ms
        };

        // Initialize comb filters with calculated feedback
        _combFilters = new CombFilter[combDelays.Length];
        for (int i = 0; i < combDelays.Length; i++)
        {
            _combFilters[i] = new CombFilter(combDelays[i]);
            _combFilters[i].SetFeedback(_params.DecayTime, _params.Damping);
        }

        // Initialize allpass filters
        _allpassFilters = new AllpassFilter[allpassDelays.Length];
        for (int i = 0; i < allpassDelays.Length; i++)
        {
            _allpassFilters[i] = new AllpassFilter(allpassDelays[i]);
        }
    }

    private void UpdateCompressorCoefficients()
    {
        // Fast attack (10ms) and moderate release (100ms) for smooth dynamics
        _compressorAttackCoef = DSPHelpers.TimeToCoefficient(10f, _sampleRate);
        _compressorReleaseCoef = DSPHelpers.TimeToCoefficient(100f, _sampleRate);
    }

    private void UpdateHighPassFilter()
    {
        // High-pass filter to remove low-frequency rumble and proximity effect
        // Industry standard: 80-100 Hz for karaoke/vocal processing
        _highPassFilter.Design(
            BiquadFilter.FilterType.HighPass,
            90f, // 80-100 Hz range
            _sampleRate,
            q: 0.707 // Butterworth response
        );
    }

    private void UpdatePresenceEQ()
    {
        // Boost at 4kHz (vocal presence frequency)
        _presenceBoost.Design(
            BiquadFilter.FilterType.Peaking,
            4000,
            _sampleRate,
            q: 2.0, // Moderate Q for natural sound
            gainDb: _params.PresenceBoost
        );
    }

    /// <summary>
    /// Comb filter with damping for natural-sounding reverb.
    /// </summary>
    private class CombFilter
    {
        private float[] _buffer;
        private int _bufferIndex;
        private float _feedback;
        private float _damping;
        private float _filterState; // One-pole low-pass for damping

        public CombFilter(int delayLength)
        {
            _buffer = new float[Math.Max(delayLength, 1)];
            _bufferIndex = 0;
            _feedback = 0.5f;
            _damping = 0.5f;
            _filterState = 0f;
        }

        public void SetFeedback(float decayTime, float damping)
        {
            // Calculate feedback for desired decay time
            // At decay time, reverb should be -60dB (1/1000 amplitude)
            // feedback^(sampleRate * decayTime) = 0.001
            // feedback = 0.001^(1 / (sampleRate * decayTime))
            int delaySamples = _buffer.Length;
            _feedback = MathF.Pow(0.001f, (float)delaySamples / (decayTime * 48000f));
            _feedback = Math.Clamp(_feedback, 0f, 0.95f); // Safety limit
            _damping = Math.Clamp(damping, 0f, 0.95f);
        }

        public float Process(float input)
        {
            // Read from buffer
            float output = _buffer[_bufferIndex];

            // Apply damping filter (one-pole low-pass)
            _filterState = (1f - _damping) * output + _damping * _filterState;

            // Write to buffer with feedback
            _buffer[_bufferIndex] = input + _filterState * _feedback;

            // Advance buffer index
            _bufferIndex = (_bufferIndex + 1) % _buffer.Length;

            return output;
        }

        public void Reset()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _filterState = 0f;
            _bufferIndex = 0;
        }
    }

    /// <summary>
    /// Allpass filter for diffusing reflections without coloring frequency response.
    /// </summary>
    private class AllpassFilter
    {
        private float[] _buffer;
        private int _bufferIndex;
        private const float Feedback = 0.5f; // Standard allpass coefficient

        public AllpassFilter(int delayLength)
        {
            _buffer = new float[Math.Max(delayLength, 1)];
            _bufferIndex = 0;
        }

        public float Process(float input)
        {
            // Read from buffer
            float delayed = _buffer[_bufferIndex];

            // Allpass formula: output = -input + delayed + feedback * input
            float output = -input + delayed;
            _buffer[_bufferIndex] = input + delayed * Feedback;

            // Advance buffer index
            _bufferIndex = (_bufferIndex + 1) % _buffer.Length;

            return output;
        }

        public void Reset()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _bufferIndex = 0;
        }
    }
}

/// <summary>
/// KARAOKE EFFECT TUNING GUIDE:
///
/// FOR CLEAR VOCALS:
/// - RoomSize: 0.6
/// - DecayTime: 0.7s
/// - Damping: 0.7 (removes harsh highs)
/// - Mix: 30%
/// - PresenceBoost: 4dB
///
/// FOR SPACIOUS SOUND:
/// - RoomSize: 0.9
/// - DecayTime: 1.2s
/// - Damping: 0.5
/// - Mix: 40%
/// - PresenceBoost: 3dB
///
/// FOR INTIMATE SOUND:
/// - RoomSize: 0.5
/// - DecayTime: 0.5s
/// - Damping: 0.8
/// - Mix: 25%
/// - PresenceBoost: 5dB
///
/// WHY COMPRESSION FOR KARAOKE:
/// Amateur singers have inconsistent volume (whisper verses, shout chorus).
/// Compression evens this out, maintaining clear, consistent vocals.
/// We use light compression (3:1) to preserve natural dynamics.
///
/// WHY PRESENCE BOOST:
/// Background music has lots of midrange content.
/// Boosting 3-5kHz helps voice "cut through" the music.
/// Too much boost (>6dB) sounds harsh and sibilant.
///
/// REVERB IMPLEMENTATION NOTES:
/// - We use 4 comb filters (standard for quality reverb)
/// - We use 3 allpass filters (balances density vs CPU)
/// - Damping simulates high-frequency absorption in real rooms
/// - Feedback automatically calculated from desired decay time
///
/// AVOIDING REVERB ARTIFACTS:
/// 1. Unnatural ringing: Use damping (0.5-0.8)
/// 2. Metallic sound: Use multiple comb filters with prime-number delays
/// 3. Muddy reverb: Keep decay time short (0.5-1.2s for karaoke)
/// 4. Loss of clarity: Keep wet mix moderate (25-40%)
///
/// CPU OPTIMIZATION:
/// - Reverb is most CPU-intensive effect
/// - Each comb/allpass processes every sample
/// - Total operations per sample: ~20 multiplies + 20 adds
/// - Still efficient enough for real-time on modern devices
/// </summary>
