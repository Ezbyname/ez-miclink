using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Dark villain voice with gravelly undertone.
///
/// SIGNAL CHAIN: Gate -> DeepVoice (-3.5 semitones, -10% formant) -> Megaphone (gravel) -> EQ -> Compressor -> Limiter
///
/// USE CASE:
/// - Movie villain character
/// - Sinister/menacing voice
/// - Dark lord impressions
/// </summary>
public class VillainPreset : AudioPresetBase
{
    public VillainPreset()
        : base(
            name: "villain",
            displayName: "Villain",
            description: "Dark sinister villain voice with gravelly tone",
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

        // Deep menacing pitch + formant shift
        var deep = new DeepVoiceEffect();
        deep.Prepare(sampleRate);
        deep.SetParameters(new DeepVoiceEffect.DeepVoiceParameters
        {
            PitchSemitones = -3.5f,
            FormantShiftPercent = -10f,
            BassBoostDb = 4f,
            Intensity = 1.0f
        });
        chain.AddEffect(deep);

        // Slight distortion for gravelly quality
        var megaphone = new MegaphoneEffect();
        megaphone.Prepare(sampleRate);
        megaphone.SetParameters(new MegaphoneEffect.MegaphoneParameters
        {
            LowCutoffHz = 100f,
            HighCutoffHz = 6000f,
            Distortion = 0.2f,
            MidBoostDb = 2f
        });
        chain.AddEffect(megaphone);

        // Dark EQ - cut highs, boost low-mids
        var eq = new ThreeBandEQEffect();
        eq.Prepare(sampleRate);
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = 3f, LowFreq = 150f,
            MidGainDb = 1f, MidFreq = 800f, MidQ = 1.2f,
            HighGainDb = -2f, HighFreq = 5000f
        });
        chain.AddEffect(eq);

        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f, Ratio = 4f, AttackMs = 10f, ReleaseMs = 120f, KneeDb = 6f, AutoMakeupGain = true
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
