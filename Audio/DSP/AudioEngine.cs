using System;
using BluetoothMicrophoneApp.Audio.Presets;

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

    // Preset registry (ARCHITECTURE PATTERN: Registry + Strategy)
    private PresetRegistry _presetRegistry;

    // Master gain control (always present, controlled by volume slider)
    private GainEffect _masterGain;
    private volatile float _masterGainValue = 1.0f; // LOCK-FREE: Atomic read/write

    // Noise reduction (global effect, always present but can be bypassed)
    // TODO: Implement NoiseReductionEffect
    // private NoiseReductionEffect _noiseReduction;
    private volatile bool _noiseReductionEnabled; // LOCK-FREE: Atomic read/write

    public AudioEngine()
    {
        _effectChain = new AudioEffectChain();
        _currentPreset = "None";
        _isInitialized = false;
        _masterGain = new GainEffect();
        // TODO: Implement NoiseReductionEffect
        // _noiseReduction = new NoiseReductionEffect();
        _noiseReductionEnabled = true; // Enabled by default

        // Initialize preset registry with refactored presets
        _presetRegistry = new PresetRegistry();
        RegisterPresets();
    }

    /// <summary>
    /// Register all audio presets.
    /// ARCHITECTURE: Demonstrates Open/Closed Principle.
    /// New presets can be added without modifying AudioEngine.
    /// </summary>
    private void RegisterPresets()
    {
        // Professional presets
        _presetRegistry.Register(new CleanPreset());
        _presetRegistry.Register(new PodcastPreset());
        _presetRegistry.Register(new AnnouncerPreset());
        _presetRegistry.Register(new StageMCPreset());
        _presetRegistry.Register(new KaraokePreset());
        _presetRegistry.Register(new StadiumPreset());

        // Voice effects
        _presetRegistry.Register(new RobotPreset());
        _presetRegistry.Register(new MegaphonePreset());
        _presetRegistry.Register(new DeepVoicePreset());
        _presetRegistry.Register(new ChipmunkPreset());
        _presetRegistry.Register(new AnimePreset());

        // Character voices
        _presetRegistry.Register(new NerdyPreset());
        _presetRegistry.Register(new SqueakyCartoonPreset());
        _presetRegistry.Register(new DopeyGiantPreset());
        _presetRegistry.Register(new SquawkyBirdPreset());
        _presetRegistry.Register(new DopeyDadPreset());
        _presetRegistry.Register(new MousePreset());
        _presetRegistry.Register(new VillainPreset());
        _presetRegistry.Register(new GrumpyCatPreset());
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
        // TODO: Implement NoiseReductionEffect
        // _noiseReduction.Prepare(sampleRate);
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

        // Apply input gain boost FIRST to amplify weak microphone signals
        // This compensates for weak Bluetooth microphone input
        const float INPUT_GAIN = 1.5f; // 50% boost to input signal
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = Math.Clamp(buffer[i] * INPUT_GAIN, -1f, 1f);
        }

        // Apply noise reduction SECOND (after input gain, before effects)
        // This removes background noise before it gets amplified by effects
        // TODO: Implement NoiseReductionEffect
        // if (_noiseReductionEnabled)
        // {
        //     _noiseReduction.Process(buffer, offset, count);
        // }

        // Process through effect chain
        _effectChain.Process(buffer, offset, count);

        // Apply master gain (LOCK-FREE via volatile field read)
        float masterGain = _masterGainValue; // Read volatile (atomic)
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = Math.Clamp(buffer[i] * masterGain, -1.0f, 1.0f);
        }

        // Update statistics (low overhead)
        _totalSamplesProcessed += count;

        // Debug logging (only log once per second to avoid spam)
        if (_totalSamplesProcessed % _sampleRate == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioEngine] Processing: preset={_currentPreset}, effects={_effectChain.Count}, noiseReduction={_noiseReductionEnabled}");
        }
    }

    /// <summary>
    /// Load an effect preset by name.
    /// This configures the effect chain for a specific use case.
    ///
    /// ARCHITECTURE: Hybrid approach (Registry + Legacy Switch)
    /// - Try registry first (new architecture, Open/Closed Principle)
    /// - Fall back to switch statement for presets not yet extracted
    /// - Allows incremental refactoring without breaking existing functionality
    /// </summary>
    public void SetPreset(string presetName)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("AudioEngine not initialized. Call Initialize() first.");

        System.Diagnostics.Debug.WriteLine($"[AudioEngine] SetPreset called: {presetName}");

        // Clear existing chain
        _effectChain.Clear();

        // TRY REGISTRY FIRST (NEW ARCHITECTURE)
        // If preset is registered, use it (Open/Closed Principle)
        if (_presetRegistry.Contains(presetName))
        {
            System.Diagnostics.Debug.WriteLine($"[AudioEngine] ✅ Using registry preset: {presetName}");
            _presetRegistry.ApplyPreset(presetName, _effectChain, _sampleRate);
            _currentPreset = presetName;
            System.Diagnostics.Debug.WriteLine($"[AudioEngine] Preset loaded successfully: {_currentPreset}");
            return; // DONE - preset applied from registry
        }

        // FALLBACK: Map aliases to registry names
        System.Diagnostics.Debug.WriteLine($"[AudioEngine] Resolving alias for: {presetName}");

        var resolvedName = presetName.ToLower() switch
        {
            "helium" => "chipmunk",
            "stage mc" => "stage_mc",
            "deep voice" => "deep_voice",
            "anime_voice" => "anime",
            "nerdy_voice" => "nerdy",
            "squeaky" => "squeaky_cartoon",
            "dopey giant" => "dopey_giant",
            "squawky bird" => "squawky_bird",
            "duck" => "squawky_bird",
            "dopey dad" => "dopey_dad",
            "mouse_squeak" => "mouse",
            "accented_villain" => "villain",
            "grumpy" => "grumpy_cat",
            "none" => "clean",
            _ => null
        };

        if (resolvedName != null && _presetRegistry.Contains(resolvedName))
        {
            System.Diagnostics.Debug.WriteLine($"[AudioEngine] Resolved '{presetName}' -> '{resolvedName}'");
            _presetRegistry.ApplyPreset(resolvedName, _effectChain, _sampleRate);
            _currentPreset = presetName;
            return;
        }

        throw new ArgumentException($"Unknown preset: {presetName}. Available: {string.Join(", ", _presetRegistry.GetAllPresetNames())}");
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
        // TODO: Implement NoiseReductionEffect
        // _noiseReduction.Reset();
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
    /// <summary>
    /// Set master volume (lock-free via volatile field).
    /// Called from UI thread, read by audio thread.
    /// </summary>
    public void SetVolume(double volume)
    {
        // Write to volatile field (atomic, lock-free)
        _masterGainValue = (float)Math.Clamp(volume, 0.0, 2.0);
        System.Diagnostics.Debug.WriteLine($"[AudioEngine] Volume set to {volume * 100}% (gain={_masterGainValue}) [LOCK-FREE]");
    }

    /// <summary>
    /// Enable or disable background noise reduction.
    /// Noise reduction removes constant background noise (AC hum, fan noise, etc.)
    /// while preserving speech and music content.
    /// </summary>
    /// <param name="enabled">True to enable noise reduction, false to disable</param>
    public void SetNoiseReduction(bool enabled)
    {
        _noiseReductionEnabled = enabled;
        System.Diagnostics.Debug.WriteLine($"[AudioEngine] Noise reduction {(enabled ? "ENABLED" : "DISABLED")}");

        // Reset noise reduction state when toggling
        // TODO: Implement NoiseReductionEffect
        // if (!enabled)
        // {
        //     _noiseReduction.Reset();
        // }
    }

    /// <summary>
    /// Get current noise reduction state.
    /// </summary>
    public bool IsNoiseReductionEnabled() => _noiseReductionEnabled;

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

    // Legacy Build methods removed - all presets now use the PresetRegistry pattern.
    // See Audio/Presets/ folder for individual preset implementations.
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
