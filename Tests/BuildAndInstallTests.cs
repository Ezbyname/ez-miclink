using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BluetoothMicrophoneApp.Tests;

/// <summary>
/// Build and Installation Sanity Tests
/// Verifies that the app can be built without errors and installed on a device.
/// </summary>
public class BuildAndInstallTests
{
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public bool Passed { get; set; }
        public string Message { get; set; } = "";
        public string? Details { get; set; }
        public TimeSpan Duration { get; set; }
    }

    private static string ProjectRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public static async Task<TestResult[]> RunTestsAsync()
    {
        var results = new[]
        {
            await TestAndroidBuildCompiles(),
            await TestAndroidApkCreated(),
            await TestDeviceConnected(),
            await TestAppInstallation(),
            await TestAppLaunch()
        };

        return results;
    }

    private static async Task<TestResult> TestAndroidBuildCompiles()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("🔨 Building Android APK...");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build -f net9.0-android",
                WorkingDirectory = ProjectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var output = "";
            var error = "";

            process.OutputDataReceived += (sender, e) => { if (e.Data != null) output += e.Data + "\n"; };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) error += e.Data + "\n"; };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait up to 3 minutes for build
            await Task.Run(() => process.WaitForExit(180000));
            stopwatch.Stop();

            if (!process.HasExited)
            {
                process.Kill();
                return new TestResult
                {
                    TestName = "Android Build Compilation",
                    Passed = false,
                    Message = "Build timed out after 3 minutes",
                    Duration = stopwatch.Elapsed
                };
            }

            // Check for errors
            var errorMatches = Regex.Matches(output + error, @"(\d+)\s+Error\(s\)", RegexOptions.IgnoreCase);
            var errorCount = 0;
            if (errorMatches.Count > 0)
            {
                int.TryParse(errorMatches[errorMatches.Count - 1].Groups[1].Value, out errorCount);
            }

            // Check for "Build succeeded" or "Build FAILED"
            var buildSucceeded = output.Contains("Build succeeded", StringComparison.OrdinalIgnoreCase);
            var buildFailed = output.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase);

            if (buildSucceeded && errorCount == 0 && !buildFailed)
            {
                // Count warnings
                var warningMatches = Regex.Matches(output, @"(\d+)\s+Warning\(s\)", RegexOptions.IgnoreCase);
                var warningCount = 0;
                if (warningMatches.Count > 0)
                {
                    int.TryParse(warningMatches[warningMatches.Count - 1].Groups[1].Value, out warningCount);
                }

                return new TestResult
                {
                    TestName = "Android Build Compilation",
                    Passed = true,
                    Message = $"Build succeeded with 0 errors, {warningCount} warnings",
                    Details = $"Build time: {stopwatch.Elapsed.TotalSeconds:F1}s",
                    Duration = stopwatch.Elapsed
                };
            }
            else
            {
                return new TestResult
                {
                    TestName = "Android Build Compilation",
                    Passed = false,
                    Message = $"Build failed with {errorCount} error(s)",
                    Details = error.Length > 500 ? error.Substring(0, 500) + "..." : error,
                    Duration = stopwatch.Elapsed
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new TestResult
            {
                TestName = "Android Build Compilation",
                Passed = false,
                Message = $"Build test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private static async Task<TestResult> TestAndroidApkCreated()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("📦 Checking for APK file...");

            await Task.Delay(100); // Small delay to ensure file system sync

            var apkPath = Path.Combine(ProjectRoot, "bin", "Debug", "net9.0-android");

            if (!Directory.Exists(apkPath))
            {
                return new TestResult
                {
                    TestName = "APK File Created",
                    Passed = false,
                    Message = "APK output directory does not exist",
                    Details = $"Expected: {apkPath}",
                    Duration = stopwatch.Elapsed
                };
            }

            var apkFiles = Directory.GetFiles(apkPath, "*.apk", SearchOption.AllDirectories);

            if (apkFiles.Length == 0)
            {
                return new TestResult
                {
                    TestName = "APK File Created",
                    Passed = false,
                    Message = "No APK files found in output directory",
                    Details = $"Searched in: {apkPath}",
                    Duration = stopwatch.Elapsed
                };
            }

            var apkFile = apkFiles[0];
            var fileInfo = new FileInfo(apkFile);
            var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);

            stopwatch.Stop();
            return new TestResult
            {
                TestName = "APK File Created",
                Passed = true,
                Message = $"APK created successfully ({sizeInMB:F2} MB)",
                Details = Path.GetFileName(apkFile),
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new TestResult
            {
                TestName = "APK File Created",
                Passed = false,
                Message = $"APK check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private static async Task<TestResult> TestDeviceConnected()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("📱 Checking for connected Android device...");

            var startInfo = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = "devices",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await Task.Run(() => process.WaitForExit(5000));
            stopwatch.Stop();

            var lines = output.Split('\n')
                .Where(l => l.Contains("device") && !l.Contains("List of devices"))
                .ToArray();

            if (lines.Length == 0)
            {
                return new TestResult
                {
                    TestName = "Device Connected",
                    Passed = false,
                    Message = "No Android devices connected",
                    Details = "Connect a device via USB and enable USB debugging",
                    Duration = stopwatch.Elapsed
                };
            }

            var deviceId = lines[0].Split('\t')[0].Trim();
            return new TestResult
            {
                TestName = "Device Connected",
                Passed = true,
                Message = $"Device connected: {deviceId}",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new TestResult
            {
                TestName = "Device Connected",
                Passed = false,
                Message = $"Device check failed: {ex.Message}",
                Details = "Ensure ADB is installed and in PATH",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private static async Task<TestResult> TestAppInstallation()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("📲 Installing app on device...");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build -f net9.0-android -t:Install",
                WorkingDirectory = ProjectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            var output = "";

            process.OutputDataReceived += (sender, e) => { if (e.Data != null) output += e.Data + "\n"; };
            process.Start();
            process.BeginOutputReadLine();

            await Task.Run(() => process.WaitForExit(120000)); // 2 minute timeout
            stopwatch.Stop();

            if (!process.HasExited)
            {
                process.Kill();
                return new TestResult
                {
                    TestName = "App Installation",
                    Passed = false,
                    Message = "Installation timed out",
                    Duration = stopwatch.Elapsed
                };
            }

            // Check if package is installed
            var checkInstall = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = "shell pm list packages | grep penlink",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var checkProcess = new Process { StartInfo = checkInstall };
            checkProcess.Start();
            var checkOutput = await checkProcess.StandardOutput.ReadToEndAsync();
            await Task.Run(() => checkProcess.WaitForExit(5000));

            if (checkOutput.Contains("com.penlink.ezmiclink"))
            {
                return new TestResult
                {
                    TestName = "App Installation",
                    Passed = true,
                    Message = "App installed successfully on device",
                    Details = "Package: com.penlink.ezmiclink",
                    Duration = stopwatch.Elapsed
                };
            }
            else
            {
                return new TestResult
                {
                    TestName = "App Installation",
                    Passed = false,
                    Message = "App package not found on device after install",
                    Duration = stopwatch.Elapsed
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new TestResult
            {
                TestName = "App Installation",
                Passed = false,
                Message = $"Installation test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private static async Task<TestResult> TestAppLaunch()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("🚀 Testing app launch...");

            var startInfo = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = "shell monkey -p com.penlink.ezmiclink -c android.intent.category.LAUNCHER 1",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await Task.Run(() => process.WaitForExit(10000));
            stopwatch.Stop();

            if (output.Contains("Events injected: 1"))
            {
                return new TestResult
                {
                    TestName = "App Launch",
                    Passed = true,
                    Message = "App launched successfully",
                    Details = "Launch event injected via monkey",
                    Duration = stopwatch.Elapsed
                };
            }
            else
            {
                return new TestResult
                {
                    TestName = "App Launch",
                    Passed = false,
                    Message = "Failed to launch app",
                    Details = output.Length > 200 ? output.Substring(0, 200) : output,
                    Duration = stopwatch.Elapsed
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new TestResult
            {
                TestName = "App Launch",
                Passed = false,
                Message = $"Launch test failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════╗");
        Console.WriteLine("║  BUILD & INSTALLATION SANITY TESTS            ║");
        Console.WriteLine("╚════════════════════════════════════════════════╝\n");

        Console.WriteLine($"Project Root: {ProjectRoot}\n");

        var results = await RunTestsAsync();

        int passed = 0;
        int failed = 0;

        foreach (var result in results)
        {
            if (result.Passed)
            {
                Console.WriteLine($"✓ {result.TestName}: PASSED");
                Console.WriteLine($"  {result.Message}");
                if (!string.IsNullOrEmpty(result.Details))
                {
                    Console.WriteLine($"  {result.Details}");
                }
                Console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F1}s\n");
                passed++;
            }
            else
            {
                Console.WriteLine($"✗ {result.TestName}: FAILED");
                Console.WriteLine($"  {result.Message}");
                if (!string.IsNullOrEmpty(result.Details))
                {
                    Console.WriteLine($"  {result.Details}");
                }
                Console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F1}s\n");
                failed++;
            }
        }

        Console.WriteLine("════════════════════════════════════════════════");
        Console.WriteLine($"Total: {results.Length} | Passed: {passed} | Failed: {failed}");
        Console.WriteLine("════════════════════════════════════════════════\n");

        if (failed > 0)
        {
            Console.WriteLine("❌ BUILD/INSTALL TESTS FAILED - Fix issues before deploying!");
            return 1;
        }
        else
        {
            Console.WriteLine("✅ ALL BUILD/INSTALL TESTS PASSED - Safe to deploy!");
            return 0;
        }
    }
}
