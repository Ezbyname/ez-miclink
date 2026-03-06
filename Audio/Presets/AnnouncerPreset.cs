using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Professional announcer voice - deep, authoritative, broadcast quality.
/// </summary>
public class AnnouncerPreset : AudioPresetBase
{
    public AnnouncerPreset()
        : base(
            name: "announcer",
            displayName: "Announcer",
            description: "Deep authoritative broadcast announcer voice",
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
            ThresholdDb = -42f, AttackMs = 1f, ReleaseMs = 120f, FloorGain = -80f, KneeDb = 6f
        });
        chain.AddEffect(gate);

        var eq = new ThreeBandEQEffect();
        eq.Prepare(sampleRate);
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = 3f, LowFreq = 120f,
            MidGainDb = 0f, MidFreq = 1000f, MidQ = 1.0f,
            HighGainDb = 2f, HighFreq = 6000f
        });
        chain.AddEffect(eq);

        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f, Ratio = 4f, AttackMs = 10f, ReleaseMs = 120f, KneeDb = 8f, AutoMakeupGain = true
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
