using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Gru-style villain voice - nasal, slightly deep, dramatic Eastern European feel.
///
/// SIGNAL CHAIN: Gate -> DeepVoice (slight) -> Nasal EQ -> Compressor -> Limiter
///
/// CHARACTER REFERENCE: Gru from Despicable Me
/// - Only slightly deeper than normal (not monster-deep)
/// - Very nasal/honky resonance (strong 1-2kHz emphasis)
/// - Dramatic, punchy delivery
/// - Slight thickness without heavy bass
/// </summary>
public class VillainPreset : AudioPresetBase
{
    public VillainPreset()
        : base(
            name: "villain",
            displayName: "Villain",
            description: "Nasal dramatic villain voice (Gru-style)",
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

        // Only slightly deeper - Gru isn't super deep, just thicker
        var deep = new DeepVoiceEffect();
        deep.Prepare(sampleRate);
        deep.SetParameters(new DeepVoiceEffect.DeepVoiceParameters
        {
            PitchSemitones = -1.5f,
            FormantShiftPercent = -5f,
            BassBoostDb = 2f,
            Intensity = 0.85f
        });
        chain.AddEffect(deep);

        // Strong nasal resonance - the key to the Gru sound
        // Boost 1-1.5kHz heavily for nasal/honky quality
        // Cut lows to reduce boominess, slight high presence for drama
        var eq = new ThreeBandEQEffect();
        eq.Prepare(sampleRate);
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = -1f, LowFreq = 200f,
            MidGainDb = 6f, MidFreq = 1200f, MidQ = 2.5f,
            HighGainDb = 1f, HighFreq = 4000f
        });
        chain.AddEffect(eq);

        // Punchy compression for dramatic delivery
        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -16f, Ratio = 5f, AttackMs = 5f, ReleaseMs = 80f, KneeDb = 4f, AutoMakeupGain = true
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
