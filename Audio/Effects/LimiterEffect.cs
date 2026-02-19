namespace BluetoothMicrophoneApp.Audio.Effects;

public class LimiterEffect : IAudioEffect
{
    public string Name => "Limiter";
    public bool Bypass { get; set; }

    private float _ceilingLinear = 1.0f;

    public void Prepare(int sampleRate, int channels) { }
    public void Reset() { }

    public void Process(AudioBuffer buffer)
    {
        if (Bypass) return;

        // Simple hard limiter (prevent clipping)
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer.Data[i] = Math.Max(-_ceilingLinear, Math.Min(_ceilingLinear, buffer.Data[i]));
        }
    }

    public void SetParameters(Dictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("ceilingDb", out var ceilingDb))
        {
            float db = Convert.ToSingle(ceilingDb);
            _ceilingLinear = (float)Math.Pow(10.0, db / 20.0);
        }
    }
}
