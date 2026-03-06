using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Stadium/arena reverb effect with extended decay and echo.
/// Goal: Large venue space simulation.
/// </summary>
public class StadiumPreset : AudioPresetBase
{
    public StadiumPreset()
        : base(
            name: "stadium",
            displayName: "Stadium",
            description: "Large arena reverb with echo reflections",
            category: "Voice Effects",
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

        var eq = new ThreeBandEQEffect();
        eq.Prepare(sampleRate);
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = -1f, LowFreq = 100f,
            MidGainDb = 0f, MidFreq = 1000f, MidQ = 1.0f,
            HighGainDb = 2f, HighFreq = 4000f
        });
        chain.AddEffect(eq);

        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f, Ratio = 3f, AttackMs = 15f, ReleaseMs = 150f, KneeDb = 8f, AutoMakeupGain = true
        });
        chain.AddEffect(compressor);

        var stadium = new KaraokeEffect();
        stadium.Prepare(sampleRate);
        stadium.SetParameters(new KaraokeEffect.KaraokeParameters
        {
            RoomSize = 1.0f, DecayTime = 3.5f, Damping = 0.4f, Mix = 0.45f,
            CompressionThreshold = -20f, PresenceBoost = 2f
        });
        chain.AddEffect(stadium);

        var echo = new EchoDelayEffect();
        echo.Prepare(sampleRate);
        echo.SetParameters(new EchoDelayEffect.EchoDelayParameters
        {
            DelayMs = 450f, Feedback = 0.45f, Mix = 0.4f, Damping = 0.4f
        });
        chain.AddEffect(echo);

        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f, AttackMs = 0.5f, ReleaseMs = 100f, LookaheadMs = 3f
        });
        chain.AddEffect(limiter);
    }
}
