using System.Collections.Concurrent;
using System.Diagnostics;

namespace BluetoothMicrophoneApp.Services;

public static class DebugLogger
{
    private static readonly ConcurrentQueue<string> _logs = new();
    private static readonly int MaxLogs = 100;

    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";

        _logs.Enqueue(logEntry);

        // Keep only last MaxLogs entries
        while (_logs.Count > MaxLogs)
        {
            _logs.TryDequeue(out _);
        }

        // Also write to system debug
        Debug.WriteLine(logEntry);
    }

    public static string GetLogs()
    {
        return string.Join("\n", _logs.Reverse());
    }

    public static void Clear()
    {
        _logs.Clear();
    }
}
