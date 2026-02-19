namespace BluetoothMicrophoneApp.Audio.Effects;

public class EQ3BandEffect : IAudioEffect
{
    public string Name => "EQ";
    public bool Bypass { get; set; }

    public void Prepare(int sampleRate, int channels) { }
    public void Reset() { }
    public void Process(AudioBuffer buffer) { } // TODO: Implement biquad filters
    public void SetParameters(Dictionary<string, object> parameters) { }
}
