using Android.Media;
using BluetoothMicrophoneApp.Services;
using BluetoothMicrophoneApp.Audio.DSP;
using Android.Content;
using System.Buffers;

namespace BluetoothMicrophoneApp.Platforms.Android.Services;

/// <summary>
/// Android audio service with proper resource management and async patterns.
/// Implements IDisposable for cleanup of unmanaged audio resources.
/// </summary>
public class AudioService : IAudioService
{
    private AudioManager? _audioManager;
    private AudioRecord? _audioRecord;
    private AudioTrack? _audioTrack;
    private bool _isRouting;
    private Thread? _audioThread;
    private bool _shouldStop;
    private AudioEngine _audioEngine;
    private float[]? _floatBuffer;
    private byte[]? _pcmBuffer;
    private bool _buffersFromPool = false;
    private ScoConnectionReceiver? _scoReceiver;
    private bool _disposed = false;

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

    public async Task<bool> StartAudioRoutingAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

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
                bool scoConnected = await _scoReceiver.WaitForConnectionAsync(3000).ConfigureAwait(false);

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

            // Load noise reduction setting (default: enabled)
            bool noiseReductionEnabled = Microsoft.Maui.Storage.Preferences.Get("noise_reduction", true);
            _audioEngine.SetNoiseReduction(noiseReductionEnabled);
            System.Diagnostics.Debug.WriteLine($"[AudioService] Noise reduction initialized: {(noiseReductionEnabled ? "ON" : "OFF")}");

            // Rent buffers from ArrayPool (reduces GC pressure, reuses memory)
            int floatBufferSize = minBufferSize / 2; // PCM16 = 2 bytes per sample
            int maxBufferSize = floatBufferSize * 4; // 4x safety margin

            _floatBuffer = ArrayPool<float>.Shared.Rent(maxBufferSize);
            _pcmBuffer = ArrayPool<byte>.Shared.Rent(maxBufferSize * 2);
            _buffersFromPool = true;

            System.Diagnostics.Debug.WriteLine($"[AudioService] ✓ Buffers rented from ArrayPool: {maxBufferSize} samples ({maxBufferSize * 2} bytes)");
            System.Diagnostics.Debug.WriteLine($"[AudioService]   - Reduces GC pressure, reuses memory from pool");

            _audioRecord.StartRecording();
            _audioTrack.Play();

            _isRouting = true;
            _shouldStop = false;

            System.Diagnostics.Debug.WriteLine("[AudioService] ");
            System.Diagnostics.Debug.WriteLine("[AudioService] ✓ AudioRecord started: Now capturing from phone microphone");
            System.Diagnostics.Debug.WriteLine("[AudioService] ✓ AudioTrack started: Now playing to Bluetooth speaker");
            System.Diagnostics.Debug.WriteLine("[AudioService] ✓ Audio routing loop starting...");
            System.Diagnostics.Debug.WriteLine("[AudioService] ");

            // Start audio routing thread with REAL-TIME PRIORITY
            _audioThread = new Thread(AudioRoutingLoop)
            {
                Name = "AudioEngine-RT",
                Priority = ThreadPriority.Highest, // Real-time priority
                IsBackground = false // Keep app alive for audio
            };
            _audioThread.Start();

            // Set Android native thread priority to URGENT_AUDIO
            SetThreadPriorityAndroid();

            StatusChanged?.Invoke(this, "Routing: Phone Mic → Bluetooth Speaker");

            return true; // ✅ No fake async wrapper
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("[AudioService] Audio routing startup cancelled");
            await StopAudioRoutingAsync(CancellationToken.None).ConfigureAwait(false);
            throw; // Re-throw to let caller handle
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error: {ex.Message}");
            return false;
        }
    }

    public async Task StopAudioRoutingAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _shouldStop = true;
        _isRouting = false;

        // Wait for audio thread to exit asynchronously (non-blocking)
        if (_audioThread != null)
        {
            await Task.Run(() => _audioThread.Join(2000), cancellationToken).ConfigureAwait(false);
        }

        // Cleanup resources (exception-safe)
        try
        {
            DisposeAudioRecord();
            DisposeAudioTrack();
            DisposeScoReceiver();

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
        }
        finally
        {
            StatusChanged?.Invoke(this, "Audio routing stopped");
        }
    }

    public void SetVolume(double volume)
    {
        ThrowIfDisposed();

        // Apply volume as digital gain in DSP engine
        // This works for Bluetooth audio (AudioTrack.SetVolume doesn't affect Bluetooth SCO)
        try
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Setting volume to {volume * 100}%");

            // Set digital gain in audio engine (LOCK-FREE via volatile field)
            _audioEngine.SetVolume(volume);

            // ALSO set Android system volume for VoiceCall stream to MAXIMUM
            // This ensures hardware output is at full volume, and we control via digital gain
            if (_audioManager != null)
            {
                try
                {
                    // Get max volume for voice call stream
                    int maxVolume = _audioManager.GetStreamMaxVolume(global::Android.Media.Stream.VoiceCall);

                    // Set system volume to maximum when volume > 0, to ensure strong output
                    // We control actual volume via digital gain in the audio engine
                    int systemVolume = volume > 0.05 ? maxVolume : 0;

                    // Set the system volume (without showing UI to avoid spam)
                    _audioManager.SetStreamVolume(global::Android.Media.Stream.VoiceCall, systemVolume, VolumeNotificationFlags.RemoveSoundAndVibrate);

                    System.Diagnostics.Debug.WriteLine($"[AudioService] System volume set to {systemVolume}/{maxVolume} (MAX)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] Failed to set system volume: {ex.Message}");
                }
            }

            // Also set AudioTrack volume to maximum (digital gain controls actual volume)
            if (_audioTrack != null)
            {
                try
                {
                    // Set AudioTrack to max volume, actual volume controlled by digital gain
                    float trackVolume = volume > 0.05 ? 1.0f : 0.0f;
                    _audioTrack.SetVolume(trackVolume);
                    System.Diagnostics.Debug.WriteLine($"[AudioService] AudioTrack volume set to {trackVolume} (MAX)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] Failed to set AudioTrack volume: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] SetVolume error: {ex.Message}");
        }
    }

    public void SetEffect(string effectName)
    {
        ThrowIfDisposed();

        try
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Changing effect to: {effectName}");

            // Lock-free effect switching (AudioEngine handles thread safety internally)
            _audioEngine.SetPreset(effectName);

            StatusChanged?.Invoke(this, $"Effect changed to: {effectName}");
            System.Diagnostics.Debug.WriteLine($"[AudioService] Effect changed successfully to: {effectName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] SetEffect error: {ex.Message}");
            StatusChanged?.Invoke(this, $"Error setting effect: {ex.Message}");
        }
    }

    public void SetNoiseReduction(bool enabled)
    {
        ThrowIfDisposed();

        try
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Setting noise reduction: {(enabled ? "ON" : "OFF")}");

            // Lock-free noise reduction toggle (uses volatile field)
            _audioEngine.SetNoiseReduction(enabled);

            StatusChanged?.Invoke(this, $"Noise reduction: {(enabled ? "ON" : "OFF")}");
            System.Diagnostics.Debug.WriteLine($"[AudioService] Noise reduction set successfully to: {(enabled ? "ON" : "OFF")}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] SetNoiseReduction error: {ex.Message}");
            StatusChanged?.Invoke(this, $"Error setting noise reduction: {ex.Message}");
        }
    }

    public string GetCurrentEffect()
    {
        ThrowIfDisposed();
        return _audioEngine.GetCurrentPreset();
    }

    public string[] GetAvailableEffects()
    {
        ThrowIfDisposed();

        return new[]
        {
            // Free effects
            "clean", "podcast", "stage_mc", "karaoke", "announcer",
            "robot", "megaphone", "stadium", "deep_voice", "chipmunk",
            // Premium character voices
            "nerdy", "squeaky_cartoon", "dopey_giant", "squawky_bird",
            "dopey_dad", "mouse", "villain", "grumpy_cat"
        };
    }

    public void SetMasterEQ(float lowDb, float midDb, float highDb)
    {
        _audioEngine.SetMasterEQ(lowDb, midDb, highDb);
    }

    public void SetMasterDistortion(float amount)
    {
        _audioEngine.SetMasterDistortion(amount);
    }

    public (float Low, float Mid, float High, float Distortion) GetMasterEQ()
    {
        return _audioEngine.GetMasterEQ();
    }

    public void ResetMasterEQ()
    {
        _audioEngine.ResetMasterEQ();
    }

    /// <summary>
    /// Real-time audio processing loop.
    /// CRITICAL: Zero allocations, zero locks, real-time priority.
    /// </summary>
    private void AudioRoutingLoop()
    {
        System.Diagnostics.Debug.WriteLine("[AudioEngine-RT] Audio thread started with HIGHEST priority");

        while (!_shouldStop && _audioRecord != null && _audioTrack != null)
        {
            try
            {
                // Read audio from microphone (uses pre-allocated buffer)
                int bytesRead = _audioRecord.Read(_pcmBuffer, 0, _pcmBuffer.Length);

                if (bytesRead > 0)
                {
                    // Convert PCM16 (byte[]) to float32 (float[])
                    int sampleCount = bytesRead / 2; // 2 bytes per PCM16 sample

                    // Safety check: Ensure we don't exceed buffer size
                    if (sampleCount > _floatBuffer.Length)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioEngine-RT] WARNING: Sample count ({sampleCount}) exceeds buffer size ({_floatBuffer.Length}), clamping");
                        sampleCount = _floatBuffer.Length;
                    }

                    // Convert PCM16 to float (no allocation)
                    ConvertPCM16ToFloat(_pcmBuffer, _floatBuffer, sampleCount);

                    // Process through DSP engine (LOCK-FREE!)
                    _audioEngine.ProcessBuffer(_floatBuffer, 0, sampleCount);

                    // Convert float back to PCM16 (no allocation)
                    ConvertFloatToPCM16(_floatBuffer, _pcmBuffer, sampleCount);

                    // Write processed audio to output
                    _audioTrack.Write(_pcmBuffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioEngine-RT] Audio routing error: {ex.Message}");
                // Don't break - continue processing. Skip this buffer and try next.
                // Only break on fatal errors like device disconnection.
                if (_shouldStop || _audioRecord?.RecordingState != global::Android.Media.RecordState.Recording)
                    break;
            }
        }

        System.Diagnostics.Debug.WriteLine("[AudioEngine-RT] Audio thread stopped");
    }

    /// <summary>
    /// Set Android native thread priority to URGENT_AUDIO for real-time performance.
    /// Must be called AFTER thread starts.
    /// </summary>
    private void SetThreadPriorityAndroid()
    {
        try
        {
            // Wait a moment for thread to fully start
            Thread.Sleep(10);

            // Set Android native thread priority to URGENT_AUDIO
            // This is the highest priority for audio processing threads
            var threadId = global::Android.OS.Process.MyTid();
            global::Android.OS.Process.SetThreadPriority(threadId,
                global::Android.OS.ThreadPriority.UrgentAudio);

            System.Diagnostics.Debug.WriteLine("[AudioService] ✓ Thread priority set to URGENT_AUDIO (highest real-time priority)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] ⚠ Warning: Could not set URGENT_AUDIO priority: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[AudioService]   Audio will use ThreadPriority.Highest instead");
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

    #region IDisposable Implementation

    /// <summary>
    /// Check if object has been disposed and throw if it has.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AudioService));
    }

    /// <summary>
    /// Public Dispose method - called by consumers to clean up resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose pattern implementation.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        System.Diagnostics.Debug.WriteLine("[AudioService] Disposing resources...");

        if (disposing)
        {
            try
            {
                // Stop audio routing first
                _shouldStop = true;
                _isRouting = false;

                // Wait for audio thread to exit (with timeout)
                _audioThread?.Join(2000);

                // Dispose all resources
                DisposeAudioRecord();
                DisposeAudioTrack();
                DisposeScoReceiver();
                DisposeBuffers();

                // Restore audio mode
                if (_audioManager != null)
                {
                    try
                    {
                        _audioManager.StopBluetoothSco();
                        _audioManager.Mode = Mode.Normal;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AudioService] Error restoring audio mode: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("[AudioService] ✓ Resources disposed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] Error during disposal: {ex.Message}");
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// Dispose AudioRecord safely.
    /// </summary>
    private void DisposeAudioRecord()
    {
        try
        {
            if (_audioRecord != null)
            {
                _audioRecord.Stop();
                _audioRecord.Release();
                _audioRecord.Dispose();
                System.Diagnostics.Debug.WriteLine("[AudioService] ✓ AudioRecord disposed");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Error disposing AudioRecord: {ex.Message}");
        }
        finally
        {
            _audioRecord = null;
        }
    }

    /// <summary>
    /// Dispose AudioTrack safely.
    /// </summary>
    private void DisposeAudioTrack()
    {
        try
        {
            if (_audioTrack != null)
            {
                _audioTrack.Stop();
                _audioTrack.Release();
                _audioTrack.Dispose();
                System.Diagnostics.Debug.WriteLine("[AudioService] ✓ AudioTrack disposed");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Error disposing AudioTrack: {ex.Message}");
        }
        finally
        {
            _audioTrack = null;
        }
    }

    /// <summary>
    /// Unregister SCO receiver safely.
    /// </summary>
    private void DisposeScoReceiver()
    {
        try
        {
            if (_scoReceiver != null)
            {
                Platform.CurrentActivity?.UnregisterReceiver(_scoReceiver);
                System.Diagnostics.Debug.WriteLine("[AudioService] ✓ SCO receiver unregistered");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Error unregistering SCO receiver: {ex.Message}");
        }
        finally
        {
            _scoReceiver = null;
        }
    }

    /// <summary>
    /// Return buffers to ArrayPool.
    /// </summary>
    private void DisposeBuffers()
    {
        try
        {
            if (_buffersFromPool)
            {
                if (_floatBuffer != null)
                {
                    ArrayPool<float>.Shared.Return(_floatBuffer, clearArray: true);
                    _floatBuffer = null;
                }

                if (_pcmBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_pcmBuffer, clearArray: true);
                    _pcmBuffer = null;
                }

                _buffersFromPool = false;
                System.Diagnostics.Debug.WriteLine("[AudioService] ✓ Buffers returned to ArrayPool");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Error disposing buffers: {ex.Message}");
        }
    }

    #endregion

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
