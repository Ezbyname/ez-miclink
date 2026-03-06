using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Stage MC / announcer with megaphone character.
/// Goal: Loud, present, cuts through crowd noise.
/// </summary>
public class StageMCPreset : AudioPresetBase
{
    public StageMCPreset()
        : base(
            name: "stage_mc",
            displayName: "Stage MC",
            description: "Loud stage announcer voice that cuts through noise",
            category: "Professional",
            isPremium: false)
    {
    }

    public override void Configure(AudioEffectChain chain, int sampleRate)
    {
        var gate = new NoiseGateEffect();
        gate.Prepare(sampleRate);
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -40f, AttackMs = 0.5f, ReleaseMs = 100f, FloorGain = -80f, KneeDb = 4f
        });
        chain.AddEffect(gate);

        var megaphone = new MegaphoneEffect();
        megaphone.Prepare(sampleRate);
        megaphone.SetParameters(new MegaphoneEffect.MegaphoneParameters
        {
            LowCutoffHz = 450f, HighCutoffHz = 3500f, Distortion = 0.4f, MidBoostDb = 4f
        });
        chain.AddEffect(megaphone);

        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -15f, Ratio = 6f, AttackMs = 5f, ReleaseMs = 80f, KneeDb = 4f, AutoMakeupGain = true
        });
        chain.AddEffect(compressor);

        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.3f, AttackMs = 0.3f, ReleaseMs = 80f, LookaheadMs = 4f
        });
        chain.AddEffect(limiter);
    }
}
