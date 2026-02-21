using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional echo/delay effect with feedback and filtering.
///
/// DELAY LINE THEORY:
/// A delay line stores audio samples and plays them back after a time interval.
/// Implementation uses circular (ring) buffer for efficiency:
/// - Fixed memory allocation (no GC during processing)
/// - O(1) read/write operations
/// - Wraps around automatically
///
/// ECHO vs REVERB:
/// - Echo: Discrete repetitions (50ms-2000ms delays)
/// - Reverb: Dense reflections (thousands of tiny delays)
/// This is an echo/delay effect, not reverb.
///
/// FEEDBACK:
/// Output is fed back into input, creating multiple echoes.
/// feedback = 0.0: Single echo
/// feedback = 0.5: Moderate decay (3-4 echoes)
/// feedback = 0.7: Long decay (many echoes)
/// feedback ≥ 1.0: UNSTABLE (infinite growth)
///
/// FEEDBACK SAFETY:
/// Must limit feedback to prevent runaway:
/// 1. Clamp feedback < 0.95
/// 2. Include damping filter (high-pass or low-pass)
/// 3. Monitor output level
///
/// DAMPING FILTER:
/// Real spaces absorb high frequencies faster than lows.
/// We simulate this with low-pass filter in feedback path.
/// Creates more natural-sounding decay.
///
/// WET/DRY MIX:
/// - Dry: Original signal (0% effect)
/// - Wet: Delayed signal (100% effect)
/// - Typical: 20-40% wet for subtle echo, 50-70% for obvious effect
/// </summary>
public class EchoDelayEffect : IAudioEffect
{
    private EchoDelayParameters _params;
    private int _sampleRate;

    // Circular delay buffer
    private float[] _delayBuffer;
    private int _delayLength;
    private int _writePos;

    // Damping filter (one-pole low-pass)
    private float _dampingState;

    public bool Bypass { get; set; }

    public class EchoDelayParameters
    {
        /// <summary>Delay time in milliseconds (10 to 2000)</summary>
        public float DelayMs { get; set; } = 250f;

        /// <summary>Feedback amount (0 to 0.9, typical 0.3-0.7)</summary>
        public float Feedback { get; set; } = 0.5f;

        /// <summary>Wet/dry mix (0=dry only, 1=wet only, typical 0.3-0.5)</summary>
        public float Mix { get; set; } = 0.4f;

        /// <summary>Damping coefficient (0=no damping, 0.5=moderate, 0.9=heavy)</summary>
        public float Damping { get; set; } = 0.3f;
    }

    public EchoDelayEffect()
    {
        _params = new EchoDelayParameters();
        _delayBuffer = Array.Empty<float>();
        _dampingState = 0f;
    }

    public void Prepare(int sampleRate)
    {
        _sampleRate = sampleRate;
        AllocateDelayBuffer();
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (Bypass)
            return;

        // Read parameters (thread-safe)
        float feedback = Math.Clamp(_params.Feedback, 0f, 0.95f); // Safety clamp
        float mix = Math.Clamp(_params.Mix, 0f, 1f);
        float damping = Math.Clamp(_params.Damping, 0f, 0.95f);

        float dryGain = 1f - mix;
        float wetGain = mix;

        for (int i = offset; i < offset + count; i++)
        {
            float inputSample = buffer[i];

            // Read from delay line
            float delaySample = _delayBuffer[_writePos];

            // Apply damping filter to feedback signal
            // One-pole low-pass: y[n] = (1-a)*x[n] + a*y[n-1]
            // Higher damping = more high-frequency rolloff
            _dampingState = (1f - damping) * delaySample + damping * _dampingState;
            float dampedDelay = _dampingState;

            // Calculate feedback signal
            float feedbackSignal = dampedDelay * feedback;

            // Write to delay line (input + feedback)
            _delayBuffer[_writePos] = inputSample + feedbackSignal;

            // Output = dry signal + wet (delayed) signal
            buffer[i] = inputSample * dryGain + dampedDelay * wetGain;

            // Advance write position (circular buffer)
            _writePos = (_writePos + 1) % _delayLength;
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is EchoDelayParameters p)
        {
            // Clamp parameters
            p.DelayMs = Math.Clamp(p.DelayMs, 10f, 2000f);
            p.Feedback = Math.Clamp(p.Feedback, 0f, 0.95f);
            p.Mix = Math.Clamp(p.Mix, 0f, 1f);
            p.Damping = Math.Clamp(p.Damping, 0f, 0.95f);

            bool needRealloc = _params.DelayMs != p.DelayMs && _sampleRate > 0;

            _params = p;

            if (needRealloc)
                AllocateDelayBuffer();
        }
    }

    public void Reset()
    {
        _writePos = 0;
        _dampingState = 0f;

        if (_delayBuffer != null)
            Array.Clear(_delayBuffer, 0, _delayBuffer.Length);
    }

    private void AllocateDelayBuffer()
    {
        // Calculate delay length in samples
        // Add extra samples for safety (prevent buffer overrun during parameter changes)
        _delayLength = (int)Math.Ceiling(_params.DelayMs * _sampleRate / 1000f) + 64;

        if (_delayLength < 128)
            _delayLength = 128;

        // Allocate buffer (happens during Prepare(), not Process())
        _delayBuffer = new float[_delayLength];
        _writePos = 0;
    }
}

/// <summary>
/// CIRCULAR BUFFER EXPLANATION:
///
/// Linear buffer (naive):
///   [0][1][2][3][4]  <- write at end
///   When full, shift entire buffer left (slow! O(N))
///
/// Circular buffer:
///   [2][3][4][0][1]  <- write position wraps around
///        ^
///       write
///   Read position = (write - delay) % length
///   No shifting needed, O(1) operation
///
/// In our implementation:
/// - Write position advances each sample
/// - Read position = write position (we read before writing)
/// - When write reaches end, wraps to 0
/// - Modulo operation (%) handles wraparound
///
/// MEMORY EFFICIENCY:
/// For 1 second delay at 48kHz:
/// - Linear approach: 48000 samples × 4 bytes = 192KB per buffer
/// - Must allocate new buffer, copy, deallocate old (GC pressure)
/// Circular approach:
/// - Allocate once during Prepare()
/// - No allocations during Process() (zero GC pressure)
/// - Critical for real-time audio
///
/// PREVENTING RUNAWAY FEEDBACK:
///
/// Mathematical analysis:
/// If feedback = f, after N echoes, amplitude = f^N
/// For f < 1: f^N → 0 as N → ∞ (stable)
/// For f = 1: amplitude stays constant (marginal)
/// For f > 1: f^N → ∞ (UNSTABLE!)
///
/// Safety measures:
/// 1. Hard limit feedback < 0.95
/// 2. Damping filter reduces high frequencies each iteration
/// 3. Could add output limiter as final safety
///
/// Example decay:
/// feedback = 0.5, each echo is half volume of previous:
/// Echo 1: 0.5 (-6dB)
/// Echo 2: 0.25 (-12dB)
/// Echo 3: 0.125 (-18dB)
/// Echo 4: 0.0625 (-24dB)
/// ...effectively silent after ~5 echoes
///
/// TYPICAL SETTINGS:
///
/// SLAP-BACK (rockabilly vocal):
/// - Delay: 80-120ms
/// - Feedback: 0.1-0.2
/// - Mix: 20-30%
/// - Damping: 0.1
///
/// TAPE ECHO (dub/reggae):
/// - Delay: 300-500ms
/// - Feedback: 0.6-0.7
/// - Mix: 40-50%
/// - Damping: 0.5
///
/// STADIUM ECHO:
/// - Delay: 400-800ms
/// - Feedback: 0.4-0.5
/// - Mix: 30-40%
/// - Damping: 0.3
///
/// PING-PONG (stereo, not implemented here):
/// - Alternate delays between L and R channels
/// - Requires two delay lines
/// - Creates spatial movement
/// </summary>
