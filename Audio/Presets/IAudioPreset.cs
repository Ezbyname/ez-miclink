namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Represents an audio preset configuration.
/// Presets define a specific combination of audio effects for a particular use case.
///
/// DESIGN PATTERN: Strategy Pattern
/// Each preset encapsulates an algorithm (effect configuration) that can be
/// selected at runtime without modifying the AudioEngine.
///
/// BENEFITS:
/// - Open/Closed Principle: Add new presets without modifying existing code
/// - Single Responsibility: Each preset class focuses on one configuration
/// - Testability: Can test presets in isolation
/// - Extensibility: Easy to add user-defined presets
/// </summary>
public interface IAudioPreset
{
    /// <summary>
    /// Unique identifier for this preset.
    /// Used for preset selection and storage.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable display name for UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Description of what this preset does.
    /// Shown in UI to help users choose.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Category for organization (e.g., "Voice Effects", "Character Voices", "Professional").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Whether this preset is available for free or requires premium.
    /// </summary>
    bool IsPremium { get; }

    /// <summary>
    /// Configure the audio effect chain for this preset.
    ///
    /// CRITICAL: This is called on UI thread, NOT audio thread.
    /// Safe to allocate memory and modify effect chain.
    ///
    /// </summary>
    /// <param name="chain">Effect chain to configure</param>
    /// <param name="sampleRate">Audio sample rate (e.g., 44100 Hz)</param>
    void Configure(DSP.AudioEffectChain chain, int sampleRate);
}
