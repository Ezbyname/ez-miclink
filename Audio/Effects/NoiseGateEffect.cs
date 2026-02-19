namespace BluetoothMicrophoneApp.Audio.Effects;

public class NoiseGateEffect : IAudioEffect
{
    public string Name => "NoiseGate";
    public bool Bypass { get; set; }

    public void Prepare(int sampleRate, int channels) { }
    public void Reset() { }
    public void Process(AudioBuffer buffer) { } // TODO: Implement
    public void SetParameters(Dictionary<string, object> parameters) { }
}
