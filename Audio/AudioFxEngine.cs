using System.Text.Json;

namespace BluetoothMicrophoneApp.Audio;

/// <summary>
/// Real-time audio effects engine with preset support
/// </summary>
public class AudioFxEngine
{
    private List<IAudioEffect> _effectChain = new();
    private int _sampleRate;
    private int _channels;
    private bool _isPrepared;

    public List<IAudioEffect> EffectChain => _effectChain;

    public void Prepare(int sampleRate, int channels)
    {
        _sampleRate = sampleRate;
        _channels = channels;

        foreach (var effect in _effectChain)
        {
            effect.Prepare(sampleRate, channels);
        }

        _isPrepared = true;
    }

    public void SetChain(List<IAudioEffect> effects)
    {
        _effectChain = new List<IAudioEffect>(effects);

        if (_isPrepared)
        {
            foreach (var effect in _effectChain)
            {
                effect.Prepare(_sampleRate, _channels);
            }
        }
    }

    public void AddEffect(IAudioEffect effect)
    {
        _effectChain.Add(effect);

        if (_isPrepared)
        {
            effect.Prepare(_sampleRate, _channels);
        }
    }

    public void ClearChain()
    {
        _effectChain.Clear();
    }

    public void Reset()
    {
        foreach (var effect in _effectChain)
        {
            effect.Reset();
        }
    }

    /// <summary>
    /// Process audio through the effect chain (real-time safe)
    /// </summary>
    public void Process(AudioBuffer buffer)
    {
        if (!_isPrepared)
            return;

        foreach (var effect in _effectChain)
        {
            if (!effect.Bypass)
            {
                effect.Process(buffer);
            }
        }
    }

    /// <summary>
    /// Apply a preset by name
    /// </summary>
    public void ApplyPreset(AudioPreset preset)
    {
        if (preset == null)
            return;

        // Clear current chain
        ClearChain();

        // Build new chain from preset
        foreach (var effectName in preset.Chain)
        {
            var effect = CreateEffect(effectName);
            if (effect != null)
            {
                AddEffect(effect);

                // Apply parameters if available
                if (preset.Params != null && preset.Params.ContainsKey(effectName))
                {
                    effect.SetParameters(preset.Params[effectName]);
                }
            }
        }
    }

    private IAudioEffect? CreateEffect(string name)
    {
        return name switch
        {
            "Gain" => new Effects.GainEffect(),
            "NoiseGate" => new Effects.NoiseGateEffect(),
            "EQ" => new Effects.EQ3BandEffect(),
            "Compressor" => new Effects.CompressorEffect(),
            "Limiter" => new Effects.LimiterEffect(),
            "Echo" => new Effects.EchoEffect(),
            _ => null
        };
    }
}
