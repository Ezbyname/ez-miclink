using System;
using System.Collections.Generic;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Manages a chain of audio effects applied in sequence.
///
/// SIGNAL FLOW:
/// Input Buffer → Effect 1 → Effect 2 → ... → Effect N → Output Buffer
///
/// DESIGN PRINCIPLES:
/// 1. ZERO ALLOCATIONS: No GC pressure during Process()
/// 2. IN-PLACE PROCESSING: Effects modify buffer directly (no copies)
/// 3. BYPASS SUPPORT: Each effect can be bypassed independently
/// 4. THREAD-SAFE: Parameter changes don't affect processing thread
/// 5. ORDERED PIPELINE: Effect order matters for sound quality
///
/// TYPICAL EFFECT ORDER FOR VOICE:
/// 1. Noise Gate - Remove background noise FIRST (before amplification)
/// 2. EQ - Shape tone before dynamics processing
/// 3. Compressor - Control dynamics with natural tone
/// 4. Effects (Robot, Megaphone, Karaoke, etc.) - Creative processing
/// 5. Echo/Delay - Spatial effects after core processing
/// 6. Limiter - Final safety, prevent clipping LAST
///
/// WHY THIS ORDER:
/// - Noise gate first: Don't amplify noise
/// - EQ before compression: Compress the tone you want
/// - Compressor before effects: Effects work better with consistent levels
/// - Limiter last: Catch any peaks from previous effects
///
/// IN-PLACE PROCESSING EXPLAINED:
/// Traditional approach (SLOW):
///   buffer1 = input
///   buffer2 = effect1.Process(buffer1)
///   buffer3 = effect2.Process(buffer2)
///   output = buffer3
///   Problem: Multiple buffers = allocations + copies = GC pressure
///
/// Our approach (FAST):
///   buffer = input
///   effect1.Process(buffer) // Modifies buffer in-place
///   effect2.Process(buffer) // Modifies same buffer
///   output = buffer
///   Benefit: Single buffer, no allocations, cache-friendly
///
/// THREAD SAFETY:
/// - Process() is called on audio thread (high priority, real-time)
/// - SetParameters() called on UI thread
/// - Effects use atomic reads of parameters
/// - No locks in Process() (would cause latency)
/// </summary>
public class AudioEffectChain
{
    private readonly List<IAudioEffect> _effects;
    private int _sampleRate;
    private bool _isPrepared;

    public AudioEffectChain()
    {
        _effects = new List<IAudioEffect>();
        _isPrepared = false;
    }

    /// <summary>
    /// Add an effect to the end of the chain.
    /// Must be called before Prepare().
    /// </summary>
    public void AddEffect(IAudioEffect effect)
    {
        if (_isPrepared)
            throw new InvalidOperationException("Cannot add effects after chain is prepared. Call Reset() first.");

        _effects.Add(effect);
    }

    /// <summary>
    /// Remove all effects from the chain.
    /// </summary>
    public void Clear()
    {
        _effects.Clear();
        _isPrepared = false;
    }

    /// <summary>
    /// Get effect at specific index.
    /// </summary>
    public IAudioEffect? GetEffect(int index)
    {
        if (index < 0 || index >= _effects.Count)
            return null;
        return _effects[index];
    }

    /// <summary>
    /// Get effect by type.
    /// Returns first matching effect or null if not found.
    /// </summary>
    public T? GetEffect<T>() where T : class, IAudioEffect
    {
        foreach (var effect in _effects)
        {
            if (effect is T typedEffect)
                return typedEffect;
        }
        return null;
    }

    /// <summary>
    /// Get number of effects in chain.
    /// </summary>
    public int Count => _effects.Count;

    /// <summary>
    /// Prepare all effects for processing.
    /// MUST be called before Process().
    /// </summary>
    public void Prepare(int sampleRate)
    {
        _sampleRate = sampleRate;

        foreach (var effect in _effects)
        {
            effect.Prepare(sampleRate);
        }

        _isPrepared = true;
    }

    /// <summary>
    /// Process audio buffer through effect chain.
    ///
    /// CRITICAL: This is called on real-time audio thread.
    /// - No allocations
    /// - No locks
    /// - No blocking operations
    /// - No exceptions (catch internally)
    /// </summary>
    public void Process(float[] buffer, int offset, int count)
    {
        if (!_isPrepared)
            return;

        // Process each effect in sequence
        // Each effect modifies buffer in-place
        foreach (var effect in _effects)
        {
            try
            {
                if (!effect.Bypass)
                {
                    effect.Process(buffer, offset, count);
                }
            }
            catch
            {
                // NEVER let exception escape from audio thread
                // Silently bypass failed effect
                // (Production code should log this)
                effect.Bypass = true;
            }
        }
    }

    /// <summary>
    /// Reset all effects to initial state.
    /// Clears internal buffers and envelope followers.
    /// </summary>
    public void Reset()
    {
        foreach (var effect in _effects)
        {
            effect.Reset();
        }
    }

    /// <summary>
    /// Enable or disable all effects.
    /// </summary>
    public void SetBypass(bool bypass)
    {
        foreach (var effect in _effects)
        {
            effect.Bypass = bypass;
        }
    }

    /// <summary>
    /// Get current effect chain configuration as string (for debugging).
    /// </summary>
    public string GetChainDescription()
    {
        if (_effects.Count == 0)
            return "Empty chain";

        var description = $"Effect chain ({_effects.Count} effects):\n";
        for (int i = 0; i < _effects.Count; i++)
        {
            var effect = _effects[i];
            var status = effect.Bypass ? "[BYPASSED]" : "[ACTIVE]";
            description += $"{i + 1}. {effect.GetType().Name} {status}\n";
        }
        return description;
    }
}

/// <summary>
/// EFFECT ORDER GUIDELINES:
///
/// BASIC VOICE PROCESSING:
/// 1. NoiseGate
/// 2. ThreeBandEQ
/// 3. Compressor
/// 4. Limiter
///
/// PODCAST/BROADCAST:
/// 1. NoiseGate (threshold: -45dB)
/// 2. ThreeBandEQ (low shelf -2dB at 100Hz, presence +3dB at 3kHz)
/// 3. Compressor (4:1 ratio, -18dB threshold)
/// 4. Limiter (-0.5dB ceiling)
///
/// STAGE MC/ANNOUNCER:
/// 1. NoiseGate (threshold: -40dB)
/// 2. MegaphoneEffect (moderate distortion)
/// 3. ThreeBandEQ (boost mids)
/// 4. Compressor (6:1 ratio, -15dB threshold)
/// 5. Limiter (-0.3dB ceiling)
///
/// KARAOKE:
/// 1. NoiseGate (threshold: -50dB, gentle)
/// 2. ThreeBandEQ (presence boost)
/// 3. KaraokeEffect (includes compression + reverb)
/// 4. Limiter (-0.5dB ceiling)
///
/// ROBOT VOICE:
/// 1. NoiseGate
/// 2. RobotVoiceEffect
/// 3. Limiter
///
/// ECHO/DELAY:
/// 1. NoiseGate
/// 2. ThreeBandEQ
/// 3. Compressor
/// 4. EchoDelayEffect (BEFORE limiter to catch echo peaks)
/// 5. Limiter
///
/// WHY LIMITER LAST:
/// The limiter is a safety net that prevents clipping.
/// It must be last to catch any peaks created by previous effects.
/// Exception: Monitoring/metering could go after limiter.
///
/// WHY NOISE GATE FIRST:
/// Noise gate removes quiet background noise.
/// If placed after compression, the compressor would amplify the noise
/// before the gate can remove it.
///
/// WHY COMPRESSOR BEFORE EFFECTS:
/// Creative effects (robot, megaphone, echo) work better with consistent input levels.
/// Compression smooths out volume variations BEFORE they hit the effects.
///
/// PARALLEL PROCESSING (not implemented here):
/// Some effects sound better in parallel:
/// Input → [Split] → Effect A → [Mix] → Output
///              ↓              ↗
///              → Effect B → ↗
///
/// Example: Reverb often sounds better mixed parallel rather than series.
/// Current implementation is series-only for simplicity.
///
/// CPU OPTIMIZATION:
/// - Effects are processed in order (no parallelism)
/// - Bypass check before each effect (skip inactive effects)
/// - In-place processing (no buffer copies)
/// - No allocations in Process() loop
///
/// Typical CPU usage (48kHz, 256-sample buffer):
/// - NoiseGate: ~0.5% CPU
/// - ThreeBandEQ: ~1% CPU (3 biquads)
/// - Compressor: ~1% CPU
/// - Limiter: ~1% CPU
/// - EchoDelay: ~0.5% CPU
/// - KaraokeEffect: ~3% CPU (reverb is expensive)
/// - Total: ~7% CPU for full chain
///
/// Real-time safety target: < 25% CPU (allows 4x headroom for spikes)
/// </summary>
