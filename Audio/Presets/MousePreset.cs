using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Extreme high-pitched mouse squeak voice.
///
/// SIGNAL CHAIN: Gate -> Helium (+11 semitones, +30% formant, +8dB brightness) -> Compressor -> Limiter
///
/// USE CASE:
/// - Mouse character (Mickey-style)
/// - Tiny creature voice
/// - Comedy/kids content
/// </summary>
public class MousePreset : AudioPresetBase
{
    public MousePreset()
        : base(
            name: "mouse",
            displayName: "Mouse Squeak",
            description: "Tiny ultra-high mouse character voice",
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

        // Extreme high pitch + extreme formant shift
        var helium = new HeliumVoiceEffect();
        helium.Prepare(sampleRate);
        helium.SetParameters(new HeliumVoiceEffect.HeliumParameters
        {
            PitchSemitones = 11f,
            FormantShiftPercent = 30f,
            BrightnessDb = 8f,
            Intensity = 1.0f
        });
        chain.AddEffect(helium);

        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -15f, Ratio = 4f, AttackMs = 5f, ReleaseMs = 80f, KneeDb = 6f, AutoMakeupGain = true
        });
        chain.AddEffect(compressor);

        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f, AttackMs = 0.3f, ReleaseMs = 80f, LookaheadMs = 2f
        });
        chain.AddEffect(limiter);
    }
}
