using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Dopey dad / goofy deep voice - subtle and warm.
///
/// SIGNAL CHAIN: Gate -> DeepVoice (-2 semitones, -6% formant, +4dB bass) -> Compressor -> Limiter
///
/// USE CASE:
/// - Goofy dad character
/// - Warm, slightly dumb-sounding voice
/// - Comedy impressions
/// </summary>
public class DopeyDadPreset : AudioPresetBase
{
    public DopeyDadPreset()
        : base(
            name: "dopey_dad",
            displayName: "Dopey Dad",
            description: "Goofy warm dad voice with subtle depth",
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

        // Subtle pitch drop + warm formant shift
        var deep = new DeepVoiceEffect();
        deep.Prepare(sampleRate);
        deep.SetParameters(new DeepVoiceEffect.DeepVoiceParameters
        {
            PitchSemitones = -2f,
            FormantShiftPercent = -6f,
            BassBoostDb = 4f,
            Intensity = 1.0f
        });
        chain.AddEffect(deep);

        // Warm EQ for thick, dopey quality
        var eq = new ThreeBandEQEffect();
        eq.Prepare(sampleRate);
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = 2f, LowFreq = 200f,
            MidGainDb = -1f, MidFreq = 2000f, MidQ = 1.0f,
            HighGainDb = -2f, HighFreq = 6000f
        });
        chain.AddEffect(eq);

        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f, Ratio = 3f, AttackMs = 15f, ReleaseMs = 150f, KneeDb = 8f, AutoMakeupGain = true
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
