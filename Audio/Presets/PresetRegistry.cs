using System;
using System.Collections.Generic;
using System.Linq;

namespace BluetoothMicrophoneApp.Audio.Presets;

/// <summary>
/// Central registry for all audio presets.
///
/// DESIGN PATTERN: Registry Pattern + Factory Pattern
///
/// BENEFITS:
/// - Single source of truth for available presets
/// - Open/Closed Principle: Add presets by registration, no code modification
/// - Supports preset discovery (list all available)
/// - Enables preset categories and filtering
/// - Thread-safe registration and lookup
///
/// USAGE:
/// ```csharp
/// // Registration (during app startup)
/// var registry = new PresetRegistry();
/// registry.Register(new PodcastPreset());
/// registry.Register(new RobotPreset());
///
/// // Lookup and apply
/// registry.ApplyPreset("podcast", effectChain, sampleRate);
/// ```
/// </summary>
public class PresetRegistry
{
    private readonly Dictionary<string, IAudioPreset> _presets;
    private readonly object _lock = new object();

    public PresetRegistry()
    {
        _presets = new Dictionary<string, IAudioPreset>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Register a preset in the registry.
    /// </summary>
    /// <param name="preset">Preset to register</param>
    /// <exception cref="ArgumentException">If preset with same name already registered</exception>
    public void Register(IAudioPreset preset)
    {
        if (preset == null)
            throw new ArgumentNullException(nameof(preset));

        lock (_lock)
        {
            if (_presets.ContainsKey(preset.Name))
                throw new ArgumentException($"Preset '{preset.Name}' is already registered");

            _presets[preset.Name] = preset;
            System.Diagnostics.Debug.WriteLine($"[PresetRegistry] Registered preset: {preset.Name}");
        }
    }

    /// <summary>
    /// Check if a preset exists.
    /// </summary>
    public bool Contains(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        lock (_lock)
        {
            return _presets.ContainsKey(name);
        }
    }

    /// <summary>
    /// Get a preset by name.
    /// </summary>
    /// <returns>The preset, or null if not found</returns>
    public IAudioPreset? GetPreset(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        lock (_lock)
        {
            _presets.TryGetValue(name, out var preset);
            return preset;
        }
    }

    /// <summary>
    /// Apply a preset to an effect chain.
    /// </summary>
    /// <param name="name">Preset name</param>
    /// <param name="chain">Effect chain to configure</param>
    /// <param name="sampleRate">Sample rate for effect preparation</param>
    /// <exception cref="ArgumentException">If preset not found</exception>
    public void ApplyPreset(string name, DSP.AudioEffectChain chain, int sampleRate)
    {
        var preset = GetPreset(name);
        if (preset == null)
            throw new ArgumentException($"Preset '{name}' not found. Available presets: {string.Join(", ", GetAllPresetNames())}");

        System.Diagnostics.Debug.WriteLine($"[PresetRegistry] Applying preset: {preset.DisplayName}");
        preset.Configure(chain, sampleRate);
    }

    /// <summary>
    /// Get all registered preset names.
    /// </summary>
    public List<string> GetAllPresetNames()
    {
        lock (_lock)
        {
            return _presets.Keys.ToList();
        }
    }

    /// <summary>
    /// Get all registered presets.
    /// </summary>
    public List<IAudioPreset> GetAllPresets()
    {
        lock (_lock)
        {
            return _presets.Values.ToList();
        }
    }

    /// <summary>
    /// Get presets by category.
    /// </summary>
    public List<IAudioPreset> GetPresetsByCategory(string category)
    {
        lock (_lock)
        {
            return _presets.Values
                .Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <summary>
    /// Get all free (non-premium) presets.
    /// </summary>
    public List<IAudioPreset> GetFreePresets()
    {
        lock (_lock)
        {
            return _presets.Values.Where(p => !p.IsPremium).ToList();
        }
    }

    /// <summary>
    /// Get all premium presets.
    /// </summary>
    public List<IAudioPreset> GetPremiumPresets()
    {
        lock (_lock)
        {
            return _presets.Values.Where(p => p.IsPremium).ToList();
        }
    }

    /// <summary>
    /// Get all unique categories.
    /// </summary>
    public List<string> GetAllCategories()
    {
        lock (_lock)
        {
            return _presets.Values
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }

    /// <summary>
    /// Unregister a preset (for testing or dynamic loading).
    /// </summary>
    public bool Unregister(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        lock (_lock)
        {
            return _presets.Remove(name);
        }
    }

    /// <summary>
    /// Clear all registered presets.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _presets.Clear();
        }
    }

    /// <summary>
    /// Get count of registered presets.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _presets.Count;
            }
        }
    }
}
