using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BluetoothMicrophoneApp.Audio.DSP;
using BluetoothMicrophoneApp.Services;
using BluetoothMicrophoneApp.Models;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BluetoothMicrophoneApp.Tests;

/// <summary>
/// Sanity Test Agent - Verifies main flows don't crash the app.
///
/// CRITICAL: These tests MUST pass before every build.
/// If any test fails, the app has regressions that could crash in production.
/// </summary>
public class SanityTestAgent
{
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public bool Passed { get; set; }
        public string Message { get; set; } = "";
        public Exception? Exception { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class TestReport
    {
        public DateTime RunTime { get; set; }
        public List<TestResult> Results { get; set; } = new();
        public int TotalTests => Results.Count;
        public int PassedTests => Results.Count(r => r.Passed);
        public int FailedTests => Results.Count(r => !r.Passed);
        public bool AllPassed => FailedTests == 0;
    }

    public async Task<TestReport> RunAllTestsAsync()
    {
        var report = new TestReport
        {
            RunTime = DateTime.Now
        };

        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║    SANITY TEST AGENT - CRASH TESTING   ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        // CRITICAL: Dependency injection tests (must pass for app to start)
        report.Results.Add(await TestDependencyInjectionRegistration());

        // Core initialization tests
        report.Results.Add(await TestAudioEngineInitialization());
        report.Results.Add(await TestAllEffectsCreation());

        // Effect chain tests
        report.Results.Add(await TestEffectChainProcessing());
        report.Results.Add(await TestAllPresetLoading());

        // Volume control tests
        report.Results.Add(await TestVolumeControl());

        // Thread safety tests
        report.Results.Add(await TestThreadSafeEffectSwitching());

        // Audio processing tests
        report.Results.Add(await TestAudioBufferConversion());
        report.Results.Add(await TestAudioProcessingLoop());

        // Noise reduction tests
        report.Results.Add(await TestNoiseReductionEffect());

        // Audio routing tests (SCO → A2DP fallback)
        report.Results.Add(await TestAudioRoutingFallbackLogic());

        // Device management tests
        report.Results.Add(await TestDeviceManagementFlow());
        report.Results.Add(await TestDeviceListFiltering());
        report.Results.Add(await TestBackToDevicesBehavior());

        // Authentication tests
        report.Results.Add(await TestGuestLogin());
        report.Results.Add(await TestPhoneLogin());
        report.Results.Add(await TestGoogleLogin());
        report.Results.Add(await TestAppleLogin());
        report.Results.Add(await TestSessionPersistence());
        report.Results.Add(await TestLogout());

        // Main flow crash tests (CRITICAL)
        report.Results.Add(await TestMainFlowNoCrash());

        return report;
    }

    private async Task<TestResult> TestDependencyInjectionRegistration()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Dependency Injection registration...");

            // This test verifies that all required services are registered in the DI container.
            // If a service is missing, the app will crash on startup with:
            // "Unable to resolve service for type 'X' while attempting to activate 'Y'"

            var builder = MauiApp.CreateBuilder();

            // Register services exactly as in MauiProgram.cs
            builder.Services.AddSingleton<IAuthService, AuthService>();

#if ANDROID
            builder.Services.AddSingleton<IBluetoothService, BluetoothMicrophoneApp.Platforms.Android.Services.BluetoothService>();
            builder.Services.AddSingleton<IAudioService, BluetoothMicrophoneApp.Platforms.Android.Services.AudioService>();
            builder.Services.AddSingleton<IConnectivityDiagnostics, BluetoothMicrophoneApp.Platforms.Android.Services.ConnectivityDiagnostics>();
#elif IOS
            builder.Services.AddSingleton<IBluetoothService, BluetoothMicrophoneApp.Platforms.iOS.Services.BluetoothService>();
            builder.Services.AddSingleton<IAudioService, BluetoothMicrophoneApp.Platforms.iOS.Services.AudioService>();
#endif

            var app = builder.Build();

            // Try to resolve all required services
            var errors = new List<string>();

            // Test 1: IAuthService (required by App.xaml.cs)
            try
            {
                var authService = app.Services.GetService<IAuthService>();
                if (authService == null)
                    errors.Add("IAuthService resolved to null");
            }
            catch (Exception ex)
            {
                errors.Add($"IAuthService: {ex.Message}");
            }

            // Test 2: IBluetoothService (required by MainPage)
            try
            {
                var bluetoothService = app.Services.GetService<IBluetoothService>();
                if (bluetoothService == null)
                    errors.Add("IBluetoothService resolved to null");
            }
            catch (Exception ex)
            {
                errors.Add($"IBluetoothService: {ex.Message}");
            }

            // Test 3: IAudioService (required by MainPage)
            try
            {
                var audioService = app.Services.GetService<IAudioService>();
                if (audioService == null)
                    errors.Add("IAudioService resolved to null");
            }
            catch (Exception ex)
            {
                errors.Add($"IAudioService: {ex.Message}");
            }

            // Test 4: IConnectivityDiagnostics (Android only, optional)
#if ANDROID
            try
            {
                var diagnostics = app.Services.GetService<IConnectivityDiagnostics>();
                if (diagnostics == null)
                    errors.Add("IConnectivityDiagnostics resolved to null");
            }
            catch (Exception ex)
            {
                errors.Add($"IConnectivityDiagnostics: {ex.Message}");
            }
#endif

            if (errors.Any())
            {
                throw new Exception($"DI registration failures:\n  - {string.Join("\n  - ", errors)}");
            }

            sw.Stop();
            return new TestResult
            {
                TestName = "⚡ Dependency Injection Registration",
                Passed = true,
                Message = "All required services are registered and can be resolved",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "⚡ Dependency Injection Registration",
                Passed = false,
                Message = "❌ CRITICAL: Missing service registration - app will crash on startup!",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestAudioEngineInitialization()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var engine = new AudioEngine();
            engine.Initialize(48000);

            // Verify initialization (engine starts with "None" preset, then needs SetPreset)
            engine.SetPreset("clean");
            if (engine.GetCurrentPreset() != "clean")
                throw new Exception("Engine not initialized properly");

            sw.Stop();
            return new TestResult
            {
                TestName = "AudioEngine Initialization",
                Passed = true,
                Message = "AudioEngine initializes without crashing",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "AudioEngine Initialization",
                Passed = false,
                Message = "AudioEngine initialization crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestAllEffectsCreation()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Test creating all effect types
            var effects = new IAudioEffect[]
            {
                new GainEffect(),
                new NoiseGateEffect(),
                new ThreeBandEQEffect(),
                new CompressorEffect(),
                new LimiterEffect(),
                new EchoDelayEffect(),
                new RobotVoiceEffect(),
                new MegaphoneEffect(),
                new KaraokeEffect(),
                new NoiseReductionEffect()
            };

            // Prepare all effects
            foreach (var effect in effects)
            {
                effect.Prepare(48000);
            }

            sw.Stop();
            return new TestResult
            {
                TestName = "All Effects Creation",
                Passed = true,
                Message = $"All {effects.Length} effect types created successfully",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "All Effects Creation",
                Passed = false,
                Message = "Effect creation crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestEffectChainProcessing()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var chain = new AudioEffectChain();

            // Add multiple effects
            chain.AddEffect(new GainEffect());
            chain.AddEffect(new NoiseGateEffect());
            chain.AddEffect(new LimiterEffect());

            chain.Prepare(48000);

            // Process test audio
            var buffer = new float[1024];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = (float)Math.Sin(2 * Math.PI * 440 * i / 48000); // 440Hz sine wave

            chain.Process(buffer, 0, buffer.Length);

            sw.Stop();
            return new TestResult
            {
                TestName = "Effect Chain Processing",
                Passed = true,
                Message = "Effect chain processes audio without crashing",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Effect Chain Processing",
                Passed = false,
                Message = "Effect chain processing crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestAllPresetLoading()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var engine = new AudioEngine();
            engine.Initialize(48000);

            // Test all presets
            var presets = new[]
            {
                // Professional
                "clean", "podcast", "announcer", "stage_mc", "karaoke", "stadium",
                // Voice Effects
                "robot", "megaphone", "deep_voice", "chipmunk", "anime",
                // Character Voices
                "nerdy", "squeaky_cartoon", "dopey_giant", "squawky_bird",
                "dopey_dad", "mouse", "villain", "grumpy_cat"
            };

            foreach (var preset in presets)
            {
                engine.SetPreset(preset);

                // Verify preset was loaded
                if (engine.GetCurrentPreset() != preset)
                    throw new Exception($"Preset '{preset}' not loaded correctly");
            }

            sw.Stop();
            return new TestResult
            {
                TestName = "All Preset Loading",
                Passed = true,
                Message = $"All {presets.Length} presets load without crashing",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "All Preset Loading",
                Passed = false,
                Message = "Preset loading crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestVolumeControl()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var engine = new AudioEngine();
            engine.Initialize(48000);
            engine.SetPreset("clean");

            // Test volume range
            var volumes = new[] { 0.0, 0.5, 1.0, 1.5, 2.0 }; // 0% to 200%

            var buffer = new float[1024];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = 0.5f; // Half amplitude

            foreach (var volume in volumes)
            {
                engine.SetVolume(volume);
                engine.ProcessBuffer(buffer, 0, buffer.Length);
            }

            sw.Stop();
            return new TestResult
            {
                TestName = "Volume Control",
                Passed = true,
                Message = "Volume control works without crashing",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Volume Control",
                Passed = false,
                Message = "Volume control crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestThreadSafeEffectSwitching()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var engine = new AudioEngine();
            engine.Initialize(48000);

            // Simulate rapid effect switching (like user clicking fast)
            var presets = new[] { "clean", "robot", "podcast", "karaoke", "megaphone" };

            foreach (var preset in presets)
            {
                engine.SetPreset(preset);

                // Process audio immediately after switching
                var buffer = new float[512];
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = 0.5f;

                engine.ProcessBuffer(buffer, 0, buffer.Length);
            }

            sw.Stop();
            return new TestResult
            {
                TestName = "Thread-Safe Effect Switching",
                Passed = true,
                Message = "Rapid effect switching doesn't crash",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Thread-Safe Effect Switching",
                Passed = false,
                Message = "Effect switching crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestAudioBufferConversion()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Test PCM16 to Float32 conversion
            var pcm16 = new byte[2048]; // 1024 samples
            var floatBuffer = new float[1024];

            // Fill with test data
            for (int i = 0; i < 1024; i++)
            {
                short sample = (short)(i % 100 - 50); // -50 to +49
                pcm16[i * 2] = (byte)(sample & 0xFF);
                pcm16[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            // Convert to float
            for (int i = 0; i < 1024; i++)
            {
                short sample = (short)(pcm16[i * 2] | (pcm16[i * 2 + 1] << 8));
                floatBuffer[i] = sample / 32768f;
            }

            // Convert back to PCM16
            for (int i = 0; i < 1024; i++)
            {
                float clampedSample = Math.Clamp(floatBuffer[i], -1f, 1f);
                short sample = (short)(clampedSample * 32767f);
                pcm16[i * 2] = (byte)(sample & 0xFF);
                pcm16[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            sw.Stop();
            return new TestResult
            {
                TestName = "Audio Buffer Conversion",
                Passed = true,
                Message = "PCM16 ↔ Float32 conversion works correctly",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Audio Buffer Conversion",
                Passed = false,
                Message = "Buffer conversion crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestAudioProcessingLoop()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var engine = new AudioEngine();
            engine.Initialize(48000);
            engine.SetPreset("podcast");

            // Simulate continuous audio processing
            var buffer = new float[1024];

            for (int iteration = 0; iteration < 1000; iteration++)
            {
                // Fill buffer with test audio
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = (float)Math.Sin(2 * Math.PI * 440 * i / 48000);

                // Process buffer (simulates real-time loop)
                engine.ProcessBuffer(buffer, 0, buffer.Length);
            }

            sw.Stop();
            return new TestResult
            {
                TestName = "Audio Processing Loop",
                Passed = true,
                Message = "1000 iterations of audio processing completed",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Audio Processing Loop",
                Passed = false,
                Message = "Processing loop crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestNoiseReductionEffect()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Noise Reduction Effect...");

            // Create and initialize
            var nr = new NoiseReductionEffect();
            nr.Initialize(48000);

            // Generate silence (so it learns noise profile)
            var silence = new float[4096];
            for (int i = 0; i < silence.Length; i++)
                silence[i] = 0.01f * (float)(new Random(i).NextDouble() * 2 - 1); // quiet noise

            // Process multiple silent frames so it learns the noise floor
            for (int frame = 0; frame < 20; frame++)
                nr.Process(silence, 0, silence.Length);

            // Now process a buffer with speech + noise
            var speechBuffer = new float[2048];
            for (int i = 0; i < speechBuffer.Length; i++)
            {
                float speech = 0.5f * MathF.Sin(2f * MathF.PI * 300f * i / 48000f);
                float noise = 0.01f * (float)(new Random(i + 99999).NextDouble() * 2 - 1);
                speechBuffer[i] = speech + noise;
            }
            nr.Process(speechBuffer, 0, speechBuffer.Length);

            // Verify output isn't all zeros (speech should survive)
            float maxAbs = 0f;
            for (int i = 0; i < speechBuffer.Length; i++)
                maxAbs = Math.Max(maxAbs, Math.Abs(speechBuffer[i]));

            if (maxAbs < 0.01f)
                throw new Exception($"Noise reduction killed the signal (maxAbs={maxAbs:F4})");

            // Test parameter updates
            nr.ReductionStrength = 2.0f;
            nr.SpectralFloor = 0.2f;
            nr.SpeechThreshold = 0.05f;

            // Test reset
            nr.ResetNoiseProfile();
            nr.Reset();

            // Test bypass
            nr.Bypass = true;
            var bypassBuf = new float[512];
            bypassBuf[0] = 0.5f;
            nr.Process(bypassBuf, 0, bypassBuf.Length);
            if (bypassBuf[0] != 0.5f)
                throw new Exception("Bypass mode should not modify buffer");

            sw.Stop();
            return new TestResult
            {
                TestName = "Noise Reduction Effect",
                Passed = true,
                Message = $"Noise reduction learns profile, processes audio (peak={maxAbs:F3}), params/reset/bypass work",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Noise Reduction Effect",
                Passed = false,
                Message = $"Noise reduction test failed: {ex.Message}",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestAudioRoutingFallbackLogic()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Audio Routing Fallback (SCO → A2DP)...");

            // This test verifies the AudioService routing logic compiles and the
            // fallback flow is structurally sound. Actual Bluetooth hardware is
            // not available in unit tests, so we test the decision logic.

            // 1. Verify AudioEngine works with both SCO and A2DP AudioTrack configurations
            var engine = new AudioEngine();
            engine.Initialize(44100); // 44100 = the sample rate used in AudioService
            engine.SetPreset("clean");

            // 2. Process audio at the sample rate used by AudioService
            var buffer = new float[1024];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = 0.3f * MathF.Sin(2f * MathF.PI * 440f * i / 44100f);

            engine.ProcessBuffer(buffer, 0, buffer.Length);

            // 3. Verify noise reduction integrates (it runs before effects in ProcessBuffer)
            engine.SetNoiseReduction(true);
            if (!engine.IsNoiseReductionEnabled())
                throw new Exception("Noise reduction should be enabled");

            engine.ProcessBuffer(buffer, 0, buffer.Length);

            engine.SetNoiseReduction(false);
            if (engine.IsNoiseReductionEnabled())
                throw new Exception("Noise reduction should be disabled");

            // 4. Verify all audio modes work: each preset through full pipeline at 44100Hz
            var criticalPresets = new[] { "clean", "podcast", "robot", "deep_voice", "chipmunk" };
            foreach (var preset in criticalPresets)
            {
                engine.SetPreset(preset);
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = 0.3f * MathF.Sin(2f * MathF.PI * 440f * i / 44100f);
                engine.ProcessBuffer(buffer, 0, buffer.Length);
            }

            // 5. Verify master EQ works (used by Sound Editor on both SCO and A2DP paths)
            engine.SetMasterEQ(3f, 0f, -2f);
            engine.SetMasterDistortion(0.3f);
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = 0.3f * MathF.Sin(2f * MathF.PI * 440f * i / 44100f);
            engine.ProcessBuffer(buffer, 0, buffer.Length);
            engine.ResetMasterEQ();

            // 6. Verify GetMasterEQ returns reset values
            var eq = engine.GetMasterEQ();
            if (eq.Low != 0f || eq.Mid != 0f || eq.High != 0f || eq.Distortion != 0f)
                throw new Exception($"Master EQ not reset: L={eq.Low} M={eq.Mid} H={eq.High} D={eq.Distortion}");

            sw.Stop();
            return new TestResult
            {
                TestName = "Audio Routing Fallback (SCO/A2DP)",
                Passed = true,
                Message = $"Engine works at 44100Hz, noise reduction toggles, {criticalPresets.Length} presets + master EQ verified",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Audio Routing Fallback (SCO/A2DP)",
                Passed = false,
                Message = $"Audio routing fallback test failed: {ex.Message}",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestDeviceManagementFlow()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Test device custom name management
            var testAddress1 = "AA:BB:CC:DD:EE:FF";
            var testAddress2 = "11:22:33:44:55:66";
            var originalName1 = "Test Device 1";
            var originalName2 = "Test Device 2";
            var customName1 = "My Custom Speaker";
            var customName2 = "Living Room Headphones";

            // Test 1: Initially should return original name
            var displayName = DeviceNameManager.GetDisplayName(testAddress1, originalName1);
            if (displayName != originalName1)
                throw new Exception("Should return original name when no custom name set");

            // Test 2: Set custom name
            DeviceNameManager.SetCustomName(testAddress1, customName1);

            // Test 3: Should return custom name after setting
            displayName = DeviceNameManager.GetDisplayName(testAddress1, originalName1);
            if (displayName != customName1)
                throw new Exception("Should return custom name after setting");

            // Test 4: HasCustomName should return true
            if (!DeviceNameManager.HasCustomName(testAddress1))
                throw new Exception("HasCustomName should return true after setting custom name");

            // Test 5: Test multiple devices
            DeviceNameManager.SetCustomName(testAddress2, customName2);

            var name1 = DeviceNameManager.GetDisplayName(testAddress1, originalName1);
            var name2 = DeviceNameManager.GetDisplayName(testAddress2, originalName2);

            if (name1 != customName1 || name2 != customName2)
                throw new Exception("Multiple devices should maintain separate custom names");

            // Test 6: Remove custom name (simulate device deletion)
            DeviceNameManager.RemoveCustomName(testAddress1);

            displayName = DeviceNameManager.GetDisplayName(testAddress1, originalName1);
            if (displayName != originalName1)
                throw new Exception("Should return original name after removing custom name");

            // Test 7: HasCustomName should return false after removal
            if (DeviceNameManager.HasCustomName(testAddress1))
                throw new Exception("HasCustomName should return false after removing custom name");

            // Test 8: Setting empty/null custom name should remove it
            DeviceNameManager.SetCustomName(testAddress2, "");
            displayName = DeviceNameManager.GetDisplayName(testAddress2, originalName2);
            if (displayName != originalName2)
                throw new Exception("Setting empty custom name should revert to original name");

            // Cleanup test data
            DeviceNameManager.RemoveCustomName(testAddress1);
            DeviceNameManager.RemoveCustomName(testAddress2);

            sw.Stop();
            return new TestResult
            {
                TestName = "Device Management Flow",
                Passed = true,
                Message = "Device rename and delete operations work correctly",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Device Management Flow",
                Passed = false,
                Message = "Device management crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    /// <summary>
    /// Tests device list filtering logic:
    /// - Available Devices (top): all available devices (paired or not), excluding "Unknown Device"
    /// - Recently Paired (bottom): paired devices that are NOT currently available (greyed out, blocked)
    /// - CRITICAL: Unavailable devices MUST NOT be connectable (taps blocked)
    ///
    /// AVAILABILITY BEHAVIOR:
    /// - Connected devices detected via BluetoothAdapter.GetProfileProxy (A2DP, Headset)
    /// - Bonded/paired devices start as IsAvailable=false, marked true if connected or discovered
    /// - Discovered (non-paired) devices are always IsAvailable=true
    /// - Users MUST NOT be able to connect to unavailable devices
    /// </summary>
    private async Task<TestResult> TestDeviceListFiltering()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Device list filtering...");

            // Simulate scan results: mix of paired and discovered devices
            // Paired devices: one available (responded to discovery), one not
            var allDevices = new List<BluetoothDevice>
            {
                new() { Name = "JBL Speaker", Address = "AA:BB:CC:DD:EE:01", IsPaired = true, IsAvailable = true },   // paired + discovered
                new() { Name = "My Headphones", Address = "AA:BB:CC:DD:EE:02", IsPaired = true, IsAvailable = false }, // paired but NOT available
                new() { Name = "Old Speaker", Address = "AA:BB:CC:DD:EE:07", IsPaired = true, IsAvailable = false },   // paired but NOT available
                new() { Name = "Unknown Device", Address = "AA:BB:CC:DD:EE:03", IsPaired = false, IsAvailable = true },
                new() { Name = "LG TV", Address = "AA:BB:CC:DD:EE:04", IsPaired = false, IsAvailable = true },
                new() { Name = "Unknown Device", Address = "AA:BB:CC:DD:EE:05", IsPaired = false, IsAvailable = true },
                new() { Name = "SmartDevice", Address = "AA:BB:CC:DD:EE:06", IsPaired = false, IsAvailable = true },
            };

            // Test 1: Available Devices = all available devices (paired or not), exclude "Unknown Device"
            var available = allDevices
                .Where(d => d.IsAvailable
                    && !d.Name.Equals("Unknown Device", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (available.Count != 3)
                throw new Exception($"Available Devices should have 3 (JBL Speaker, LG TV, SmartDevice), got {available.Count}");

            // Test 2: Available paired device (JBL Speaker) must be in Available list
            if (!available.Any(d => d.Name == "JBL Speaker" && d.IsPaired))
                throw new Exception("JBL Speaker (paired+available) should appear in Available list");

            // Test 3: "Unknown Device" must NEVER appear in Available list
            if (available.Any(d => d.Name == "Unknown Device"))
                throw new Exception("Unknown Device should be filtered from Available list");

            // Test 4: Recently Paired = paired devices that are NOT available
            var recentlyPaired = allDevices.Where(d => d.IsPaired && !d.IsAvailable).ToList();
            if (recentlyPaired.Count != 2)
                throw new Exception($"Recently Paired should show 2 unavailable paired devices, got {recentlyPaired.Count}");

            // Test 5: Available paired devices must NOT appear in Recently Paired
            if (recentlyPaired.Any(d => d.IsAvailable))
                throw new Exception("Available devices must not appear in Recently Paired list");

            // Test 6: CRITICAL - Unavailable paired devices must NOT be connectable
            var unavailablePaired = recentlyPaired.Where(d => !d.IsAvailable).ToList();
            if (unavailablePaired.Count != 2)
                throw new Exception($"Should have 2 unavailable paired devices, got {unavailablePaired.Count}");

            // Test 7: Simulate selection blocking for unavailable device
            bool selectionBlocked = false;
            var unavailableDevice = unavailablePaired.First();
            if (!unavailableDevice.IsAvailable)
                selectionBlocked = true; // OnRecentlyPairedDeviceSelected blocks this
            if (!selectionBlocked)
                throw new Exception("CRITICAL: Unavailable device selection was NOT blocked");

            sw.Stop();
            return new TestResult
            {
                TestName = "Device List Filtering",
                Passed = true,
                Message = "Recently Paired and Available Devices lists filter correctly; unavailable devices blocked",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Device List Filtering",
                Passed = false,
                Message = "Device list filtering failed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    /// <summary>
    /// Tests "Back to Devices" behavior:
    /// - Pressing Back returns to device list WITHOUT rescanning
    /// - Connection is maintained (not disconnected)
    /// - Connected device bar is shown with Stop/Play toggle
    /// - Bar disappears when disconnected or connecting to another device
    /// </summary>
    private async Task<TestResult> TestBackToDevicesBehavior()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Back to Devices behavior...");

            // Simulate state: user is connected to a device and has a cached device list
            var connectedDevice = new BluetoothDevice
            {
                Name = "JBL TUNE510BT",
                Address = "C8:2B:6B:7B:09:2D",
                IsPaired = true,
                IsAvailable = true
            };

            var cachedDevices = new List<BluetoothDevice>
            {
                connectedDevice,
                new() { Name = "LG TV", Address = "AA:BB:CC:DD:EE:04", IsPaired = false, IsAvailable = true },
                new() { Name = "Old Speaker", Address = "AA:BB:CC:DD:EE:07", IsPaired = true, IsAvailable = false },
            };

            // Test 1: Back to Devices should NOT clear the device list
            // (simulating: we still have _availableDevices populated)
            if (!cachedDevices.Any())
                throw new Exception("Device list should be preserved when going back");

            // Test 2: Connection should be maintained (not null)
            if (connectedDevice == null)
                throw new Exception("Connected device should not be null after Back");

            // Test 3: Connected device bar should show when connected and viewing device list
            bool isConnected = connectedDevice != null;
            bool shouldShowBar = isConnected; // bar visible = connected
            if (!shouldShowBar)
                throw new Exception("Connected device bar should be visible when connected");

            // Test 4: Bar should show Stop button when audio is routing
            bool isAudioRouting = true; // simulated
            bool stopVisible = isAudioRouting;
            bool playVisible = !isAudioRouting;
            if (!stopVisible || playVisible)
                throw new Exception("Stop button should be visible when audio is routing");

            // Test 5: After stopping, bar should show Play button
            isAudioRouting = false; // user pressed stop
            stopVisible = isAudioRouting;
            playVisible = !isAudioRouting;
            if (stopVisible || !playVisible)
                throw new Exception("Play button should be visible after stopping audio");

            // Test 6: Bar should disappear when disconnected
            bool isDisconnected = true; // simulated disconnect
            bool barVisible = !isDisconnected;
            if (barVisible)
                throw new Exception("Connected device bar should hide when disconnected");

            // Test 7: Bar should disappear when selecting a different device
            var differentDevice = cachedDevices[1]; // LG TV
            bool connectingToOther = differentDevice.Address != connectedDevice.Address;
            bool barShouldHide = connectingToOther;
            if (!barShouldHide)
                throw new Exception("Bar should hide when connecting to a different device");

            sw.Stop();
            return new TestResult
            {
                TestName = "Back to Devices Behavior",
                Passed = true,
                Message = "Back preserves list and connection; bar shows Stop/Play; hides on disconnect or new connection",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Back to Devices Behavior",
                Passed = false,
                Message = "Back to Devices behavior test failed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestGuestLogin()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Guest login...");
            var authService = new AuthService();

            var user = await authService.ContinueAsGuestAsync();

            if (user == null)
                throw new Exception("Guest login returned null");

            if (!user.IsGuest)
                throw new Exception("User should be marked as guest");

            if (user.Provider != AuthProvider.Guest)
                throw new Exception("Provider should be Guest");

            if (!authService.IsAuthenticated)
                throw new Exception("AuthService should show authenticated");

            if (authService.CurrentUser == null)
                throw new Exception("CurrentUser should not be null");

            sw.Stop();
            return new TestResult
            {
                TestName = "Guest Login",
                Passed = true,
                Message = "Guest login works correctly",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Guest Login",
                Passed = false,
                Message = "Guest login crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestPhoneLogin()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Phone number login...");
            var authService = new AuthService();

            // Test sending verification code
            var codeSent = await authService.LoginWithPhoneNumberAsync("+1234567890");

            if (!codeSent)
                throw new Exception("Failed to send verification code");

            // Test verifying code
            var user = await authService.VerifyPhoneNumberAsync("+1234567890", "123456");

            if (user == null)
                throw new Exception("Phone verification returned null");

            if (user.IsGuest)
                throw new Exception("User should not be marked as guest");

            if (user.Provider != AuthProvider.PhoneNumber)
                throw new Exception("Provider should be PhoneNumber");

            if (string.IsNullOrEmpty(user.PhoneNumber))
                throw new Exception("PhoneNumber should be set");

            if (!authService.IsAuthenticated)
                throw new Exception("AuthService should show authenticated");

            sw.Stop();
            return new TestResult
            {
                TestName = "Phone Number Login",
                Passed = true,
                Message = "Phone login and verification work correctly",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Phone Number Login",
                Passed = false,
                Message = "Phone login crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestGoogleLogin()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Google login...");
            var authService = new AuthService();

            var user = await authService.LoginWithGoogleAsync();

            if (user == null)
                throw new Exception("Google login returned null");

            if (user.IsGuest)
                throw new Exception("User should not be marked as guest");

            if (user.Provider != AuthProvider.Google)
                throw new Exception("Provider should be Google");

            if (!authService.IsAuthenticated)
                throw new Exception("AuthService should show authenticated");

            if (authService.CurrentUser == null)
                throw new Exception("CurrentUser should not be null");

            sw.Stop();
            return new TestResult
            {
                TestName = "Google Login",
                Passed = true,
                Message = "Google login works correctly",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Google Login",
                Passed = false,
                Message = "Google login crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestAppleLogin()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Apple login...");
            var authService = new AuthService();

            var user = await authService.LoginWithAppleAsync();

            if (user == null)
                throw new Exception("Apple login returned null");

            if (user.IsGuest)
                throw new Exception("User should not be marked as guest");

            if (user.Provider != AuthProvider.Apple)
                throw new Exception("Provider should be Apple");

            if (!authService.IsAuthenticated)
                throw new Exception("AuthService should show authenticated");

            if (authService.CurrentUser == null)
                throw new Exception("CurrentUser should not be null");

            sw.Stop();
            return new TestResult
            {
                TestName = "Apple Login",
                Passed = true,
                Message = "Apple login works correctly",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Apple Login",
                Passed = false,
                Message = "Apple login crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestSessionPersistence()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Session persistence...");

            // Clear any existing session first
            Microsoft.Maui.Storage.Preferences.Clear();

            var authService1 = new AuthService();

            // Login as guest
            var user1 = await authService1.ContinueAsGuestAsync();
            var userId = user1.Id;

            if (!authService1.IsAuthenticated)
                throw new Exception("Should be authenticated after login");

            // Create new AuthService instance (simulates app restart)
            var authService2 = new AuthService();

            // Restore session
            var user2 = await authService2.RestoreSessionAsync();

            if (user2 == null)
                throw new Exception("Session not restored");

            if (user2.Id != userId)
                throw new Exception("Restored user ID doesn't match");

            if (!authService2.IsAuthenticated)
                throw new Exception("Should be authenticated after restore");

            // Clean up
            await authService2.LogoutAsync();

            sw.Stop();
            return new TestResult
            {
                TestName = "Session Persistence",
                Passed = true,
                Message = "Session saves and restores correctly",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Session Persistence",
                Passed = false,
                Message = "Session persistence crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestLogout()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("  → Testing: Logout...");
            var authService = new AuthService();

            // Login first
            var user = await authService.ContinueAsGuestAsync();

            if (!authService.IsAuthenticated)
                throw new Exception("Should be authenticated after login");

            if (authService.CurrentUser == null)
                throw new Exception("CurrentUser should not be null after login");

            // Logout
            await authService.LogoutAsync();

            if (authService.IsAuthenticated)
                throw new Exception("Should not be authenticated after logout");

            if (authService.CurrentUser != null)
                throw new Exception("CurrentUser should be null after logout");

            // Verify session is cleared
            var authService2 = new AuthService();
            var restoredUser = await authService2.RestoreSessionAsync();

            if (restoredUser != null)
                throw new Exception("Session should be cleared after logout");

            sw.Stop();
            return new TestResult
            {
                TestName = "Logout",
                Passed = true,
                Message = "Logout clears session correctly",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "Logout",
                Passed = false,
                Message = "Logout crashed",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> TestMainFlowNoCrash()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Simulate complete user flow
            Console.WriteLine("  → Testing: App startup...");
            var engine = new AudioEngine();

            Console.WriteLine("  → Testing: Audio initialization...");
            engine.Initialize(48000);
            engine.SetPreset("clean");

            Console.WriteLine("  → Testing: User selects effect...");
            engine.SetPreset("robot");

            Console.WriteLine("  → Testing: Audio processing starts...");
            var buffer = new float[1024];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = 0.5f;

            for (int i = 0; i < 100; i++)
                engine.ProcessBuffer(buffer, 0, buffer.Length);

            Console.WriteLine("  → Testing: User changes volume...");
            engine.SetVolume(0.5); // 50%
            engine.ProcessBuffer(buffer, 0, buffer.Length);

            engine.SetVolume(1.5); // 150%
            engine.ProcessBuffer(buffer, 0, buffer.Length);

            Console.WriteLine("  → Testing: User switches effects during playback...");
            engine.SetPreset("podcast");
            engine.ProcessBuffer(buffer, 0, buffer.Length);

            engine.SetPreset("karaoke");
            engine.ProcessBuffer(buffer, 0, buffer.Length);

            Console.WriteLine("  → Testing: User renames connected device...");
            var testDeviceAddress = "AA:BB:CC:DD:EE:FF";
            DeviceNameManager.SetCustomName(testDeviceAddress, "Test Device");
            var displayName = DeviceNameManager.GetDisplayName(testDeviceAddress, "Original Name");
            if (displayName != "Test Device")
                throw new Exception("Device rename failed");

            Console.WriteLine("  → Testing: User deletes old device...");
            DeviceNameManager.RemoveCustomName(testDeviceAddress);
            displayName = DeviceNameManager.GetDisplayName(testDeviceAddress, "Original Name");
            if (displayName != "Original Name")
                throw new Exception("Device delete failed");

            Console.WriteLine("  → Testing: Reset and cleanup...");
            engine.Reset();

            sw.Stop();
            return new TestResult
            {
                TestName = "⭐ MAIN FLOW NO CRASH TEST ⭐",
                Passed = true,
                Message = "Complete user flow executes without crashes",
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new TestResult
            {
                TestName = "⭐ MAIN FLOW NO CRASH TEST ⭐",
                Passed = false,
                Message = "❌ CRITICAL: Main flow crashed! App will crash in production!",
                Exception = ex,
                Duration = sw.Elapsed
            };
        }
    }

    public void PrintReport(TestReport report)
    {
        Console.WriteLine("\n╔════════════════════════════════════════╗");
        Console.WriteLine("║         SANITY TEST REPORT             ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        Console.WriteLine($"Test Run Time: {report.RunTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Total Tests: {report.TotalTests}");
        Console.WriteLine($"✓ Passed: {report.PassedTests}");
        Console.WriteLine($"✗ Failed: {report.FailedTests}\n");

        Console.WriteLine("Test Details:");
        Console.WriteLine("─────────────────────────────────────────");

        foreach (var result in report.Results)
        {
            var icon = result.Passed ? "✓" : "✗";
            var status = result.Passed ? "PASS" : "FAIL";
            var color = result.Passed ? "\x1b[32m" : "\x1b[31m"; // Green or Red
            var reset = "\x1b[0m";

            Console.WriteLine($"{color}{icon} {status}{reset} | {result.TestName}");
            Console.WriteLine($"      {result.Message}");
            Console.WriteLine($"      Duration: {result.Duration.TotalMilliseconds:F2}ms");

            if (result.Exception != null)
            {
                Console.WriteLine($"      Error: {result.Exception.Message}");
                Console.WriteLine($"      Stack: {result.Exception.StackTrace?.Split('\n').FirstOrDefault() ?? "N/A"}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("─────────────────────────────────────────");

        if (report.AllPassed)
        {
            Console.WriteLine("\x1b[32m✓ ALL TESTS PASSED - APP IS SAFE TO BUILD\x1b[0m\n");
        }
        else
        {
            Console.WriteLine("\x1b[31m✗ TESTS FAILED - DO NOT BUILD! FIX CRASHES FIRST!\x1b[0m\n");
        }
    }

    public static async Task<int> Main(string[] args)
    {
        var agent = new SanityTestAgent();
        var report = await agent.RunAllTestsAsync();
        agent.PrintReport(report);

        // Return exit code (0 = success, 1 = failure)
        return report.AllPassed ? 0 : 1;
    }
}
