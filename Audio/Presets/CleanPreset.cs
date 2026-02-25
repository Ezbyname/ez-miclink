using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Clean / passthrough preset with no effects.
///
/// GOAL: Pure, unprocessed audio
/// SIGNAL CHAIN: (none - passthrough)
///
/// USE CASE:
/// - Testing microphone quality
/// - Comparing with effects
/// - Natural voice communication
/// - Resetting to default
/// </summary>
public class CleanPreset : AudioPresetBase
{
    public CleanPreset()
        : base(
            name: "clean",
            displayName: "Clean",
            description: "Pure unprocessed audio with no effects",
            category: "Basic",
            isPremium: false)
    {
    }

    public override void Configure(AudioEffectChain chain, int sampleRate)
    {
        // No effects - clean passthrough
        // Audio will only have:
        // - Input gain boost (applied in AudioEngine.ProcessBuffer)
        // - Noise reduction (if enabled globally)
        // - Master volume (applied in AudioEngine.ProcessBuffer)
    }
}
