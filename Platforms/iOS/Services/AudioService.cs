using AVFoundation;
using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.Platforms.iOS.Services;

public class AudioService : IAudioService
{
    private AVAudioEngine? _audioEngine;
    private AVAudioInputNode? _inputNode;
    private AVAudioMixerNode? _mixerNode;
    private bool _isRouting;

    public bool IsRouting => _isRouting;

    public event EventHandler<string>? StatusChanged;

    public async Task<bool> StartAudioRoutingAsync()
    {
        try
        {
            if (_isRouting)
                return true;

            var audioSession = AVAudioSession.SharedInstance();

            // Configure audio session for Bluetooth and microphone
            var error = audioSession.SetCategory(
                AVAudioSessionCategory.PlayAndRecord,
                AVAudioSessionCategoryOptions.AllowBluetooth |
                AVAudioSessionCategoryOptions.DefaultToSpeaker
            );

            if (error != null)
            {
                StatusChanged?.Invoke(this, $"Error setting audio category: {error.Description}");
                return false;
            }

            error = audioSession.SetMode(AVAudioSession.ModeVoiceChat, out _);
            if (error != null)
            {
                StatusChanged?.Invoke(this, $"Error setting audio mode: {error.Description}");
                return false;
            }

            error = audioSession.SetActive(true);
            if (error != null)
            {
                StatusChanged?.Invoke(this, $"Error activating audio session: {error.Description}");
                return false;
            }

            // Configure audio engine
            _audioEngine = new AVAudioEngine();
            _inputNode = _audioEngine.InputNode;
            _mixerNode = _audioEngine.MainMixerNode;

            var inputFormat = _inputNode.GetBusOutputFormat(0);

            // Install tap to route microphone input to output
            _inputNode.InstallTapOnBus(0, 4096, inputFormat, (buffer, when) =>
            {
                // Route audio through mixer
                _mixerNode?.RenderAudioAndRender(ref buffer.MutableAudioBufferList, when);
            });

            // Connect input to mixer to output
            _audioEngine.Connect(_inputNode, _mixerNode, inputFormat);

            _audioEngine.Prepare();
            _audioEngine.StartAndReturnError(out error);

            if (error != null)
            {
                StatusChanged?.Invoke(this, $"Error starting audio engine: {error.Description}");
                return false;
            }

            _isRouting = true;
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
        _audioEngine?.Stop();
        _inputNode?.RemoveTapOnBus(0);

        _audioEngine?.Dispose();
        _audioEngine = null;
        _inputNode = null;
        _mixerNode = null;

        var audioSession = AVAudioSession.SharedInstance();
        audioSession.SetActive(false);

        _isRouting = false;
        StatusChanged?.Invoke(this, "Audio routing stopped");

        await Task.CompletedTask;
    }

    public void SetVolume(double volume)
    {
        // Volume control for mixer node (0.0 to 1.0)
        if (_mixerNode != null)
        {
            try
            {
                _mixerNode.Volume = (float)volume;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] SetVolume error: {ex.Message}");
            }
        }
    }
}
