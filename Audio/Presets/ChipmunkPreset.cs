using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Chipmunk/Helium voice effect with pitch and formant shifting.
///
/// GOAL: Higher, squeaky voice characteristic
/// SIGNAL CHAIN: Gate → Helium Effect → Compressor → Limiter
///
/// USE CASE:
/// - Fun, cartoonish voice
/// - Helium balloon effect
/// - Chipmunk character voice
/// - Comedy content
/// </summary>
public class ChipmunkPreset : AudioPresetBase
{
    public ChipmunkPreset()
        : base(
            name: "chipmunk",
            displayName: "Chipmunk",
            description: "High-pitched squeaky voice like helium balloon",
            category: "Voice Effects",
            isPremium: false)
    {
    }

    public override void Configure(AudioEffectChain chain, int sampleRate)
    {
        // 1. Noise Gate
        var gate = new NoiseGateEffect();
        gate.Prepare(sampleRate);
        gate.SetParameters(new NoiseGateEffect.NoiseGateParameters
        {
            ThresholdDb = -45f,
            AttackMs = 1f,
            ReleaseMs = 150f,
            FloorGain = -80f,
            KneeDb = 6f
        });
        chain.AddEffect(gate);

        // 2. Helium Voice Effect (pitch + formant shift)
        var helium = new HeliumVoiceEffect();
        helium.Prepare(sampleRate);
        helium.SetParameters(new HeliumVoiceEffect.HeliumParameters
        {
            PitchSemitones = 5f,        // Up 5 semitones
            FormantShiftPercent = 15f,   // Raise formants 15%
            BrightnessDb = 4f,           // Add sparkle
            Intensity = 1.0f             // Full effect
        });
        chain.AddEffect(helium);

        // 3. Compressor
        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f,
            Ratio = 3f,
            AttackMs = 10f,
            ReleaseMs = 100f,
            KneeDb = 8f,
            AutoMakeupGain = true
        });
        chain.AddEffect(compressor);

        // 4. Limiter
        var limiter = new LimiterEffect();
        limiter.Prepare(sampleRate);
        limiter.SetParameters(new LimiterEffect.LimiterParameters
        {
            CeilingDb = -0.5f,
            AttackMs = 0.5f,
            ReleaseMs = 100f,
            LookaheadMs = 3f
        });
        chain.AddEffect(limiter);
    }
}
