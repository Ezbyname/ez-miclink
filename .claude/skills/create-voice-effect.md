# Create Voice Effect Skill

## Description
This skill helps you create new voice effects for the E-z MicLink audio app with proper DSP implementation.

## Usage
```
/create-voice-effect [effect-name]
```

## What This Skill Does

When invoked, this skill will:

1. **Ask for effect parameters** - Guide you through defining the effect characteristics
2. **Create DSP class** - Generate a new effect class in `Audio/DSP/`
3. **Update AudioEngine** - Add the effect to available presets
4. **Add to settings** - Update SettingsPage to include the new effect
5. **Document** - Create documentation for the effect

## Effect Parameters You'll Define

### Basic Information
- **Effect name** (e.g., "Underwater", "Alien", "Cave")
- **Description** (what it sounds like)
- **Category** (Voice, Ambient, Special FX)

### DSP Components
- **Pitch shift** (semitones, -12 to +12)
- **Formant shift** (%, -20 to +20)
- **EQ bands** (frequency, gain, Q)
- **Compression** (ratio, threshold, attack, release)
- **Reverb** (room size, decay, wet/dry)
- **Distortion** (amount, type)
- **Modulation** (type, rate, depth)

### Performance
- **CPU target** (low, medium, high)
- **Latency tolerance** (low, balanced, high quality)

## Example Effects You Can Create

### 🌊 Underwater Voice
- Low-pass filter @ 2kHz
- Pitch shift: -2 semitones
- Formant shift: -10%
- Slow modulation (LFO @ 0.5Hz)
- Reverb: large room

### 👽 Alien Voice
- Ring modulation @ 40Hz
- Pitch shift: +3 semitones
- Formant shift: +15%
- Resonant filter sweep

### 🗣️ Telephone Voice
- Band-pass: 300Hz - 3.4kHz
- Slight distortion
- No reverb
- Light compression

### 🎪 Carnival Announcer
- Pitch shift: -1 semitone
- Heavy compression (8:1)
- Mid boost @ 1.5kHz
- Short reverb

## Technical Implementation

The skill will create a class structure like:

```csharp
namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// [Effect Name] voice effect
///
/// [Description of what the effect does]
///
/// DSP Chain:
/// 1. [Component 1]
/// 2. [Component 2]
/// ...
/// </summary>
public class [EffectName]Effect : IAudioEffect
{
    private SimplePitchShifter? _pitchShifter;
    private BiquadFilter[] _eqFilters;
    private CompressorEffect? _compressor;
    // ... other components

    public void Initialize(int sampleRate)
    {
        // Initialize DSP components
    }

    public void Process(float[] buffer, int offset, int length)
    {
        // Process audio in real-time
    }

    // Parameter setters
}
```

## Preset Integration

The skill will also add a preset builder to AudioEngine:

```csharp
private void Build[EffectName]Preset()
{
    _currentChain.Clear();

    // Add noise gate
    _currentChain.Add(_noiseGate);

    // Add your effect
    var effect = new [EffectName]Effect();
    effect.Initialize(_sampleRate);
    _currentChain.Add(effect);

    // Add compression
    _currentChain.Add(_compressor);

    // Add limiter
    _currentChain.Add(_limiter);
}
```

## Available DSP Building Blocks

You can use these existing components:

### Pitch & Formant
- `SimplePitchShifter` - Real-time pitch shifting (±12 semitones)
- Formant shifting via biquad filter chains

### Filters (BiquadFilter)
- Low-pass
- High-pass
- Band-pass
- Notch
- Low-shelf
- High-shelf
- Peaking EQ

### Dynamics
- `CompressorEffect` - RMS compressor with soft knee
- `LimiterEffect` - Brick-wall limiter with lookahead
- `NoiseGateEffect` - Gate with adjustable threshold

### Effects
- `EchoDelayEffect` - Echo/delay with feedback
- Ring modulation (in KaraokeEffect)
- Reverb (Schroeder reverb in KaraokeEffect)

### Distortion
- Soft clipping
- Hard clipping
- Saturation
- Bit crushing (can be added)

## Performance Targets

| CPU Budget | Suitable Components | Typical Effects |
|------------|-------------------|-----------------|
| Low (5-10%) | EQ, simple filters, compression | Telephone, Hi-Fi, Clean |
| Medium (10-20%) | Pitch shift, reverb, modulation | Most voice effects |
| High (20-30%) | Multiple pitch shifters, vocoders | Complex transformations |

## Best Practices

1. **Always initialize in Initialize()** - Never allocate in Process()
2. **Clamp parameters** - Prevent invalid values
3. **Use pre-allocated buffers** - Zero allocations in audio thread
4. **Document DSP theory** - Explain what the effect does
5. **Provide typical values** - Help users understand parameters
6. **Test CPU usage** - Profile on target device
7. **Handle silence** - Don't process when input is quiet

## After Creation

The skill will:
1. ✅ Create the effect class file
2. ✅ Update AudioEngine with new preset
3. ✅ Add to voice preset picker in SettingsPage
4. ✅ Create documentation file
5. ✅ Run build to verify compilation
6. ✅ Provide testing instructions

## Examples

### Create a simple effect
```
/create-voice-effect cave
```

This will prompt you for parameters and create a cave reverb effect.

### Create with parameters
```
/create-voice-effect alien --pitch +3 --formant +15 --modulation ring:40hz
```

This creates an alien voice with specified parameters.

## Tips

- Start simple with EQ and compression
- Add pitch/formant for dramatic changes
- Use reverb for spatial effects
- Add modulation for movement/character
- Test on real voice input
- Iterate based on sound quality
- Consider CPU budget for real-time use

## Related Skills

- `/equalizer` - Create custom EQ presets
- `/audio-preset` - Manage and combine effects
- `/test-audio` - Test effects with sample audio

---

**Skill Version:** 1.0
**Last Updated:** 2026-02-21
**Status:** Active
