using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Nerdy/nasally high-pitched voice.
///
/// SIGNAL CHAIN: Gate -> Helium (+7 semitones, +18% formant) -> Nasal EQ boost -> Compressor -> Limiter
///
/// USE CASE:
/// - Nerd character voice
/// - Geeky sidekick impression
/// - Comedy content
/// </summary>
public class NerdyPreset : AudioPresetBase
{
    public NerdyPreset()
        : base(
            name: "nerdy",
            displayName: "Nerdy",
            description: "Nasally high-pitched nerd character voice",
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

        // High pitch + raised formants for nerdy character
        var helium = new HeliumVoiceEffect();
        helium.Prepare(sampleRate);
        helium.SetParameters(new HeliumVoiceEffect.HeliumParameters
        {
            PitchSemitones = 7f,
            FormantShiftPercent = 18f,
            BrightnessDb = 2f,
            Intensity = 1.0f
        });
        chain.AddEffect(helium);

        // Nasal EQ boost around 1kHz for nerdy quality
        var eq = new ThreeBandEQEffect();
        eq.Prepare(sampleRate);
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = -2f, LowFreq = 150f,
            MidGainDb = 5f, MidFreq = 1000f, MidQ = 2.0f,
            HighGainDb = 1f, HighFreq = 5000f
        });
        chain.AddEffect(eq);

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
