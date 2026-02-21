# Equalizer Skill

## Description
This skill helps you create and manage custom equalizer presets with visual frequency response and professional audio engineering guidance.

## Usage
```
/equalizer [preset-name]
/equalizer --bands 5
/equalizer --type graphic
/equalizer --import [file]
```

## What This Skill Does

When invoked, this skill will:

1. **Create EQ UI** - Build a visual equalizer interface in SettingsPage
2. **Generate EQ presets** - Create professional EQ curves
3. **Save/Load presets** - Manage custom EQ configurations
4. **Real-time preview** - Apply EQ changes immediately
5. **Visual feedback** - Show frequency response curve

## Equalizer Types

### Parametric EQ (Recommended)
- **Bands**: 3-10 adjustable bands
- **Controls**: Frequency, Gain, Q (bandwidth)
- **Use case**: Professional mixing, surgical corrections
- **CPU**: Low (one biquad per band)

### Graphic EQ
- **Bands**: Fixed frequencies (31Hz, 63Hz, 125Hz... 16kHz)
- **Controls**: Gain only (±12dB)
- **Use case**: Quick tone shaping, live sound
- **CPU**: Medium (10-31 biquads)

### Shelf EQ
- **Bands**: 2 (low and high shelf)
- **Controls**: Frequency, Gain, Slope
- **Use case**: Basic tone control, mastering
- **CPU**: Very low (2 biquads)

## Standard EQ Presets

### 🎤 Vocal Clarity
```
Bass Cut:    80Hz, -3dB, Q=0.7 (remove rumble)
Body Boost:  250Hz, +2dB, Q=1.0 (warmth)
Presence:    3kHz, +3dB, Q=2.0 (clarity)
Air:         10kHz, +2dB, Q=0.7 (sparkle)
```

### 📻 Radio Voice
```
High-pass:   300Hz (remove low end)
Mid Boost:   1.5kHz, +4dB, Q=2.0 (presence)
Cut:         3kHz, -2dB, Q=1.5 (reduce harshness)
High Roll:   5kHz, -6dB, shelf (muffled)
```

### 🎵 Bass Boost
```
Sub Boost:   60Hz, +4dB, Q=1.0
Low Boost:   120Hz, +3dB, Q=1.0
Mid Cut:     400Hz, -2dB, Q=0.7 (more bass impact)
```

### ✨ Bright & Airy
```
High Shelf:  5kHz, +4dB, Q=0.7 (brightness)
Air Boost:   12kHz, +3dB, Q=1.0 (air)
```

### 📞 Telephone
```
High-pass:   300Hz (no bass)
Low-pass:    3.4kHz (no treble)
Peak:        1kHz, +3dB, Q=2.0 (telephone character)
```

## Frequency Reference Guide

| Frequency Range | Description | Typical Use |
|----------------|-------------|-------------|
| **20-60 Hz** | Sub-bass, rumble | Remove unless needed for effect |
| **60-250 Hz** | Bass, warmth, body | Boost for depth, cut for clarity |
| **250-500 Hz** | Low mids, muddiness | Often needs cutting |
| **500Hz-2kHz** | Mids, presence | Critical for vocal intelligibility |
| **2-5 kHz** | Upper mids, clarity | Boost for presence, harsh if too much |
| **5-10 kHz** | Brilliance, sibilance | Add sparkle, cut if harsh |
| **10-20 kHz** | Air, shimmer | Subtle boosts add "air" |

## Q Factor Guide

| Q Value | Bandwidth | Use Case |
|---------|-----------|----------|
| **0.3-0.7** | Very wide | Shelving, gentle tone shaping |
| **0.7-1.5** | Wide | Musical boosts/cuts |
| **1.5-3.0** | Medium | Targeted corrections |
| **3.0-10** | Narrow | Surgical notching, feedback removal |

## Implementation Structure

### EQ Manager Class
```csharp
public class EqualizerManager
{
    public class EQBand
    {
        public float Frequency { get; set; }
        public float Gain { get; set; }      // dB, -12 to +12
        public float Q { get; set; }         // 0.3 to 10
        public FilterType Type { get; set; } // Peak, LowShelf, HighShelf
    }

    public class EQPreset
    {
        public string Name { get; set; }
        public List<EQBand> Bands { get; set; }
        public string Description { get; set; }
    }

    public void ApplyPreset(string presetName);
    public void SaveCustomPreset(string name, List<EQBand> bands);
    public List<EQPreset> GetAllPresets();
    public void SetBand(int index, float freq, float gain, float q);
}
```

### UI Components

#### Parametric EQ Sliders
```xml
<!-- For each EQ band -->
<Grid ColumnDefinitions="Auto,*,Auto,Auto,Auto">
    <!-- Band number -->
    <Label Text="Band 1" />

    <!-- Frequency slider (20Hz - 20kHz, log scale) -->
    <Slider x:Name="FreqSlider" Minimum="20" Maximum="20000" />
    <Label x:Name="FreqLabel" Text="1000Hz" />

    <!-- Gain slider (-12dB to +12dB) -->
    <Slider x:Name="GainSlider" Minimum="-12" Maximum="12" />
    <Label x:Name="GainLabel" Text="+0dB" />

    <!-- Q slider (0.3 to 10) -->
    <Slider x:Name="QSlider" Minimum="0.3" Maximum="10" />
    <Label x:Name="QLabel" Text="Q:1.0" />
</Grid>
```

#### Graphic EQ Sliders
```xml
<HorizontalStackLayout Spacing="8">
    <!-- 31Hz -->
    <VerticalStackLayout>
        <Slider Orientation="Vertical"
                Minimum="-12" Maximum="12"
                ValueChanged="OnEQChanged" />
        <Label Text="31Hz" />
    </VerticalStackLayout>

    <!-- 63Hz -->
    <VerticalStackLayout>
        <Slider Orientation="Vertical"
                Minimum="-12" Maximum="12" />
        <Label Text="63Hz" />
    </VerticalStackLayout>

    <!-- ... more bands ... -->
</HorizontalStackLayout>
```

### Preset Storage
```csharp
// Save preset to Preferences
var preset = new EQPreset
{
    Name = "My EQ",
    Bands = new List<EQBand>
    {
        new() { Frequency = 100, Gain = -3, Q = 0.7, Type = FilterType.Peak },
        new() { Frequency = 1000, Gain = +2, Q = 1.0, Type = FilterType.Peak },
        // ...
    }
};

string json = JsonSerializer.Serialize(preset);
Preferences.Set("eq_preset_my_eq", json);
```

## Visual Frequency Response

### ASCII Visualization (for console/debug)
```
dB
+12 ┤                    ╭╮
 +9 ┤                   ╭╯╰╮
 +6 ┤                  ╭╯  ╰╮
 +3 ┤        ╭────────╯    ╰────
  0 ┼────────╯
 -3 ┤    ╭╯
 -6 ┤   ╭╯
-12 ┤───╯
    └─┬──┬──┬──┬──┬──┬──┬──┬──┬
    20 50 100 500 1k 5k 10k 20k Hz
```

### Graph Component (MAUI)
```csharp
// Use Microsoft.Maui.Graphics to draw frequency response
public class FrequencyResponseView : GraphicsView
{
    public void DrawResponse(List<EQBand> bands)
    {
        // Calculate frequency response at each point
        // Draw curve with gradient fill
        // Add frequency labels
    }
}
```

## Audio Processing Integration

### Apply EQ to Audio Chain
```csharp
public class EqualizerEffect : IAudioEffect
{
    private BiquadFilter[] _filters; // One per band

    public void Initialize(int sampleRate)
    {
        _filters = new BiquadFilter[_bands.Count];
        for (int i = 0; i < _bands.Count; i++)
        {
            _filters[i] = new BiquadFilter();
            UpdateBand(i, _bands[i]);
        }
    }

    public void Process(float[] buffer, int offset, int length)
    {
        // Apply each filter in series
        foreach (var filter in _filters)
        {
            filter.Process(buffer, offset, length);
        }
    }

    public void UpdateBand(int index, EQBand band)
    {
        var filter = _filters[index];

        switch (band.Type)
        {
            case FilterType.Peak:
                filter.SetPeakingEQ(_sampleRate, band.Frequency, band.Gain, band.Q);
                break;
            case FilterType.LowShelf:
                filter.SetLowShelf(_sampleRate, band.Frequency, band.Gain, band.Q);
                break;
            case FilterType.HighShelf:
                filter.SetHighShelf(_sampleRate, band.Frequency, band.Gain, band.Q);
                break;
        }
    }
}
```

## Preset Examples with Code

### Create "Vocal Clarity" Preset
```csharp
var vocalClarity = new EQPreset
{
    Name = "Vocal Clarity",
    Description = "Enhance voice intelligibility and presence",
    Bands = new List<EQBand>
    {
        new() { Frequency = 80, Gain = -3, Q = 0.7, Type = FilterType.Peak },
        new() { Frequency = 250, Gain = +2, Q = 1.0, Type = FilterType.Peak },
        new() { Frequency = 3000, Gain = +3, Q = 2.0, Type = FilterType.Peak },
        new() { Frequency = 10000, Gain = +2, Q = 0.7, Type = FilterType.HighShelf },
    }
};
```

### Create "Bass Boost" Preset
```csharp
var bassBoost = new EQPreset
{
    Name = "Bass Boost",
    Description = "Enhanced low frequency response",
    Bands = new List<EQBand>
    {
        new() { Frequency = 60, Gain = +4, Q = 1.0, Type = FilterType.LowShelf },
        new() { Frequency = 120, Gain = +3, Q = 1.0, Type = FilterType.Peak },
        new() { Frequency = 400, Gain = -2, Q = 0.7, Type = FilterType.Peak },
    }
};
```

## EQ Best Practices

### Do's ✅
- Cut before boosting (cleaner sound)
- Use wide Q for musical adjustments
- High-pass below 80Hz (remove rumble)
- Boost presence (2-5kHz) for clarity
- Use narrow Q for problem frequency removal
- A/B test with bypass frequently
- Start subtle (+/-3dB), adjust by ear

### Don'ts ❌
- Don't boost every band (sounds unnatural)
- Don't use extreme Q unless needed (sounds phasey)
- Don't boost and cut same frequency range
- Don't EQ in isolation (test with full mix)
- Don't overdo high frequencies (fatiguing)
- Don't ignore phase issues (narrow Q creates phase shift)

## Performance Considerations

### CPU Usage
- Each biquad filter: ~0.5-1% CPU
- 5-band parametric EQ: ~3-5% CPU
- 10-band graphic EQ: ~5-10% CPU
- Real-time update: negligible

### Latency
- Biquad filters: <1 sample delay
- EQ chain: <1ms total
- No lookahead needed

## Integration with Voice Effects

### Combine EQ with Voice Effects
```csharp
// In preset builder
private void BuildCustomPreset()
{
    _currentChain.Clear();

    // 1. Pre-EQ (shape input)
    var preEQ = new EqualizerEffect();
    preEQ.LoadPreset("vocal_clarity");
    _currentChain.Add(preEQ);

    // 2. Voice effect (pitch, formant, etc.)
    var effect = new HeliumVoiceEffect();
    _currentChain.Add(effect);

    // 3. Post-EQ (shape output)
    var postEQ = new EqualizerEffect();
    postEQ.LoadPreset("brightness");
    _currentChain.Add(postEQ);

    // 4. Dynamics
    _currentChain.Add(_compressor);
    _currentChain.Add(_limiter);
}
```

## Testing & Tuning

### Test with Real Voice
1. Record 10-second voice sample
2. Apply EQ preset
3. Listen for:
   - Clarity improvements
   - Natural tone
   - No harshness
   - No muddiness
4. Adjust and re-test

### Use Reference Audio
- Compare to professional recordings
- Match frequency balance
- Don't over-process

### Frequency Sweeping
- Play sine sweep (20Hz-20kHz)
- Listen for resonances
- Use narrow cut to fix problems

## Advanced Features

### Auto-EQ
- Analyze voice spectrum
- Suggest EQ corrections
- One-click optimization

### EQ Matching
- Load reference audio
- Match its frequency response
- Apply to your voice

### Dynamic EQ
- EQ responds to input level
- Compress specific frequencies
- Advanced de-essing

## Related Skills

- `/create-voice-effect` - Create custom voice effects
- `/audio-preset` - Manage complete effect presets
- `/analyze-audio` - Spectrum analysis and recommendations

---

**Skill Version:** 1.0
**Last Updated:** 2026-02-21
**Status:** Active
