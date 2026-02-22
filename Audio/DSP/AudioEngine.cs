using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Main audio processing engine that integrates DSP effects with AudioService.
///
/// ARCHITECTURE:
/// AudioService (Platform-specific) → AudioEngine → EffectChain → Effects → Output
///
/// RESPONSIBILITIES:
/// 1. Manage effect chain lifecycle
/// 2. Process audio buffers in real-time
/// 3. Handle sample rate changes
/// 4. Provide preset management
/// 5. Monitor processing performance
///
/// INTEGRATION WITH AudioService:
/// The AudioService (platform-specific Android/iOS code) captures audio and
/// calls ProcessBuffer() on this engine. The engine applies effects and returns
/// the processed audio.
///
/// THREAD MODEL:
/// - ProcessBuffer() called on HIGH-PRIORITY audio thread
/// - SetPreset(), AddEffect(), etc. called on UI thread
/// - Must be thread-safe without locks in audio path
///
/// REAL-TIME SAFETY:
/// ProcessBuffer() MUST complete in less than buffer duration.
/// Example: 256 samples at 48kHz = 5.3ms
/// Target: Process in < 1.3ms (25% CPU, 4x safety margin)
///
/// ZERO ALLOCATION POLICY:
/// ProcessBuffer() makes ZERO allocations (no GC pressure).
/// All buffers pre-allocated during Initialize().
/// </summary>
public class AudioEngine
{
    private AudioEffectChain _effectChain;
    private int _sampleRate;
    private bool _isInitialized;

    // Performance monitoring
    private long _totalSamplesProcessed;
    private DateTime _processingStartTime;

    // Current preset
    private string _currentPreset;

    // Master gain control (always present, controlled by volume slider)
    private GainEffect _masterGain;

    public AudioEngine()
    {
        _effectChain = new AudioEffectChain();
        _currentPreset = "None";
        _isInitialized = false;
        _masterGain = new GainEffect();
    }

    /// <summary>
    /// Initialize the audio engine with sample rate.
    /// MUST be called before ProcessBuffer().
    /// </summary>
    public void Initialize(int sampleRate)
    {
        if (sampleRate < 8000 || sampleRate > 192000)
            throw new ArgumentException($"Invalid sample rate: {sampleRate}");

        _sampleRate = sampleRate;
        _totalSamplesProcessed = 0;
        _processingStartTime = DateTime.Now;
        _masterGain.Prepare(sampleRate);
        _isInitialized = true;
    }

    /// <summary>
    /// Process an audio buffer through the effect chain.
    ///
    /// CRITICAL: Called on real-time audio thread!
    /// - Must complete in < buffer_duration
    /// - No allocations
    /// - No locks
    /// - No blocking I/O
    ///
    /// Parameters:
    /// - buffer: Audio samples in range [-1.0, 1.0]
    /// - offset: Start index in buffer
    /// - count: Number of samples to process
    /// </summary>
    public void ProcessBuffer(float[] buffer, int offset, int count)
    {
        if (!_isInitialized)
            return;

        // Safety clamp (shouldn't be needed if input is correct, but protects against bad data)
        // Note: This loop is fast (no branching in CPU pipeline)
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = Math.Clamp(buffer[i], -1f, 1f);
        }

        // Process through effect chain
        _effectChain.Process(buffer, offset, count);

        // Apply master gain (user volume control)
        _masterGain.Process(buffer, offset, count);

        // Final safety clipping (hard limit at ±0.98 to prevent inter-sample peaks)
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = Math.Clamp(buffer[i], -0.98f, 0.98f);
        }

        // Update statistics (low overhead)
        _totalSamplesProcessed += count;

        // Debug logging (only log once per second to avoid spam)
        if (_totalSamplesProcessed % _sampleRate == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioEngine] Processing: preset={_currentPreset}, effects={_effectChain.Count}");
        }
    }

    /// <summary>
    /// Load an effect preset by name.
    /// This configures the effect chain for a specific use case.
    /// </summary>
    public void SetPreset(string presetName)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("AudioEngine not initialized. Call Initialize() first.");

        System.Diagnostics.Debug.WriteLine($"[AudioEngine] SetPreset called: {presetName}");

        // Clear existing chain
        _effectChain.Clear();

        // Build new chain based on preset
        switch (presetName.ToLower())
        {
            case "podcast":
                BuildPodcastPreset();
                break;

            case "stage_mc":
            case "stage mc":
                BuildStageMCPreset();
                break;

            case "karaoke":
                BuildKaraokePreset();
                break;

            case "announcer":
                BuildAnnouncerPreset();
                break;

            case "robot":
                BuildRobotPreset();
                break;

            case "megaphone":
                BuildMegaphonePreset();
                break;

            case "stadium":
                BuildStadiumPreset();
                break;

            case "deep_voice":
            case "deep voice":
                BuildDeepVoicePreset();
                break;

            case "chipmunk":
            case "helium":
                BuildChipmunkPreset();
                break;

            case "anime":
                BuildAnimeVoicePreset();
                break;

            case "podcast":
            case "podcast voice":
                BuildPodcastPreset();
                break;

            case "clean":
            case "none":
                BuildCleanPreset();
                break;

            default:
                throw new ArgumentException($"Unknown preset: {presetName}");
        }

        // Prepare all effects
        _effectChain.Prepare(_sampleRate);
        _currentPreset = presetName;

        System.Diagnostics.Debug.WriteLine($"[AudioEngine] Preset '{presetName}' loaded successfully with {_effectChain.Count} effects");
    }

    /// <summary>
    /// Get the current effect chain (for advanced manipulation).
    /// </summary>
    public AudioEffectChain GetEffectChain() => _effectChain;

    /// <summary>
    /// Reset all effects to initial state.
    /// Clears reverb tails, delay buffers, envelope states, etc.
    /// </summary>
    public void Reset()
    {
        _effectChain.Reset();
        _totalSamplesProcessed = 0;
    }

    /// <summary>
    /// Enable or disable all effects (bypass).
    /// </summary>
    public void SetBypass(bool bypass)
    {
        _effectChain.SetBypass(bypass);
    }

    /// <summary>
    /// Get current preset name.
    /// </summary>
    public string GetCurrentPreset() => _currentPreset;

    /// <summary>
    /// Set master volume/gain.
    /// This controls the final output volume regardless of preset.
    /// </summary>
    /// <param name="volume">Volume level (0.5 = 50%, 1.0 = 100%, 2.0 = 200%)</param>
    public void SetVolume(double volume)
    {
        float gain = (float)Math.Clamp(volume, 0.0, 2.0);
        _masterGain.SetGain(gain);
        System.Diagnostics.Debug.WriteLine($"[AudioEngine] Volume set to {volume * 100}% (gain={gain})");
    }

    /// <summary>
    /// Get processing statistics.
    /// </summary>
    public string GetStatistics()
    {
        var duration = DateTime.Now - _processingStartTime;
        var seconds = Math.Max(duration.TotalSeconds, 0.001);
        var samplesPerSecond = _totalSamplesProcessed / seconds;

        return $"Preset: {_currentPreset}\n" +
               $"Sample Rate: {_sampleRate} Hz\n" +
               $"Total Samples: {_totalSamplesProcessed:N0}\n" +
               $"Processing Rate: {samplesPerSecond:N0} samples/sec\n" +
               $"Effect Chain:\n{_effectChain.GetChainDescription()}";
    }

    // ============================================================================
    // PRESET BUILDERS
    // Each preset configures a specific effect chain with tuned parameters
    // ============================================================================

    private void BuildPodcastPreset()
    {
        // Professional podcast voice processing
        // Goal: Clear, consistent, broadcast-quality

        // 1. Noise Gate - Remove background noise
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -45f,
            AttackMs = 1f,
            ReleaseMs = 150f,
            FloorGain = -80f,
            KneeDb = 6f
        });
        _effectChain.AddEffect(gate);

        // 2. EQ - Shape tone (reduce bass rumble, boost presence)
        var eq = new ThreeBandEQEffect();
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = -2f,      // Reduce bass rumble
            LowFreq = 100f,
            MidGainDb = 1f,       // Slight body boost
            MidFreq = 800f,
            MidQ = 1.0f,
            HighGainDb = 3f,      // Presence boost
            HighFreq = 4000f
        });
        _effectChain.AddEffect(eq);

        // 3. Compressor - Even out dynamics
        var compressor = new CompressorEffect();
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -20f,
            Ratio = 4f,
            AttackMs = 15f,
            ReleaseMs = 150f,
            KneeDb = 8f,
            AutoMakeupGain = true
        });
        _effectChain.AddEffect(compressor);

        // 4. Limiter - Final safety
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            LookaheadMs = 3f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildStageMCPreset()
    {
        // Stage MC / Announcer with megaphone character
        // Goal: Loud, present, cuts through crowd noise

        // 1. Noise Gate
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -40f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            FloorGain = -80f,
            KneeDb = 4f
        });
        _effectChain.AddEffect(gate);

        // 2. Megaphone Effect
        var megaphone = new MegaphoneEffect();
        megaphone.SetParameters(new MegaphoneEffect.MegaphoneParameters
        {
            LowCutoffHz = 450f,
            HighCutoffHz = 3500f,
            Distortion = 0.4f,
            MidBoostDb = 4f
        });
        _effectChain.AddEffect(megaphone);

        // 3. Compressor - Aggressive
        var compressor = new CompressorEffect();
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -15f,
            Ratio = 6f,
            AttackMs = 5f,
            ReleaseMs = 80f,
            KneeDb = 4f,
            AutoMakeupGain = true
        });
        _effectChain.AddEffect(compressor);

        // 4. Limiter
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.3f,
            AttackMs = 0.3f,
            ReleaseMs = 80f,
            LookaheadMs = 4f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildKaraokePreset()
    {
        // Karaoke with reverb and dynamics
        // Goal: Spacious, flattering vocal sound

        // 1. Noise Gate - Gentle
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -50f,
            AttackMs = 2f,
            ReleaseMs = 200f,
            FloorGain = -70f,
            KneeDb = 10f
        });
        _effectChain.AddEffect(gate);

        // 2. EQ - Presence boost
        var eq = new ThreeBandEQEffect();
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = -1f,
            LowFreq = 120f,
            MidGainDb = 2f,
            MidFreq = 1000f,
            MidQ = 1.2f,
            HighGainDb = 2f,
            HighFreq = 5000f
        });
        _effectChain.AddEffect(eq);

        // 3. Karaoke Effect (includes compression + reverb)
        var karaoke = new KaraokeEffect();
        karaoke.SetParameters(new KaraokeEffect.KaraokeParameters
        {
            RoomSize = 0.7f,
            DecayTime = 0.9f,
            Damping = 0.6f,
            Mix = 0.35f,
            CompressionThreshold = -18f,
            PresenceBoost = 3f
        });
        _effectChain.AddEffect(karaoke);

        // 4. Limiter
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            LookaheadMs = 3f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildAnnouncerPreset()
    {
        // Professional announcer voice
        // Goal: Deep, authoritative, broadcast quality

        // 1. Noise Gate
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -42f,
            AttackMs = 1f,
            ReleaseMs = 120f,
            FloorGain = -80f,
            KneeDb = 6f
        });
        _effectChain.AddEffect(gate);

        // 2. EQ - Warm, full-bodied
        var eq = new ThreeBandEQEffect();
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = 3f,       // Boost bass for depth
            LowFreq = 120f,
            MidGainDb = 0f,
            MidFreq = 1000f,
            MidQ = 1.0f,
            HighGainDb = 2f,      // Clarity
            HighFreq = 6000f
        });
        _effectChain.AddEffect(eq);

        // 3. Compressor - Moderate
        var compressor = new CompressorEffect();
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f,
            Ratio = 4f,
            AttackMs = 10f,
            ReleaseMs = 120f,
            KneeDb = 8f,
            AutoMakeupGain = true
        });
        _effectChain.AddEffect(compressor);

        // 4. Limiter
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            LookaheadMs = 3f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildRobotPreset()
    {
        // Classic robot voice
        // Goal: Mechanical, synthetic, but intelligible

        // 1. Noise Gate
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -45f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            FloorGain = -80f,
            KneeDb = 4f
        });
        _effectChain.AddEffect(gate);

        // 2. Robot Effect
        var robot = new RobotVoiceEffect();
        robot.SetParameters(new RobotVoiceEffect.RobotVoiceParameters
        {
            CarrierFrequencyHz = 150f,
            Intensity = 0.85f,
            OctaveShift = 0f
        });
        _effectChain.AddEffect(robot);

        // 3. Limiter
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.3f,
            ReleaseMs = 80f,
            LookaheadMs = 2f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildMegaphonePreset()
    {
        // Pure megaphone effect
        // Goal: Lo-fi, distorted, loudspeaker character

        // 1. Noise Gate
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -40f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            FloorGain = -80f,
            KneeDb = 4f
        });
        _effectChain.AddEffect(gate);

        // 2. Megaphone Effect
        var megaphone = new MegaphoneEffect();
        megaphone.SetParameters(new MegaphoneEffect.MegaphoneParameters
        {
            LowCutoffHz = 400f,
            HighCutoffHz = 3000f,
            Distortion = 0.6f,
            MidBoostDb = 4f
        });
        _effectChain.AddEffect(megaphone);

        // 3. Limiter
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.3f,
            ReleaseMs = 80f,
            LookaheadMs = 2f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildStadiumPreset()
    {
        // Stadium/arena reverb effect with extended decay
        // Goal: Large venue space simulation (2.5-4.5s decay for true stadium)
        // Industry standard: Concert hall acoustics

        // 1. Noise Gate
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -45f,
            AttackMs = 1f,
            ReleaseMs = 150f,
            FloorGain = -80f,
            KneeDb = 6f
        });
        _effectChain.AddEffect(gate);

        // 2. EQ - Clean presence for large space
        var eq = new ThreeBandEQEffect();
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = -1f,
            LowFreq = 100f,
            MidGainDb = 0f,
            MidFreq = 1000f,
            MidQ = 1.0f,
            HighGainDb = 2f,
            HighFreq = 4000f
        });
        _effectChain.AddEffect(eq);

        // 3. Compressor
        var compressor = new CompressorEffect();
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f,
            Ratio = 3f,
            AttackMs = 15f,
            ReleaseMs = 150f,
            KneeDb = 8f,
            AutoMakeupGain = true
        });
        _effectChain.AddEffect(compressor);

        // 4. Stadium Reverb (extended decay time for large venue)
        var stadium = new KaraokeEffect();
        stadium.SetParameters(new KaraokeEffect.KaraokeParameters
        {
            RoomSize = 1.0f,          // Maximum room size for stadium
            DecayTime = 3.5f,         // Extended decay: 2.5-4.5s typical for stadium
            Damping = 0.4f,           // Less damping for larger space
            Mix = 0.45f,              // More wet for stadium ambience
            CompressionThreshold = -20f, // Built-in compression
            PresenceBoost = 2f        // Moderate presence
        });
        _effectChain.AddEffect(stadium);

        // 5. Echo Effect (for distinct reflections)
        var echo = new EchoDelayEffect();
        echo.SetParameters(new EchoDelayEffect.EchoDelayParameters
        {
            DelayMs = 450f,
            Feedback = 0.45f,
            Mix = 0.4f,
            Damping = 0.4f
        });
        _effectChain.AddEffect(echo);

        // 5. Limiter
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            LookaheadMs = 3f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildDeepVoicePreset()
    {
        // Deep voice effect with true pitch and formant shifting
        // Goal: Lower, fuller voice with natural character
        // Uses professional pitch shifting + formant preservation

        // 1. Noise Gate
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -45f,
            AttackMs = 1f,
            ReleaseMs = 150f,
            FloorGain = -80f,
            KneeDb = 6f
        });
        _effectChain.AddEffect(gate);

        // 2. Deep Voice Effect (pitch + formant shift)
        var deepVoice = new DeepVoiceEffect();
        deepVoice.SetParameters(new DeepVoiceEffect.DeepVoiceParameters
        {
            PitchSemitones = -4f,      // Down 4 semitones
            FormantShiftPercent = -8f,  // Lower formants 8%
            BassBoostDb = 4f,           // Add warmth
            Intensity = 1.0f            // Full effect
        });
        _effectChain.AddEffect(deepVoice);

        // 3. Compressor
        var compressor = new CompressorEffect();
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f,
            Ratio = 3f,
            AttackMs = 15f,
            ReleaseMs = 150f,
            KneeDb = 8f,
            AutoMakeupGain = true
        });
        _effectChain.AddEffect(compressor);

        // 4. Limiter
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            LookaheadMs = 3f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildChipmunkPreset()
    {
        // Chipmunk/Helium voice effect with true pitch and formant shifting
        // Goal: Higher, squeaky voice characteristic
        // Uses professional pitch shifting + formant shifting

        // 1. Noise Gate
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -45f,
            AttackMs = 1f,
            ReleaseMs = 150f,
            FloorGain = -80f,
            KneeDb = 6f
        });
        _effectChain.AddEffect(gate);

        // 2. Helium Voice Effect (pitch + formant shift)
        var helium = new HeliumVoiceEffect();
        helium.SetParameters(new HeliumVoiceEffect.HeliumParameters
        {
            PitchSemitones = 5f,        // Up 5 semitones
            FormantShiftPercent = 15f,   // Raise formants 15%
            BrightnessDb = 4f,           // Add sparkle
            Intensity = 1.0f             // Full effect
        });
        _effectChain.AddEffect(helium);

        // 3. Compressor
        var compressor = new CompressorEffect();
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f,
            Ratio = 3f,
            AttackMs = 10f,
            ReleaseMs = 100f,
            KneeDb = 8f,
            AutoMakeupGain = true
        });
        _effectChain.AddEffect(compressor);

        // 4. Limiter
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            LookaheadMs = 3f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildAnimeVoicePreset()
    {
        // Anime voice effect with pitch and formant shifting
        // Goal: Bright, energetic anime character voice
        // Popular on TikTok and social media content

        // 1. Noise Gate
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -45f,
            AttackMs = 1f,
            ReleaseMs = 150f,
            FloorGain = -80f,
            KneeDb = 6f
        });
        _effectChain.AddEffect(gate);

        // 2. Anime Voice Effect (pitch + formant shift + brightness + air)
        var anime = new AnimeVoiceEffect();
        anime.SetParameters(new AnimeVoiceEffect.AnimeParameters
        {
            PitchSemitones = 5f,        // Up 5 semitones (anime character tone)
            FormantShiftPercent = 15f,   // Raise formants 15% (smaller vocal tract)
            BrightnessDb = 4f,           // Add clarity and presence
            AirDb = 3f,                  // Add sweet, airy quality
            Intensity = 1.0f             // Full effect
        });
        _effectChain.AddEffect(anime);

        // 3. Compressor (already included in effect, but add extra polish)
        var compressor = new CompressorEffect();
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f,
            Ratio = 3f,
            AttackMs = 10f,
            ReleaseMs = 100f,
            KneeDb = 8f,
            AutoMakeupGain = true
        });
        _effectChain.AddEffect(compressor);

        // 4. Limiter
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            LookaheadMs = 3f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildCleanPreset()
    {
        // Clean processing - minimal coloration
        // Goal: Transparent, safe, professional

        // 1. Noise Gate - Very gentle
        var gate = new NoiseGateEffect();
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -50f,
            AttackMs = 2f,
            ReleaseMs = 200f,
            FloorGain = -80f,
            KneeDb = 10f
        });
        _effectChain.AddEffect(gate);

        // 2. EQ - Subtle cleanup
        var eq = new ThreeBandEQEffect();
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = -1f,      // Remove slight rumble
            LowFreq = 80f,
            MidGainDb = 0f,
            MidFreq = 1000f,
            MidQ = 1.0f,
            HighGainDb = 1f,      // Slight air boost
            HighFreq = 8000f
        });
        _effectChain.AddEffect(eq);

        // 3. Limiter - Safety only
        var limiter = new LimiterEffect();
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            LookaheadMs = 3f
        });
        _effectChain.AddEffect(limiter);
    }

    private void BuildPodcastPreset()
    {
        // Professional podcast voice processing
        // Goal: Broadcast-quality voice with -16 LUFS loudness (industry standard)
        // Signal chain: HPF → Gate → De-esser → EQ → Compression → Limiter

        // 1. Podcast Voice Effect (complete broadcast chain)
        var podcast = new PodcastVoiceEffect();
        podcast.SetParameters(new PodcastVoiceEffect.PodcastParameters
        {
            HighPassFreq = 80f,              // Remove rumble
            GateThresholdDb = -45f,          // Remove background noise
            DeEsserAmount = 0.5f,            // Control sibilance (moderate)
            PresenceBoostDb = 4f,            // Voice clarity and intelligibility
            AirBoostDb = 2f,                 // Professional sheen
            CompressionRatio = 4f,           // Broadcast standard (4:1)
            CompressionThresholdDb = -18f,   // Catch most dynamic range
            LimiterEnabled = true            // Safety net, prevent clipping
        });
        _effectChain.AddEffect(podcast);

        // Note: PodcastVoiceEffect is a complete broadcast chain.
        // It includes all stages (HPF, gate, de-esser, EQ, compressor, limiter).
        // No additional effects needed - this is broadcast-ready audio.
    }
}

/// <summary>
/// INTEGRATION NOTES:
///
/// To integrate with platform-specific AudioService:
///
/// 1. Create AudioEngine instance in AudioService:
///    private AudioEngine _audioEngine = new AudioEngine();
///
/// 2. Initialize in StartAudioRoutingAsync():
///    _audioEngine.Initialize(48000); // or your sample rate
///    _audioEngine.SetPreset("podcast"); // or user's choice
///
/// 3. Call ProcessBuffer() in audio callback:
///    // After reading from microphone into buffer:
///    _audioEngine.ProcessBuffer(audioBuffer, 0, audioBuffer.Length);
///    // Then write buffer to output
///
/// 4. Clean up in StopAudioRoutingAsync():
///    _audioEngine.Reset();
///
/// EXAMPLE (Android):
/// ```csharp
/// private void AudioRecordingCallback(byte[] audioData)
/// {
///     // Convert byte[] to float[]
///     float[] floatBuffer = ConvertBytesToFloat(audioData);
///
///     // Process through engine
///     _audioEngine.ProcessBuffer(floatBuffer, 0, floatBuffer.Length);
///
///     // Convert back to byte[]
///     byte[] processedData = ConvertFloatToBytes(floatBuffer);
///
///     // Play to output
///     _audioTrack.Write(processedData, 0, processedData.Length);
/// }
/// ```
///
/// PRESET SWITCHING:
/// Users can switch presets at runtime:
/// ```csharp
/// public void ChangeEffect(string effectName)
/// {
///     _audioEngine.SetPreset(effectName);
/// }
/// ```
///
/// This will rebuild the effect chain with new settings.
/// Old effects are cleaned up automatically (GC handled, not in audio thread).
/// </summary>
