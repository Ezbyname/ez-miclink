namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Base class for audio presets providing common functionality.
/// Reduces boilerplate code for preset implementations.
/// </summary>
public abstract class AudioPresetBase : IAudioPreset
{
    protected AudioPresetBase(string name, string displayName, string description, string category, bool isPremium)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
        Category = category;
        IsPremium = isPremium;
    }

    public string Name { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string Category { get; }
    public bool IsPremium { get; }

    /// <summary>
    /// Configure the audio effect chain.
    /// Override this to define your preset's effects.
    /// </summary>
    public abstract void Configure(DSP.AudioEffectChain chain, int sampleRate);
}
