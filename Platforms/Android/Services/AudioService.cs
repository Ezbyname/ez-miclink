using Android.Media;
using BluetoothMicrophoneApp.Services;
using BluetoothMicrophoneApp.Audio.DSP;
using Android.Content;

namespace BluetoothMicrophoneApp.Platforms.Android.Services;

public class AudioService : IAudioService
{
    private AudioManager? _audioManager;
    private AudioRecord? _audioRecord;
    private AudioTrack? _audioTrack;
    private bool _isRouting;
    private Thread? _audioThread;
    private bool _shouldStop;
    private AudioEngine _audioEngine;
    private float[] _floatBuffer;
    private readonly object _engineLock = new object();
    private ScoConnectionReceiver? _scoReceiver;

    public bool IsRouting => _isRouting;

    public event EventHandler<string>? StatusChanged;

    public AudioService()
    {
        var context = Platform.CurrentActivity;
        if (context != null)
        {
            _audioManager = (AudioManager?)context.GetSystemService(global::Android.Content.Context.AudioService);
        }
        _audioEngine = new AudioEngine();
        _floatBuffer = Array.Empty<float>();
    }

    public async Task<bool> StartAudioRoutingAsync()
    {
        try
        {
            if (_isRouting)
                return true;

            // Start foreground service to keep app running in background
            var context = Platform.CurrentActivity;
            if (context != null)
            {
                var serviceIntent = new Intent(context, typeof(AudioForegroundService));
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                {
                    context.StartForegroundService(serviceIntent);
                }
                else
                {
                    context.StartService(serviceIntent);
                }
                System.Diagnostics.Debug.WriteLine("[AudioService] Foreground service started");
            }

            // Set audio mode to communication
            if (_audioManager != null)
            {
                _audioManager.Mode = Mode.InCommunication;

                // Register SCO connection receiver
                _scoReceiver = new ScoConnectionReceiver();
                var intentFilter = new IntentFilter();
                intentFilter.AddAction(AudioManager.ActionScoAudioStateUpdated);
                Platform.CurrentActivity?.RegisterReceiver(_scoReceiver, intentFilter);

                // Start Bluetooth SCO
                _audioManager.StartBluetoothSco();

                // Wait for SCO connection (up to 3 seconds)
                System.Diagnostics.Debug.WriteLine("[AudioService] Waiting for Bluetooth SCO connection...");
                bool scoConnected = await _scoReceiver.WaitForConnectionAsync(3000);

                if (!scoConnected)
                {
                    System.Diagnostics.Debug.WriteLine("[AudioService] WARNING: Bluetooth SCO did not connect, audio may route to phone speaker");
                    StatusChanged?.Invoke(this, "Warning: Bluetooth audio connection delayed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AudioService] Bluetooth SCO connected successfully");
                }
            }

            // Configure audio recording from microphone
            const int sampleRate = 44100;
            const ChannelIn channelConfig = ChannelIn.Mono;
            const Encoding audioFormat = Encoding.Pcm16bit;

            int minBufferSize = AudioRecord.GetMinBufferSize(sampleRate, channelConfig, audioFormat);

            System.Diagnostics.Debug.WriteLine("[AudioService] ╔══════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("[AudioService] ║   AUDIO ROUTING CONFIGURATION           ║");
            System.Diagnostics.Debug.WriteLine("[AudioService] ╚══════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine("[AudioService] ");
            System.Diagnostics.Debug.WriteLine("[AudioService] INPUT SOURCE:  Phone Microphone (AudioSource.Mic)");
            System.Diagnostics.Debug.WriteLine("[AudioService] OUTPUT TARGET: Bluetooth Speaker (via SCO)");
            System.Diagnostics.Debug.WriteLine("[AudioService] ");
            System.Diagnostics.Debug.WriteLine("[AudioService] Audio Flow:");
            System.Diagnostics.Debug.WriteLine("[AudioService]   1. Capture from Phone Mic");
            System.Diagnostics.Debug.WriteLine("[AudioService]   2. Process with DSP Effects");
            System.Diagnostics.Debug.WriteLine("[AudioService]   3. Output to Bluetooth Speaker");
            System.Diagnostics.Debug.WriteLine("[AudioService] ");

            _audioRecord = new AudioRecord(
                AudioSource.Mic,  // ← CAPTURES FROM PHONE'S MICROPHONE
                sampleRate,
                channelConfig,
                audioFormat,
                minBufferSize * 2
            );

            System.Diagnostics.Debug.WriteLine("[AudioService] ✓ AudioRecord created: Capturing from phone microphone");

            // Configure audio playback through Bluetooth
            _audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.VoiceCommunication)  // ← Routes to Bluetooth when SCO active
                    .SetContentType(AudioContentType.Speech)
                    .Build())
                .SetAudioFormat(new AudioFormat.Builder()
                    .SetEncoding(Encoding.Pcm16bit)
                    .SetSampleRate(sampleRate)
                    .SetChannelMask(ChannelOut.Mono)
                    .Build())
                .SetBufferSizeInBytes(minBufferSize * 2)
                .Build();

            System.Diagnostics.Debug.WriteLine("[AudioService] ✓ AudioTrack created: Will output to Bluetooth speaker (via SCO)");

            // Initialize audio engine
            _audioEngine.Initialize(sampleRate);
            _audioEngine.SetPreset("clean"); // Start with clean preset

            // Allocate float buffer for DSP processing
            int floatBufferSize = minBufferSize / 2; // PCM16 = 2 bytes per sample
            _floatBuffer = new float[floatBufferSize];

            _audioRecord.StartRecording();
            _audioTrack.Play();

            _isRouting = true;
            _shouldStop = false;

            System.Diagnostics.Debug.WriteLine("[AudioService] ");
            System.Diagnostics.Debug.WriteLine("[AudioService] ✓ AudioRecord started: Now capturing from phone microphone");
            System.Diagnostics.Debug.WriteLine("[AudioService] ✓ AudioTrack started: Now playing to Bluetooth speaker");
            System.Diagnostics.Debug.WriteLine("[AudioService] ✓ Audio routing loop starting...");
            System.Diagnostics.Debug.WriteLine("[AudioService] ");

            // Start audio routing thread
            _audioThread = new Thread(AudioRoutingLoop);
            _audioThread.Start();

            StatusChanged?.Invoke(this, "Routing: Phone Mic → Bluetooth Speaker");

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

        // Unregister SCO receiver
        if (_scoReceiver != null)
        {
            try
            {
                Platform.CurrentActivity?.UnregisterReceiver(_scoReceiver);
            }
            catch { }
            _scoReceiver = null;
        }

        if (_audioManager != null)
        {
            _audioManager.StopBluetoothSco();
            _audioManager.Mode = Mode.Normal;
        }

        // Stop foreground service
        var context = Platform.CurrentActivity;
        if (context != null)
        {
            var serviceIntent = new Intent(context, typeof(AudioForegroundService));
            context.StopService(serviceIntent);
            System.Diagnostics.Debug.WriteLine("[AudioService] Foreground service stopped");
        }

        StatusChanged?.Invoke(this, "Audio routing stopped");

        await Task.CompletedTask;
    }

    public void SetVolume(double volume)
    {
        // Apply volume as digital gain in DSP engine
        // This works for Bluetooth audio (AudioTrack.SetVolume doesn't affect Bluetooth SCO)
        try
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Setting volume to {volume * 100}%");
            lock (_engineLock)
            {
                _audioEngine.SetVolume(volume);
            }
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
            System.Diagnostics.Debug.WriteLine($"[AudioService] Changing effect to: {effectName}");

            // Thread-safe effect switching
            lock (_engineLock)
            {
                _audioEngine.SetPreset(effectName);
            }

            StatusChanged?.Invoke(this, $"Effect changed to: {effectName}");
            System.Diagnostics.Debug.WriteLine($"[AudioService] Effect changed successfully to: {effectName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] SetEffect error: {ex.Message}");
            StatusChanged?.Invoke(this, $"Error setting effect: {ex.Message}");
        }
    }

    public string GetCurrentEffect()
    {
        return _audioEngine.GetCurrentPreset();
    }

    public string[] GetAvailableEffects()
    {
        return new[]
        {
            "clean", "podcast", "stage_mc", "karaoke", "announcer",
            "robot", "megaphone", "stadium", "deep_voice", "chipmunk"
        };
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
                    // Convert PCM16 (byte[]) to float32 (float[])
                    int sampleCount = bytesRead / 2; // 2 bytes per PCM16 sample
                    if (_floatBuffer.Length < sampleCount)
                    {
                        _floatBuffer = new float[sampleCount];
                    }

                    ConvertPCM16ToFloat(buffer, _floatBuffer, sampleCount);

                    // Process through DSP engine (thread-safe)
                    lock (_engineLock)
                    {
                        _audioEngine.ProcessBuffer(_floatBuffer, 0, sampleCount);
                    }

                    // Convert float32 back to PCM16
                    ConvertFloatToPCM16(_floatBuffer, buffer, sampleCount);

                    // Write processed audio to output
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

    /// <summary>
    /// Convert PCM16 (signed 16-bit integer) to float32 normalized to [-1.0, 1.0]
    /// </summary>
    private static void ConvertPCM16ToFloat(byte[] pcm16Buffer, float[] floatBuffer, int sampleCount)
    {
        for (int i = 0; i < sampleCount; i++)
        {
            // Read 16-bit signed integer (little-endian)
            short sample = (short)(pcm16Buffer[i * 2] | (pcm16Buffer[i * 2 + 1] << 8));

            // Normalize to [-1.0, 1.0]
            floatBuffer[i] = sample / 32768f;
        }
    }

    /// <summary>
    /// Convert float32 normalized [-1.0, 1.0] back to PCM16 (signed 16-bit integer)
    /// </summary>
    private static void ConvertFloatToPCM16(float[] floatBuffer, byte[] pcm16Buffer, int sampleCount)
    {
        for (int i = 0; i < sampleCount; i++)
        {
            // Clamp to [-1.0, 1.0] and convert to 16-bit integer
            float clampedSample = Math.Clamp(floatBuffer[i], -1f, 1f);
            short sample = (short)(clampedSample * 32767f);

            // Write as little-endian bytes
            pcm16Buffer[i * 2] = (byte)(sample & 0xFF);
            pcm16Buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }
    }

    /// <summary>
    /// BroadcastReceiver to detect when Bluetooth SCO audio connection is established.
    /// </summary>
    private class ScoConnectionReceiver : BroadcastReceiver
    {
        private TaskCompletionSource<bool>? _connectionTask;
        private readonly object _lock = new object();

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != AudioManager.ActionScoAudioStateUpdated)
                return;

            int state = intent.GetIntExtra(AudioManager.ExtraScoAudioState, -1);
            System.Diagnostics.Debug.WriteLine($"[ScoReceiver] SCO state changed: {state}");

            lock (_lock)
            {
                if (_connectionTask != null && !_connectionTask.Task.IsCompleted)
                {
                    if (state == (int)ScoAudioState.Connected)
                    {
                        System.Diagnostics.Debug.WriteLine("[ScoReceiver] SCO Connected!");
                        _connectionTask.TrySetResult(true);
                    }
                    else if (state == (int)ScoAudioState.Disconnected)
                    {
                        System.Diagnostics.Debug.WriteLine("[ScoReceiver] SCO Disconnected");
                    }
                }
            }
        }

        public Task<bool> WaitForConnectionAsync(int timeoutMs)
        {
            lock (_lock)
            {
                _connectionTask = new TaskCompletionSource<bool>();

                // Set timeout
                Task.Delay(timeoutMs).ContinueWith(_ =>
                {
                    lock (_lock)
                    {
                        if (_connectionTask != null && !_connectionTask.Task.IsCompleted)
                        {
                            System.Diagnostics.Debug.WriteLine("[ScoReceiver] Connection timeout");
                            _connectionTask.TrySetResult(false);
                        }
                    }
                });

                return _connectionTask.Task;
            }
        }
    }
}
