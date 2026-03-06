using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Grumpy, raspy, annoyed cat-like voice.
///
/// SIGNAL CHAIN: Gate -> DeepVoice (-4 semitones, -10% formant) -> Megaphone (rasp) -> EQ -> Compressor -> Limiter
///
/// USE CASE:
/// - Grumpy cat character
/// - Annoyed/sarcastic voice
/// - Old grumpy character
/// </summary>
public class GrumpyCatPreset : AudioPresetBase
{
    public GrumpyCatPreset()
        : base(
            name: "grumpy_cat",
            displayName: "Grumpy Cat",
            description: "Raspy annoyed grumpy cat character voice",
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

        // Deep grumpy pitch + formant shift
        var deep = new DeepVoiceEffect();
        deep.Prepare(sampleRate);
        deep.SetParameters(new DeepVoiceEffect.DeepVoiceParameters
        {
            PitchSemitones = -4f,
            FormantShiftPercent = -10f,
            BassBoostDb = 5f,
            Intensity = 1.0f
        });
        chain.AddEffect(deep);

        // More distortion for raspy/growly quality
        var megaphone = new MegaphoneEffect();
        megaphone.Prepare(sampleRate);
        megaphone.SetParameters(new MegaphoneEffect.MegaphoneParameters
        {
            LowCutoffHz = 120f,
            HighCutoffHz = 5000f,
            Distortion = 0.3f,
            MidBoostDb = 3f
        });
        chain.AddEffect(megaphone);

        // Emphasis on low-mids for grumpy rumble
        var eq = new ThreeBandEQEffect();
        eq.Prepare(sampleRate);
        eq.SetParameters(new ThreeBandEQEffect.ThreeBandEQParameters
        {
            LowGainDb = 3f, LowFreq = 180f,
            MidGainDb = 2f, MidFreq = 600f, MidQ = 1.5f,
            HighGainDb = -3f, HighFreq = 4000f
        });
        chain.AddEffect(eq);

        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -18f, Ratio = 4f, AttackMs = 10f, ReleaseMs = 120f, KneeDb = 6f, AutoMakeupGain = true
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
