using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Classic robot voice preset.
///
/// GOAL: Mechanical, synthetic, but intelligible voice
/// SIGNAL CHAIN: Gate → Robot Effect → Limiter
///
/// USE CASE:
/// - Robot character voices
/// - Sci-fi effects
/// - AI assistant voice
/// </summary>
public class RobotPreset : AudioPresetBase
{
    public RobotPreset()
        : base(
            name: "robot",
            displayName: "Robot",
            description: "Classic mechanical robot voice with synthetic character",
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
            ThresholdDb = -45f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            FloorGain = -80f,
            KneeDb = 4f
        });
        chain.AddEffect(gate);

        // 2. Robot Effect - Vocoder-style modulation
        var robot = new RobotVoiceEffect();
        robot.Prepare(sampleRate);
        robot.SetParameters(new RobotVoiceEffect.RobotVoiceParameters
        {
            CarrierFrequencyHz = 150f,  // Carrier tone frequency
            Intensity = 0.85f,           // Strong robotic effect
            OctaveShift = 0f             // No pitch shift
        });
        chain.AddEffect(robot);

        // 3. Limiter - Prevent clipping
        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.3f,
            ReleaseMs = 80f,
            LookaheadMs = 2f
        });
        chain.AddEffect(limiter);
    }
}
