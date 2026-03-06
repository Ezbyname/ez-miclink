using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Anime character voice - bright, energetic, kawaii.
/// Popular for TikTok and social media content.
/// </summary>
public class AnimePreset : AudioPresetBase
{
    public AnimePreset()
        : base(
            name: "anime",
            displayName: "Anime Voice",
            description: "Bright energetic anime character voice",
            category: "Character Voices",
            isPremium: false)
    {
    }

    public override void Configure(AudioEffectChain chain, int sampleRate)
    {
        var gate = new NoiseGateEffect();
        gate.Prepare(sampleRate);
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -45f, AttackMs = 1f, ReleaseMs = 150f, FloorGain = -80f, KneeDb = 6f
        });
        chain.AddEffect(gate);

        var anime = new AnimeVoiceEffect();
        anime.Prepare(sampleRate);
        anime.SetParameters(new AnimeVoiceEffect.AnimeParameters
        {
            PitchSemitones = 5f,
            FormantShiftPercent = 15f,
            BrightnessDb = 4f,
            AirDb = 3f,
            Intensity = 1.0f
        });
        chain.AddEffect(anime);

        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f, Ratio = 3f, AttackMs = 10f, ReleaseMs = 100f, KneeDb = 8f, AutoMakeupGain = true
        });
        chain.AddEffect(compressor);

        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f, AttackMs = 0.5f, ReleaseMs = 100f, LookaheadMs = 3f
        });
        chain.AddEffect(limiter);
    }
}
