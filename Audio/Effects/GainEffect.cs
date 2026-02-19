namespace BluetoothMicrophoneApp.Audio.Effects;

public class GainEffect : IAudioEffect
{
    public string Name => "Gain";
    public bool Bypass { get; set; }

    private float _gainLinear = 1.0f;
    private int _sampleRate;
    private int _channels;

    public void Prepare(int sampleRate, int channels)
    {
        _sampleRate = sampleRate;
        _channels = channels;
    }

    public void Reset()
    {
        // No state to reset for gain
    }

    public void Process(AudioBuffer buffer)
    {
        if (Bypass) return;

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer.Data[i] *= _gainLinear;
        }
    }

    public void SetParameters(Dictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("gainDb", out var gainDb))
        {
            float db = Convert.ToSingle(gainDb);
            _gainLinear = (float)Math.Pow(10.0, db / 20.0);
        }
    }
}
