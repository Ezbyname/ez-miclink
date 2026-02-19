namespace BluetoothMicrophoneApp.Audio.Effects;

public class EchoEffect : IAudioEffect
{
    public string Name => "Echo";
    public bool Bypass { get; set; }

    private float[] _delayBuffer = Array.Empty<float>();
    private int _delayBufferSize;
    private int _writeIndex;
    private float _feedback = 0.3f;
    private float _mix = 0.3f;

    public void Prepare(int sampleRate, int channels)
    {
        // Default 300ms delay buffer
        _delayBufferSize = (int)(sampleRate * 0.3f * channels);
        _delayBuffer = new float[_delayBufferSize];
        Reset();
    }

    public void Reset()
    {
        Array.Clear(_delayBuffer, 0, _delayBuffer.Length);
        _writeIndex = 0;
    }

    public void Process(AudioBuffer buffer)
    {
        if (Bypass || _delayBuffer.Length == 0) return;

        for (int i = 0; i < buffer.Length; i++)
        {
            float input = buffer.Data[i];
            float delayed = _delayBuffer[_writeIndex];

            // Mix dry + wet
            buffer.Data[i] = input * (1.0f - _mix) + delayed * _mix;

            // Write to delay buffer with feedback
            _delayBuffer[_writeIndex] = input + delayed * _feedback;

            _writeIndex = (_writeIndex + 1) % _delayBufferSize;
        }
    }

    public void SetParameters(Dictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("feedback", out var feedback))
        {
            _feedback = Math.Max(0f, Math.Min(0.85f, Convert.ToSingle(feedback)));
        }

        if (parameters.TryGetValue("mix", out var mix))
        {
            _mix = Math.Max(0f, Math.Min(1f, Convert.ToSingle(mix)));
        }
    }
}
