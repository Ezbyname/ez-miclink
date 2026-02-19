using Android.Media;
using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.Platforms.Android.Services;

public class AudioService : IAudioService
{
    private AudioManager? _audioManager;
    private AudioRecord? _audioRecord;
    private AudioTrack? _audioTrack;
    private bool _isRouting;
    private Thread? _audioThread;
    private bool _shouldStop;

    public bool IsRouting => _isRouting;

    public event EventHandler<string>? StatusChanged;

    public AudioService()
    {
        var context = Platform.CurrentActivity;
        if (context != null)
        {
            _audioManager = (AudioManager?)context.GetSystemService(global::Android.Content.Context.AudioService);
        }
    }

    public async Task<bool> StartAudioRoutingAsync()
    {
        try
        {
            if (_isRouting)
                return true;

            // Set audio mode to communication
            if (_audioManager != null)
            {
                _audioManager.Mode = Mode.InCommunication;
                _audioManager.StartBluetoothSco();
            }

            // Configure audio recording from microphone
            const int sampleRate = 44100;
            const ChannelIn channelConfig = ChannelIn.Mono;
            const Encoding audioFormat = Encoding.Pcm16bit;

            int minBufferSize = AudioRecord.GetMinBufferSize(sampleRate, channelConfig, audioFormat);

            _audioRecord = new AudioRecord(
                AudioSource.Mic,
                sampleRate,
                channelConfig,
                audioFormat,
                minBufferSize * 2
            );

            // Configure audio playback through Bluetooth
            _audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.VoiceCommunication)
                    .SetContentType(AudioContentType.Speech)
                    .Build())
                .SetAudioFormat(new AudioFormat.Builder()
                    .SetEncoding(Encoding.Pcm16bit)
                    .SetSampleRate(sampleRate)
                    .SetChannelMask(ChannelOut.Mono)
                    .Build())
                .SetBufferSizeInBytes(minBufferSize * 2)
                .Build();

            _audioRecord.StartRecording();
            _audioTrack.Play();

            _isRouting = true;
            _shouldStop = false;

            // Start audio routing thread
            _audioThread = new Thread(AudioRoutingLoop);
            _audioThread.Start();

            StatusChanged?.Invoke(this, "Audio routing started");

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error: {ex.Message}");
            return false;
        }
    }

    public async Task StopAudioRoutingAsync()
    {
        _shouldStop = true;
        _isRouting = false;

        _audioThread?.Join(1000);

        _audioRecord?.Stop();
        _audioRecord?.Release();
        _audioRecord = null;

        _audioTrack?.Stop();
        _audioTrack?.Release();
        _audioTrack = null;

        if (_audioManager != null)
        {
            _audioManager.StopBluetoothSco();
            _audioManager.Mode = Mode.Normal;
        }

        StatusChanged?.Invoke(this, "Audio routing stopped");

        await Task.CompletedTask;
    }

    public void SetVolume(double volume)
    {
        // Volume control for audio track (0.0 to 1.0)
        if (_audioTrack != null)
        {
            try
            {
                _audioTrack.SetVolume((float)volume);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] SetVolume error: {ex.Message}");
            }
        }
    }

    private void AudioRoutingLoop()
    {
        var buffer = new byte[1024];

        while (!_shouldStop && _audioRecord != null && _audioTrack != null)
        {
            try
            {
                int bytesRead = _audioRecord.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    _audioTrack.Write(buffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio routing error: {ex.Message}");
                break;
            }
        }
    }
}
