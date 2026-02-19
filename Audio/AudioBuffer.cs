namespace BluetoothMicrophoneApp.Audio;

/// <summary>
/// Audio buffer for real-time processing (float32 [-1..1])
/// </summary>
public class AudioBuffer
{
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int Frames { get; set; }
    public float[] Data { get; set; }

    public AudioBuffer(int sampleRate, int channels, int frames)
    {
        SampleRate = sampleRate;
        Channels = channels;
        Frames = frames;
        Data = new float[frames * channels];
    }

    public int Length => Data.Length;

    public void Clear()
    {
        Array.Clear(Data, 0, Data.Length);
    }

    /// <summary>
    /// Convert int16 PCM to float32 [-1..1]
    /// </summary>
    public static void Int16ToFloat(byte[] int16Data, float[] floatData, int frames, int channels)
    {
        int sampleCount = frames * channels;
        for (int i = 0; i < sampleCount; i++)
        {
            short sample = (short)(int16Data[i * 2] | (int16Data[i * 2 + 1] << 8));
            floatData[i] = sample / 32768.0f;
        }
    }

    /// <summary>
    /// Convert float32 [-1..1] to int16 PCM
    /// </summary>
    public static void FloatToInt16(float[] floatData, byte[] int16Data, int frames, int channels)
    {
        int sampleCount = frames * channels;
        for (int i = 0; i < sampleCount; i++)
        {
            // Clamp to prevent overflow
            float clamped = Math.Max(-1.0f, Math.Min(1.0f, floatData[i]));
            short sample = (short)(clamped * 32767.0f);
            int16Data[i * 2] = (byte)(sample & 0xFF);
            int16Data[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }
    }
}
