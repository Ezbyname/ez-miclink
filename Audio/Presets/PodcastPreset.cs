using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Professional podcast voice processing preset.
///
/// GOAL: Broadcast-quality voice with -16 LUFS loudness (industry standard)
/// SIGNAL CHAIN: HPF → Gate → De-esser → EQ → Compression → Limiter
///
/// USE CASE:
/// - Recording podcasts
/// - Professional voiceovers
/// - YouTube narration
/// - Audiobook recording
/// </summary>
public class PodcastPreset : AudioPresetBase
{
    public PodcastPreset()
        : base(
            name: "podcast",
            displayName: "Podcast",
            description: "Professional broadcast-quality voice with clarity and warmth",
            category: "Professional",
            isPremium: false)
    {
    }

    public override void Configure(AudioEffectChain chain, int sampleRate)
    {
        // Podcast Voice Effect (complete broadcast chain)
        // Includes: HPF, Gate, De-esser, EQ, Compressor, Limiter
        var podcast = new PodcastVoiceEffect();
        podcast.Prepare(sampleRate);
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
        chain.AddEffect(podcast);

        // Note: PodcastVoiceEffect is a complete broadcast chain.
        // No additional effects needed - this is broadcast-ready audio.
    }
}
