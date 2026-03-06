using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Extremely high-pitched squeaky cartoon character voice.
///
/// SIGNAL CHAIN: Gate -> Helium (+9 semitones, +22% formant) -> Megaphone (slight distortion) -> Limiter
///
/// USE CASE:
/// - Cartoon mouse/bird character
/// - Squeaky toy voice
/// - Kids' content
/// </summary>
public class SqueakyCartoonPreset : AudioPresetBase
{
    public SqueakyCartoonPreset()
        : base(
            name: "squeaky_cartoon",
            displayName: "Squeaky Cartoon",
            description: "Ultra-high squeaky cartoon character voice",
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

        // Very high pitch + formant for squeaky character
        var helium = new HeliumVoiceEffect();
        helium.Prepare(sampleRate);
        helium.SetParameters(new HeliumVoiceEffect.HeliumParameters
        {
            PitchSemitones = 9f,
            FormantShiftPercent = 22f,
            BrightnessDb = 6f,
            Intensity = 1.0f
        });
        chain.AddEffect(helium);

        // Slight megaphone distortion for cartoon crackle
        var megaphone = new MegaphoneEffect();
        megaphone.Prepare(sampleRate);
        megaphone.SetParameters(new MegaphoneEffect.MegaphoneParameters
        {
            LowCutoffHz = 300f,
            HighCutoffHz = 6000f,
            Distortion = 0.15f,
            MidBoostDb = 2f
        });
        chain.AddEffect(megaphone);

        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f, AttackMs = 0.5f, ReleaseMs = 100f, LookaheadMs = 3f
        });
        chain.AddEffect(limiter);
    }
}
