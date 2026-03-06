using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Slow, deep, dopey giant character voice.
///
/// SIGNAL CHAIN: Gate -> DeepVoice (-6 semitones, -12% formant, +6dB bass) -> Compressor -> Limiter
///
/// USE CASE:
/// - Giant/ogre character
/// - Slow-witted sidekick
/// - Fantasy character voices
/// </summary>
public class DopeyGiantPreset : AudioPresetBase
{
    public DopeyGiantPreset()
        : base(
            name: "dopey_giant",
            displayName: "Dopey Giant",
            description: "Deep slow giant/ogre character voice",
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

        // Very deep pitch + heavy formant shift for giant voice
        var deep = new DeepVoiceEffect();
        deep.Prepare(sampleRate);
        deep.SetParameters(new DeepVoiceEffect.DeepVoiceParameters
        {
            PitchSemitones = -6f,
            FormantShiftPercent = -12f,
            BassBoostDb = 6f,
            Intensity = 1.0f
        });
        chain.AddEffect(deep);

        var compressor = new CompressorEffect();
        compressor.Prepare(sampleRate);
        compressor.SetParameters(new CompressorEffect.CompressorParameters
        {
            ThresholdDb = -20f, Ratio = 4f, AttackMs = 20f, ReleaseMs = 200f, KneeDb = 8f, AutoMakeupGain = true
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
