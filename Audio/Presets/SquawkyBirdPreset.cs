using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Squawky bird/duck character voice.
///
/// SIGNAL CHAIN: Gate -> Helium (+4 semitones, +25% formant) -> Robot (ring mod) -> Megaphone (distortion) -> Limiter
///
/// USE CASE:
/// - Duck/parrot character
/// - Angry bird voice
/// - Comedy animal impressions
/// </summary>
public class SquawkyBirdPreset : AudioPresetBase
{
    public SquawkyBirdPreset()
        : base(
            name: "squawky_bird",
            displayName: "Squawky Bird",
            description: "Quacking duck or squawking parrot voice",
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
            ThresholdDb = -45f, AttackMs = 0.5f, ReleaseMs = 100f, FloorGain = -80f, KneeDb = 4f
        });
        chain.AddEffect(gate);

        // Medium-high pitch + very high formant for bird-like quality
        var helium = new HeliumVoiceEffect();
        helium.Prepare(sampleRate);
        helium.SetParameters(new HeliumVoiceEffect.HeliumParameters
        {
            PitchSemitones = 4f,
            FormantShiftPercent = 25f,
            BrightnessDb = 3f,
            Intensity = 1.0f
        });
        chain.AddEffect(helium);

        // Light ring modulation for quacking/squawking character
        var robot = new RobotVoiceEffect();
        robot.Prepare(sampleRate);
        robot.SetParameters(new RobotVoiceEffect.RobotVoiceParameters
        {
            CarrierFrequencyHz = 220f,
            Intensity = 0.4f,
            OctaveShift = 0f
        });
        chain.AddEffect(robot);

        // Distortion for harsh bird squawk
        var megaphone = new MegaphoneEffect();
        megaphone.Prepare(sampleRate);
        megaphone.SetParameters(new MegaphoneEffect.MegaphoneParameters
        {
            LowCutoffHz = 350f,
            HighCutoffHz = 5000f,
            Distortion = 0.5f,
            MidBoostDb = 3f
        });
        chain.AddEffect(megaphone);

        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f, AttackMs = 0.3f, ReleaseMs = 80f, LookaheadMs = 2f
        });
        chain.AddEffect(limiter);
    }
}
