using AVFoundation;
using BluetoothMicrophoneApp.Services;
using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Platforms.iOS.Services;

public class AudioService : IAudioService
{
    private AVAudioEngine? _audioEngine;
    private AVAudioInputNode? _inputNode;
    private AVAudioMixerNode? _mixerNode;
    private bool _isRouting;
    private AudioEngine _dspEngine;
    private float[] _floatBuffer;

    public bool IsRouting => _isRouting;

    public event EventHandler<string>? StatusChanged;

    public AudioService()
    {
        _dspEngine = new AudioEngine();
        _floatBuffer = Array.Empty<float>();
    }

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

            // Initialize DSP engine with sample rate
            int sampleRate = (int)inputFormat.SampleRate;
            _dspEngine.Initialize(sampleRate);
            _dspEngine.SetPreset("clean"); // Start with clean preset

            // Allocate float buffer for DSP processing
            uint bufferSize = 4096;
            _floatBuffer = new float[bufferSize];

            // Install tap to route microphone input through DSP to output
            _inputNode.InstallTapOnBus(0, bufferSize, inputFormat, (buffer, when) =>
            {
                try
                {
                    // Process audio through DSP engine
                    ProcessAudioBuffer(buffer);

                    // Route processed audio through mixer
                    _mixerNode?.RenderAudioAndRender(ref buffer.MutableAudioBufferList, when);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[iOS AudioService] Tap error: {ex.Message}");
                }
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
        // Apply volume as digital gain in DSP engine
        // This ensures consistent volume control across both platforms
        try
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Setting volume to {volume * 100}%");
            _dspEngine.SetVolume(volume);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] SetVolume error: {ex.Message}");
        }
    }

    public void SetEffect(string effectName)
    {
        try
        {
            _dspEngine.SetPreset(effectName);
            StatusChanged?.Invoke(this, $"Effect changed to: {effectName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] SetEffect error: {ex.Message}");
            StatusChanged?.Invoke(this, $"Error setting effect: {ex.Message}");
        }
    }

    public string GetCurrentEffect()
    {
        return _dspEngine.GetCurrentPreset();
    }

    public string[] GetAvailableEffects()
    {
        return new[]
        {
            "clean", "podcast", "stage_mc", "karaoke", "announcer",
            "robot", "megaphone", "stadium", "deep_voice", "chipmunk"
        };
    }

    private unsafe void ProcessAudioBuffer(AVAudioPCMBuffer buffer)
    {
        // Get audio buffer info
        var audioBufferList = buffer.AudioBufferList;
        int channelCount = (int)buffer.Format.ChannelCount;
        int frameCount = (int)buffer.FrameLength;

        // iOS typically uses Float32 audio format
        // Process each channel (mono or stereo)
        for (int channel = 0; channel < channelCount; channel++)
        {
            var audioBuffer = audioBufferList[channel];
            float* data = (float*)audioBuffer.Data;

            if (data == null)
                continue;

            // Resize float buffer if needed
            if (_floatBuffer.Length < frameCount)
            {
                _floatBuffer = new float[frameCount];
            }

            // Copy from native buffer to managed array
            for (int i = 0; i < frameCount; i++)
            {
                _floatBuffer[i] = data[i];
            }

            // Process through DSP engine
            _dspEngine.ProcessBuffer(_floatBuffer, 0, frameCount);

            // Copy back to native buffer
            for (int i = 0; i < frameCount; i++)
            {
                data[i] = _floatBuffer[i];
            }
        }
    }
}
