using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Karaoke vocal processing with reverb.
/// Goal: Spacious, flattering vocal sound.
/// </summary>
public class KaraokePreset : AudioPresetBase
{
    public KaraokePreset()
        : base(
            name: "karaoke",
            displayName: "Karaoke",
            description: "Spacious vocal with reverb for singing",
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
            ThresholdDb = -50f, AttackMs = 2f, ReleaseMs = 200f, FloorGain = -70f, KneeDb = 10f
        });
        chain.AddEffect(gate);

        var eq = new ThreeBandEQEffect();
        eq.Prepare(sampleRate);
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = -1f, LowFreq = 120f,
            MidGainDb = 2f, MidFreq = 1000f, MidQ = 1.2f,
            HighGainDb = 2f, HighFreq = 5000f
        });
        chain.AddEffect(eq);

        var karaoke = new KaraokeEffect();
        karaoke.Prepare(sampleRate);
        karaoke.SetParameters(new KaraokeEffect.KaraokeParameters
        {
            RoomSize = 0.7f, DecayTime = 0.9f, Damping = 0.6f, Mix = 0.35f,
            CompressionThreshold = -18f, PresenceBoost = 3f
        });
        chain.AddEffect(karaoke);

        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f, AttackMs = 0.5f, ReleaseMs = 100f, LookaheadMs = 3f
        });
        chain.AddEffect(limiter);
    }
}
