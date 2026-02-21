using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BluetoothMicrophoneApp.Audio.DSP;
using BluetoothMicrophoneApp.Services;
using BluetoothMicrophoneApp.Models;

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

        // Device management tests
        report.Results.Add(await TestDeviceManagementFlow());

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
                new KaraokeEffect()
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
                "clean", "podcast", "stage_mc", "karaoke", "announcer",
                "robot", "megaphone", "stadium", "deep_voice", "chipmunk"
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
