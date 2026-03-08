# Skill: Add Unique Sounds and Voices

## When to Use
When the user asks to add a new voice effect, character voice, sound preset, or modify an existing one.

## Architecture Overview

The app uses a DSP effect chain system:
1. **Effects** (`Audio/DSP/`) - Individual audio processors (EQ, pitch shift, reverb, etc.)
2. **Presets** (`Audio/Presets/`) - Named configurations that chain effects together
3. **PresetRegistry** (`Audio/Presets/PresetRegistry.cs`) - Thread-safe registry of all presets
4. **AudioEngine** (`Audio/DSP/AudioEngine.cs`) - Runs the effect chain in real-time on audio thread

## Step-by-Step: Adding a New Voice Preset

### Step 1: Create Preset Class

Create `Audio/Presets/{Name}Preset.cs`:

```csharp
using BluetoothMicrophoneApp.Audio.DSP;

namespace BluetoothMicrophoneApp.Audio.Presets;

public class MyNewPreset : AudioPresetBase
{
    public override string Id => "my_new_voice";
    public override string DisplayName => "My New Voice";
    public override string Category => "Character Voices"; // or "Professional", "Voice Effects", "Basic"
    public override bool IsPremium => false;

    protected override void ConfigureChain(AudioEffectChain chain, int sampleRate)
    {
        // 1. Always start with a noise gate
        var gate = new NoiseGateEffect();
        gate.Initialize(sampleRate);
        gate.ThresholdDb = -45f;
        gate.AttackMs = 1f;
        gate.ReleaseMs = 150f;
        chain.AddEffect(gate);

        // 2. Add voice effects (pitch, formant, character)
        var voice = new DeepVoiceEffect();
        voice.Initialize(sampleRate);
        voice.PitchSemitones = -3f;
        voice.FormantShiftPercent = -5f;
        voice.BassBoostDb = 3f;
        voice.Intensity = 0.9f;
        chain.AddEffect(voice);

        // 3. Shape with EQ
        var eq = new ThreeBandEQEffect();
        eq.Initialize(sampleRate);
        eq.SetLowShelf(150f, 2f);   // +2dB bass
        eq.SetMidPeak(1200f, 4f, 1.0f); // +4dB mid presence
        eq.SetHighShelf(5000f, -1f); // slight treble cut
        chain.AddEffect(eq);

        // 4. Compress dynamics
        var comp = new CompressorEffect();
        comp.Initialize(sampleRate);
        comp.ThresholdDb = -18f;
        comp.Ratio = 4f;
        comp.AttackMs = 10f;
        comp.ReleaseMs = 120f;
        chain.AddEffect(comp);

        // 5. Always end with a limiter (prevents clipping)
        var limiter = new LimiterEffect();
        limiter.Initialize(sampleRate);
        limiter.CeilingDb = -0.5f;
        chain.AddEffect(limiter);
    }
}
```

### Step 2: Register in PresetRegistry

Edit `Audio/Presets/PresetRegistry.cs`, add to the constructor:

```csharp
Register(new MyNewPreset());
```

### Step 3: Add to Available Effects List

Edit `Platforms/Android/Services/AudioService.cs` in `GetAvailableEffects()`:

```csharp
return new[] {
    "clean", "podcast", ...,
    "my_new_voice"  // add here
};
```

### Step 4: Add to Character Voices Page (if character voice)

Edit `UI/CharacterVoicesPage.xaml.cs` - add to the grid of voice buttons.

### Step 5: Add Sound Editor Defaults (if applicable)

Edit `UI/SoundEditorPage.xaml.cs` in `LoadPresetDefaults()`:

```csharp
case "my_new_voice":
    _defaultTone = 2f; _defaultSpace = 0f;
    _defaultBright = -1f; _defaultCharacter = 3f;
    break;
```

## Available DSP Effects (Building Blocks)

### Voice Pitch/Character
| Effect | Key Params | Use For |
|--------|-----------|---------|
| `DeepVoiceEffect` | Pitch (-12 to -1 semi), Formant (-20 to 0%), BassBoost (0-8dB) | Deep/low voices, giants, villains |
| `HeliumVoiceEffect` | Pitch (+2 to +12 semi), Formant (0-30%), Brightness (0-8dB) | High voices, mice, chipmunks, anime |
| `AnimeVoiceEffect` | Pitch (+2 to +10 semi), Formant (0-30%), Brightness + Air boost | Bright anime characters |
| `RobotVoiceEffect` | CarrierFreq (30-500Hz), Intensity (0-1.0), OctaveShift (-2 to +2) | Metallic/synthetic voices |
| `MegaphoneEffect` | LowCut (200-800Hz), HighCut (1500-5000Hz), Distortion (0-1.0) | Lo-fi, loudspeaker, radio |

### Spatial/Ambience
| Effect | Key Params | Use For |
|--------|-----------|---------|
| `KaraokeEffect` | RoomSize (0.3-1.0), DecayTime (0.3-4.5s), Mix (0-1.0) | Reverb, room ambience |
| `EchoDelayEffect` | Delay (10-2000ms), Feedback (0-0.95), Mix (0-1.0) | Echo, canyon, stadium |

### Dynamics & Tone
| Effect | Key Params | Use For |
|--------|-----------|---------|
| `ThreeBandEQEffect` | Low/Mid/High gain (-18 to +18dB), frequencies, Q | Tone shaping |
| `CompressorEffect` | Threshold (-60 to 0dB), Ratio (1-20:1), Attack/Release | Even out volume |
| `LimiterEffect` | Ceiling (-12 to -0.1dB), Lookahead (0-10ms) | Prevent clipping |
| `NoiseGateEffect` | Threshold (-60 to 0dB), Attack/Release | Cut background noise |
| `DeEsserEffect` | Frequency (3-15kHz), Threshold, Amount (0-1.0) | Reduce sibilance |
| `GainEffect` | Gain (0-2.0x) | Simple volume |

### Broadcast
| Effect | Key Params | Use For |
|--------|-----------|---------|
| `PodcastVoiceEffect` | HPF, Gate, De-esser, EQ, Compression, Limiter (all-in-one) | Professional broadcast chain |

## Voice Design Recipes

### Making a Voice Deeper
```
DeepVoiceEffect: Pitch -3 to -6 semi, Formant -5 to -12%, BassBoost 3-6dB
+ CompressorEffect: 3:1, -18dB (tighten dynamics)
+ ThreeBandEQ: +2dB @ 150Hz, -2dB @ 3kHz (warm, less nasal)
```

### Making a Voice Higher/Squeakier
```
HeliumVoiceEffect: Pitch +5 to +11 semi, Formant +15 to +30%, Brightness 4-8dB
+ CompressorEffect: 3:1, -15dB (control peaks)
```

### Adding Nasality (nerdy/villain character)
```
ThreeBandEQ: MidPeak +4 to +6dB @ 1000-1200Hz, Q=1.0-2.0
```

### Adding Warmth (cozy/radio voice)
```
ThreeBandEQ: LowShelf +3dB @ 180Hz, HighShelf -2dB @ 5kHz
+ CompressorEffect: 4:1, -18dB, slow attack (15ms)
```

### Adding Grit/Distortion
```
MegaphoneEffect: Distortion 0.3-0.7, LowCut 300-500Hz
-- OR --
Master Distortion via SoundEditor: Character slider 4-8
```

### Adding Space/Room
```
KaraokeEffect: RoomSize 0.5-0.8, DecayTime 0.5-1.5s, Mix 0.2-0.4
```

### Adding Echo
```
EchoDelayEffect: Delay 200-500ms, Feedback 0.3-0.5, Mix 0.3-0.4
```

## Creative Combination Ideas for New Voices

| Voice Idea | Effects Chain |
|------------|--------------|
| **Darth Vader** | DeepVoice(-4semi) + MegaphoneEffect(dist=0.3) + KaraokeEffect(room=0.3, decay=0.3) |
| **Fairy/Pixie** | Helium(+8semi, +20%formant) + KaraokeEffect(room=0.4, mix=0.15) + EQ(+3dB@8kHz) |
| **Underwater** | DeepVoice(-2semi) + EQ(-6dB@5kHz, +3dB@200Hz) + KaraokeEffect(room=0.8, damp=0.9) |
| **Walkie-Talkie** | MegaphoneEffect(lowCut=500, highCut=2500, dist=0.4) + CompressorEffect(8:1, -12dB) |
| **Ghost/Whisper** | Helium(+2semi) + KaraokeEffect(room=0.9, decay=3.0, mix=0.5) + EQ(-4dB@1kHz) |
| **Old Man** | DeepVoice(-2semi, -4%formant) + EQ(+3dB@1kHz, -3dB@5kHz) + slight tremolo via RobotVoice(carrier=6Hz, intensity=0.15) |
| **Evil Demon** | DeepVoice(-8semi) + MegaphoneEffect(dist=0.7) + EchoDelay(300ms, 0.4) + KaraokeEffect(room=0.6) |
| **Alien** | RobotVoice(carrier=200Hz, intensity=0.5) + Helium(+3semi) + EchoDelay(150ms, 0.3) |
| **Telephone** | MegaphoneEffect(lowCut=300, highCut=3400, dist=0.1) + Compressor(6:1) |
| **Stadium Announcer** | EQ(+3dB@2kHz) + Compressor(6:1, -15dB) + KaraokeEffect(room=1.0, decay=3.0) + EchoDelay(400ms) |

## Thread Safety Rules
- All effect parameters use `volatile` or atomic reads/writes
- Never allocate memory in `ProcessBuffer()` (runs on audio thread)
- Effect chain swaps are lock-free (Interlocked.Exchange)
- Initialize filters with `sampleRate` before use
- Preset's `ConfigureChain()` runs on UI thread, chain is swapped atomically

## File Locations
- Effects: `Audio/DSP/*.cs`
- Presets: `Audio/Presets/*.cs`
- Registry: `Audio/Presets/PresetRegistry.cs`
- Engine: `Audio/DSP/AudioEngine.cs`
- UI voices: `UI/CharacterVoicesPage.xaml.cs`
- Sound editor: `UI/SoundEditorPage.xaml.cs`
- Custom sounds: `Services/CustomSoundService.cs`
- Model: `Models/SavedSound.cs`
