using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Simple real-time pitch shifter using time-stretching approach.
///
/// PITCH SHIFTING THEORY:
/// Pitch shifting changes the fundamental frequency of audio without changing tempo.
/// This is different from simple speed change (which affects both pitch and tempo).
///
/// SIMPLIFIED APPROACH FOR REAL-TIME:
/// Instead of complex overlap-add, we use direct resampling with crossfading.
/// This sacrifices some quality for reliability and low latency.
///
/// ALGORITHM:
/// 1. Write input to delay buffer
/// 2. Read at different rate (pitch shift)
/// 3. Crossfade between read heads to avoid clicks
/// 4. Reset read position when needed
///
/// PITCH SHIFT RATIO:
/// - ratio = 1.0: No change
/// - ratio = 2.0: Up one octave (double frequency)
/// - ratio = 0.5: Down one octave (half frequency)
/// - ratio = 1.059: Up one semitone (2^(1/12))
///
/// SEMITONES TO RATIO:
/// ratio = 2^(semitones/12)
/// Examples:
/// - +12 semitones = 2.0 (one octave up)
/// - -12 semitones = 0.5 (one octave down)
/// - +5 semitones = 1.335 (perfect fourth up)
///
/// LIMITATIONS:
/// This method changes ALL frequencies proportionally, including formants.
/// Result: "chipmunk" or "monster" effect.
/// For natural voice, formant shifting is done separately in voice effects.
///
/// REAL-TIME CONSIDERATIONS:
/// - Simple algorithm, very CPU efficient
/// - Low latency (buffer size only)
/// - No complex windowing or FFT
/// - Some artifacts acceptable for "fun" effects
///
/// For "fun" effects (helium, deep voice), we WANT formant shift.
/// So simple pitch shifting is actually perfect for our use case!
/// </summary>
public class SimplePitchShifter
{
    private const int BUFFER_SIZE = 8192; // Large buffer for smooth operation
    private const int CROSSFADE_SIZE = 256; // Samples for crossfade

    // Circular delay buffer
    private float[] _buffer;
    private int _writePos;
    private float _readPos1;
    private float _readPos2;
    private bool _useReadPos1;
    private int _samplesUntilCrossfade;

    // Pitch shift ratio (1.0 = no shift)
    private float _pitchRatio;

    public SimplePitchShifter()
    {
        _buffer = new float[BUFFER_SIZE];
        _pitchRatio = 1.0f;
        Reset();
    }

    /// <summary>
    /// Set pitch shift amount in semitones.
    /// Positive = higher pitch, Negative = lower pitch
    /// Range: -12 to +12 semitones (±1 octave)
    /// </summary>
    public void SetPitchSemitones(float semitones)
    {
        semitones = Math.Clamp(semitones, -12f, 12f);
        _pitchRatio = MathF.Pow(2f, semitones / 12f);
    }

    /// <summary>
    /// Set pitch shift ratio directly.
    /// 1.0 = no change, 2.0 = up octave, 0.5 = down octave
    /// </summary>
    public void SetPitchRatio(float ratio)
    {
        _pitchRatio = Math.Clamp(ratio, 0.5f, 2.0f);
    }

    /// <summary>
    /// Get current pitch ratio
    /// </summary>
    public float GetPitchRatio() => _pitchRatio;

    /// <summary>
    /// Process audio with pitch shifting.
    /// Simple resampling with crossfade for real-time performance.
    /// </summary>
    public void Process(float[] buffer, int offset, int count)
    {
        // If no pitch shift, pass through
        if (MathF.Abs(_pitchRatio - 1.0f) < 0.001f)
            return;

        for (int i = offset; i < offset + count; i++)
        {
            float inputSample = buffer[i];

            // Write to circular buffer
            _buffer[_writePos] = inputSample;
            _writePos = (_writePos + 1) % BUFFER_SIZE;

            // Read with pitch shift (linear interpolation)
            float sample1 = ReadInterpolated(_readPos1);
            float sample2 = ReadInterpolated(_readPos2);

            // Crossfade between two read heads to avoid clicks
            float output;
            if (_samplesUntilCrossfade <= 0)
            {
                // Use active read head
                output = _useReadPos1 ? sample1 : sample2;
            }
            else if (_samplesUntilCrossfade >= CROSSFADE_SIZE)
            {
                // No crossfade yet
                output = _useReadPos1 ? sample1 : sample2;
            }
            else
            {
                // Crossfading
                float crossfadeRatio = (float)_samplesUntilCrossfade / CROSSFADE_SIZE;
                if (_useReadPos1)
                {
                    // Fading from read1 to read2
                    output = sample1 * crossfadeRatio + sample2 * (1f - crossfadeRatio);
                }
                else
                {
                    // Fading from read2 to read1
                    output = sample2 * crossfadeRatio + sample1 * (1f - crossfadeRatio);
                }
                _samplesUntilCrossfade--;

                if (_samplesUntilCrossfade <= 0)
                {
                    // Crossfade complete, switch active read head
                    _useReadPos1 = !_useReadPos1;
                }
            }

            buffer[i] = output;

            // Advance read positions at different rate
            _readPos1 += _pitchRatio;
            _readPos2 += _pitchRatio;

            // Check if we need to reset read position (prevent drift)
            // When read position gets too far from write position, reset and trigger crossfade
            float distance1 = GetDistance(_writePos, _readPos1);
            float distance2 = GetDistance(_writePos, _readPos2);

            if (_useReadPos1 && distance1 > BUFFER_SIZE * 0.7f)
            {
                // Reset inactive read head and start crossfade
                _readPos2 = _writePos - BUFFER_SIZE / 2;
                if (_readPos2 < 0) _readPos2 += BUFFER_SIZE;
                _samplesUntilCrossfade = CROSSFADE_SIZE;
            }
            else if (!_useReadPos1 && distance2 > BUFFER_SIZE * 0.7f)
            {
                // Reset inactive read head and start crossfade
                _readPos1 = _writePos - BUFFER_SIZE / 2;
                if (_readPos1 < 0) _readPos1 += BUFFER_SIZE;
                _samplesUntilCrossfade = CROSSFADE_SIZE;
            }
        }
    }

    /// <summary>
    /// Reset internal state
    /// </summary>
    public void Reset()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _writePos = 0;
        _readPos1 = BUFFER_SIZE / 2;
        _readPos2 = BUFFER_SIZE / 4;
        _useReadPos1 = true;
        _samplesUntilCrossfade = 0;
    }

    private float ReadInterpolated(float position)
    {
        // Wrap position
        while (position < 0) position += BUFFER_SIZE;
        while (position >= BUFFER_SIZE) position -= BUFFER_SIZE;

        // Linear interpolation
        int idx0 = (int)position;
        int idx1 = (idx0 + 1) % BUFFER_SIZE;
        float frac = position - idx0;

        return _buffer[idx0] * (1f - frac) + _buffer[idx1] * frac;
    }

    private float GetDistance(int writePos, float readPos)
    {
        float dist = writePos - readPos;
        if (dist < 0) dist += BUFFER_SIZE;
        return dist;
    }
}

/// <summary>
/// ALTERNATIVE PITCH SHIFTING ALGORITHMS:
///
/// 1. SIMPLE RESAMPLING (implemented above):
/// Pros: Simple, low latency, very efficient, reliable
/// Cons: Some artifacts, formants shift with pitch
/// Best for: Real-time fun effects, moderate pitch shifts (±8 semitones)
///
/// 2. TIME-DOMAIN SOLA (Synchronized Overlap-Add):
/// Pros: Better quality, smoother
/// Cons: More complex, needs careful buffer management
/// Best for: Real-time, quality pitch shifts (±5 semitones)
///
/// 3. PHASE VOCODER (frequency domain):
/// Method: FFT → adjust phases → IFFT
/// Pros: Highest quality, better for large shifts
/// Cons: Complex, more latency, CPU intensive
/// Best for: Offline processing, large pitch shifts
///
/// 4. PSOLA (Pitch Synchronous Overlap-Add):
/// Method: Detect pitch periods, overlap-add at different rate
/// Pros: Best quality for voice, preserves formants
/// Cons: Requires pitch detection, very complex
/// Best for: Voice-specific pitch shifting (professional tools)
///
/// WHY SIMPLE RESAMPLING FOR REAL-TIME:
/// - Extremely simple and reliable
/// - Very low CPU usage
/// - No complex windowing, FFT, or pitch detection
/// - Acceptable quality for "fun" voice effects
/// - Works well in real-time streaming scenarios
/// - Crossfading prevents most audible clicks
///
/// FORMANT PRESERVATION:
/// Simple pitch shifting moves formants (vocal tract resonances).
/// Result: Characteristic "chipmunk" or "monster" voice.
/// For natural voice, you need separate formant shifting (done in voice effects).
/// For fun effects, this characteristic sound is actually desired!
///
/// TYPICAL USE CASES:
/// - Chipmunk/Helium voice: +4 to +8 semitones
/// - Deep/Monster voice: -4 to -7 semitones
/// - Slight tuning: ±1 to 2 semitones
///
/// ARTIFACTS AND MITIGATION:
/// - Metallic/robotic sound: Normal for large shifts, reduced by formant EQ
/// - Clicks/pops: Mitigated by crossfade between read heads
/// - Pitch drift: Reset read position periodically
/// - Quality vs CPU: This implementation favors reliability and performance
/// </summary>
