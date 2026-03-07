using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Garfield-style lazy, low, bored cat voice.
///
/// SIGNAL CHAIN: Gate -> DeepVoice (moderate) -> Warm EQ -> Compressor -> Limiter
///
/// CHARACTER REFERENCE: Garfield (Bill Murray / Lorenzo Music)
/// - Moderately deep, smooth and round (not raspy)
/// - Warm and laid-back, slightly bored quality
/// - Rich low-mids for chest resonance
/// - Rolled-off highs for that lazy, muffled feel
/// - No distortion - Garfield is smooth, not growly
/// </summary>
public class GrumpyCatPreset : AudioPresetBase
{
    public GrumpyCatPreset()
        : base(
            name: "grumpy_cat",
            displayName: "Grumpy Cat",
            description: "Lazy low smooth cat voice (Garfield-style)",
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
            ThresholdDb = -50f, AttackMs = 0.5f, ReleaseMs = 200f, FloorGain = -40f, KneeDb = 10f
        });
        chain.AddEffect(gate);

        // Moderately deep - Garfield is low but not monster-deep
        var deep = new DeepVoiceEffect();
        deep.Prepare(sampleRate);
        deep.SetParameters(new DeepVoiceEffect.DeepVoiceParameters
        {
            PitchSemitones = -2.5f,
            FormantShiftPercent = -7f,
            BassBoostDb = 3f,
            Intensity = 0.9f
        });
        chain.AddEffect(deep);

        // Warm, round EQ - boost low-mids for chest, cut highs for lazy feel
        // No distortion/megaphone - Garfield is smooth not raspy
        var eq = new ThreeBandEQEffect();
        eq.Prepare(sampleRate);
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = 2f, LowFreq = 180f,
            MidGainDb = 1f, MidFreq = 500f, MidQ = 0.8f,
            HighGainDb = -4f, HighFreq = 3500f
        });
        chain.AddEffect(eq);

        // Gentle compression - laid-back dynamics, not punchy
        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -20f, Ratio = 3f, AttackMs = 15f, ReleaseMs = 150f, KneeDb = 10f, AutoMakeupGain = true
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
