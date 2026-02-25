using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Deep voice effect with pitch and formant shifting.
///
/// GOAL: Lower, fuller voice with natural character
/// SIGNAL CHAIN: Gate → Deep Voice Effect → Compressor → Limiter
///
/// USE CASE:
/// - Deeper, more authoritative voice
/// - Masculine voice enhancement
/// - Movie trailer voice
/// </summary>
public class DeepVoicePreset : AudioPresetBase
{
    public DeepVoicePreset()
        : base(
            name: "deep_voice",
            displayName: "Deep Voice",
            description: "Lower pitch with fuller, more authoritative tone",
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

        // 2. Deep Voice Effect (pitch + formant shift)
        var deepVoice = new DeepVoiceEffect();
        deepVoice.Prepare(sampleRate);
        deepVoice.SetParameters(new DeepVoiceEffect.DeepVoiceParameters
        {
            PitchSemitones = -4f,      // Down 4 semitones
            FormantShiftPercent = -8f,  // Lower formants 8%
            BassBoostDb = 4f,           // Add warmth
            Intensity = 1.0f            // Full effect
        });
        chain.AddEffect(deepVoice);

        // 3. Compressor
        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f,
            Ratio = 3f,
            AttackMs = 15f,
            ReleaseMs = 150f,
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
