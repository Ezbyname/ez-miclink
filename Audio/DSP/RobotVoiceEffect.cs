using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Robot voice effect using ring modulation.
///
/// RING MODULATION THEORY:
/// Ring modulation multiplies audio signal by a sine wave (carrier frequency).
/// Result: creates sidebands at carrier ± audio frequencies.
///
/// Example: 1000Hz voice + 500Hz carrier
/// Output: 500Hz, 1500Hz (original 1000Hz removed)
///
/// Why "robot"?
/// - Removes original pitch
/// - Creates inharmonic partials
/// - Sounds metallic/mechanical
/// - Still intelligible but unnatural
///
/// MATHEMATICS:
/// Input: s(t)
/// Carrier: c(t) = sin(2πft)
/// Output: o(t) = s(t) × c(t)
///
/// Using trig identity:
/// sin(A) × sin(B) = 0.5[cos(A-B) - cos(A+B)]
///
/// This creates sum and difference frequencies (sidebands).
///
/// CARRIER FREQUENCY SELECTION:
/// - 30-100Hz: Deep, buzzy robot
/// - 100-300Hz: Classic Dalek/robot
/// - 300-500Hz: Metallic but clear
/// - 500Hz+: Harsh, distorted
///
/// Optimal for intelligibility: 80-200Hz
///
/// IMPLEMENTATION CHOICES:
/// 1. Simple ring mod: Just multiply by sine
/// 2. With formant preservation: Blend with original
/// 3. With filtering: Remove carrier artifacts
///
/// We use #2: blend between clean and modulated.
/// This preserves some naturalness while adding robot character.
/// </summary>
public class RobotVoiceEffect : IAudioEffect
{
    private RobotVoiceParameters _params;
    private int _sampleRate;

    // Oscillator phase (0 to 2π)
    private float _phase;
    private float _phaseIncrement;

    public bool Bypass { get; set; }

    public class RobotVoiceParameters
    {
        /// <summary>Carrier frequency in Hz (30-500, typical 100-200)</summary>
        public float CarrierFrequencyHz { get; set; } = 150f;

        /// <summary>Effect intensity (0=clean, 1=full robot, typical 0.7-0.9)</summary>
        public float Intensity { get; set; } = 0.8f;

        /// <summary>Octave shift (-2 to +2, 0=no shift)</summary>
        public float OctaveShift { get; set; } = 0f;
    }

    public RobotVoiceEffect()
    {
        _params = new RobotVoiceParameters();
        _phase = 0f;
    }

    public void Prepare(int sampleRate)
    {
        _sampleRate = sampleRate;
        UpdateOscillator();
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (Bypass)
            return;

        float intensity = Math.Clamp(_params.Intensity, 0f, 1f);

        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];

            // Generate carrier sine wave
            float carrier = MathF.Sin(_phase);

            // Ring modulation: multiply signal by carrier
            float modulated = sample * carrier;

            // Blend between clean and modulated based on intensity
            float output = DSPHelpers.Lerp(sample, modulated, intensity);

            buffer[i] = output;

            // Advance oscillator phase
            _phase += _phaseIncrement;

            // Wrap phase to prevent accumulation error
            while (_phase >= MathF.PI * 2f)
                _phase -= MathF.PI * 2f;
        }
    }

    public void SetParameters(object parameters)
    {
        if (parameters is RobotVoiceParameters p)
        {
            // Clamp parameters
            p.CarrierFrequencyHz = Math.Clamp(p.CarrierFrequencyHz, 30f, 500f);
            p.Intensity = Math.Clamp(p.Intensity, 0f, 1f);
            p.OctaveShift = Math.Clamp(p.OctaveShift, -2f, 2f);

            _params = p;

            if (_sampleRate > 0)
                UpdateOscillator();
        }
    }

    public void Reset()
    {
        _phase = 0f;
    }

    private void UpdateOscillator()
    {
        // Calculate frequency with octave shift
        // Octave shift: multiply frequency by 2^shift
        float shiftMultiplier = MathF.Pow(2f, _params.OctaveShift);
        float actualFreq = _params.CarrierFrequencyHz * shiftMultiplier;

        // Calculate phase increment per sample
        // phase_increment = 2π * frequency / sampleRate
        _phaseIncrement = (2f * MathF.PI * actualFreq) / _sampleRate;
    }
}

/// <summary>
/// ALTERNATIVE ROBOT VOICE METHODS:
///
/// 1. RING MODULATION (implemented above):
/// Pros: Simple, efficient, classic robot sound
/// Cons: Can be harsh, loses pitch information
///
/// 2. PITCH QUANTIZATION:
/// Force pitch to chromatic scale or single note
/// Pros: Maintains formants, very "auto-tune robot"
/// Cons: Requires pitch detection (complex, latency)
///
/// 3. VOCODER:
/// Analysis: Extract amplitude envelope of frequency bands
/// Synthesis: Apply envelopes to synthetic carrier
/// Pros: Clear, controllable, classic Daft Punk sound
/// Cons: Complex (16-32 band filter bank), CPU intensive
///
/// 4. BITCRUSHER + RING MOD:
/// Reduce bit depth + ring modulation
/// Pros: Very robotic, "digital" character
/// Cons: Can be too harsh for intelligibility
///
/// WHY RING MODULATION FOR REAL-TIME:
/// - Zero latency (no buffering needed)
/// - Low CPU (just one multiply per sample)
/// - Deterministic (no pitch detection failures)
/// - Instantly responsive to parameter changes
/// - Maintains intelligibility at proper settings
///
/// TYPICAL SETTINGS:
///
/// CLASSIC ROBOT (Dalek):
/// - Carrier: 150Hz
/// - Intensity: 0.85
/// - Octave: 0
///
/// DEEP ROBOT (Transformer):
/// - Carrier: 80Hz
/// - Intensity: 0.9
/// - Octave: -1
///
/// SPACE ROBOT:
/// - Carrier: 220Hz
/// - Intensity: 0.75
/// - Octave: +0.5
///
/// SUBTLE SYNTHETIC:
/// - Carrier: 300Hz
/// - Intensity: 0.5
/// - Octave: 0
///
/// IMPROVING INTELLIGIBILITY:
/// 1. Lower intensity (0.6-0.8) preserves more original signal
/// 2. Lower carrier frequency (80-150Hz) less harsh
/// 3. Add slight high-pass filter (remove low-frequency warble)
/// 4. Optional: Blend with formant-preserved signal
///
/// FORMANT PRESERVATION (advanced):
/// To maintain voice character:
/// 1. Split signal into carrier (pitch) and formant (tone)
/// 2. Apply ring mod only to carrier
/// 3. Recombine
/// Requires LPC analysis - beyond scope of simple ring mod.
///
/// OSCILLATOR PRECISION:
/// We use float32 phase accumulator.
/// Potential issue: phase drift over time (accumulation error).
/// Solution: Wrap phase frequently (every cycle).
/// For extreme precision, use double or phase modulo 2π each sample.
/// </summary>
