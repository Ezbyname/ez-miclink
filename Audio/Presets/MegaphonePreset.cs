using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Megaphone / loudspeaker effect preset.
///
/// GOAL: Lo-fi, distorted, loudspeaker character
/// SIGNAL CHAIN: Gate → Megaphone Effect → Limiter
///
/// USE CASE:
/// - Stadium announcements
/// - Protest megaphone
/// - Emergency broadcasts
/// - Retro radio effect
/// </summary>
public class MegaphonePreset : AudioPresetBase
{
    public MegaphonePreset()
        : base(
            name: "megaphone",
            displayName: "Megaphone",
            description: "Lo-fi distorted loudspeaker effect",
            category: "Voice Effects",
            isPremium: false)
    {
    }

    public override void Configure(AudioEffectChain chain, int sampleRate)
    {
        // 1. Noise Gate - Remove background noise
        var gate = new NoiseGateEffect();
        gate.Prepare(sampleRate);
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -40f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            FloorGain = -80f,
            KneeDb = 4f
        });
        chain.AddEffect(gate);

        // 2. Megaphone Effect - Band-limited distortion
        var megaphone = new MegaphoneEffect();
        megaphone.Prepare(sampleRate);
        megaphone.SetParameters(new MegaphoneEffect.MegaphoneParameters
        {
            LowCutoffHz = 400f,      // Remove low frequencies
            HighCutoffHz = 3000f,    // Remove high frequencies
            Distortion = 0.6f,       // Moderate distortion
            MidBoostDb = 4f          // Boost midrange presence
        });
        chain.AddEffect(megaphone);

        // 3. Limiter - Prevent clipping
        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -1f,
            AttackMs = 0.5f,
            ReleaseMs = 50f,
            LookaheadMs = 3f
        });
        chain.AddEffect(limiter);
    }
}
