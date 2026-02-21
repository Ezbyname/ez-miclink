# E-z MicLink Audio Skills

Custom skills for creating and managing professional voice effects in the E-z MicLink app.

## Available Skills

### 🔍 `/discover-voice-effects` - Discover Trending Voice Effects (NEW!)

Launches an AI agent that explores websites and suggests new voice effects to implement.

**Usage:**
```
/discover-voice-effects
/discover-voice-effects --focus gaming
/discover-voice-effects --category character-voices
```

**What it does:**
- Searches trending voice effects on TikTok, YouTube, gaming platforms
- Analyzes what's popular (voicemod, discord voice changers, etc.)
- Generates implementation specs with DSP parameters
- Prioritizes by popularity and difficulty
- Creates comprehensive report with code examples

**Perfect for:**
- Staying current with trends
- Finding popular effects to implement
- Competitive analysis
- Market research
- Product roadmap planning

**Automation:**
- Can run automatically every 3 days
- GitHub Actions, Azure Functions, or cron jobs
- See `AUTOMATION_SETUP.md` for details

**Output:**
- Detailed markdown report with 10-15 effect suggestions
- DSP implementation specs for each
- Popularity rankings
- Difficulty ratings
- Priority recommendations

**Example discoveries:**
- 🎌 Anime voice filter (very trending on TikTok)
- 👹 Demon/devil voice (popular in gaming)
- 🎵 Auto-tune effect (music/karaoke demand)
- 👻 Ghost/ethereal voice (horror content)
- 👶 Baby voice (comedy/pet videos)

**See:** `discover-voice-effects.md` for full documentation

---

### 🎤 `/create-voice-effect` - Create New Voice Effects

Create custom voice effects with professional DSP implementation.

**Usage:**
```
/create-voice-effect underwater
/create-voice-effect alien --pitch +3 --formant +15
/create-voice-effect [name]
```

**What it does:**
- Guides you through effect parameters (pitch, formant, EQ, reverb, etc.)
- Creates new effect class in `Audio/DSP/`
- Updates AudioEngine with new preset
- Adds to settings UI
- Generates documentation

**Perfect for:**
- Creating themed voice effects (underwater, cave, space)
- Experimenting with new voice transformations
- Building custom effects for specific use cases

**See:** `create-voice-effect.md` for full documentation

---

### 🎚️ `/equalizer` - Custom EQ Presets

Create and manage parametric/graphic equalizer presets with visual feedback.

**Usage:**
```
/equalizer vocal-clarity
/equalizer --bands 5
/equalizer --type graphic
```

**What it does:**
- Creates professional EQ presets
- Builds EQ UI in Settings page
- Provides frequency reference guide
- Real-time visual frequency response
- Save/load custom EQ curves

**Includes presets for:**
- Vocal clarity
- Bass boost
- Radio voice
- Telephone
- Bright & airy

**Perfect for:**
- Fine-tuning voice tone
- Removing problem frequencies
- Enhancing voice presence
- Creating signature sound

**See:** `equalizer.md` for frequency guide and best practices

---

### 🎛️ `/audio-preset` - Preset Management

Create, manage, and share complete effect chain presets.

**Usage:**
```
/audio-preset create "My Radio Voice"
/audio-preset edit podcast-pro
/audio-preset export alien-voice
/audio-preset list
```

**What it does:**
- Combines multiple effects into presets
- Manages preset library
- Export/import presets (JSON)
- Share via QR code
- Preset recommendations

**Included templates:**
- Podcast Pro
- Live Stage
- Radio Announcer
- Sci-Fi Robot
- Stadium/Karaoke

**Perfect for:**
- Building complete voice processing chains
- Saving favorite combinations
- Sharing presets with others
- Professional audio workflows

**See:** `audio-preset.md` for preset templates and examples

---

## Quick Start

### 1. Create a Simple Voice Effect

```
/create-voice-effect cave
```

Claude will guide you through:
- Effect type (reverb, pitch, etc.)
- Parameters (room size, decay, etc.)
- CPU budget
- Testing

Result: New `CaveEffect.cs` + preset in AudioEngine

### 2. Design Custom EQ

```
/equalizer my-voice
```

Claude will create:
- Parametric EQ UI
- 5-10 band controls
- Visual frequency response
- Save/load functionality

Result: Custom EQ preset in Settings

### 3. Build Complete Preset

```
/audio-preset create "Podcast Master"
```

Claude will help you:
- Choose effects chain
- Set parameters
- Test and refine
- Save preset

Result: Full preset in preset library

---

## Skill Workflow

### Creating a Professional Voice Preset

**Step 1:** Create individual effects
```
/create-voice-effect warm-bass --pitch -2 --formant -5
```

**Step 2:** Design custom EQ
```
/equalizer broadcast --boost-presence
```

**Step 3:** Combine into preset
```
/audio-preset create "Radio Host"
```

**Step 4:** Test and refine
- Apply preset in app
- Adjust parameters
- Save final version

**Step 5:** Share
```
/audio-preset export radio-host
```

---

## Skill Architecture

### How Skills Work

1. **Skill Definition** (`.md` files)
   - Instructions for Claude
   - Parameter specifications
   - Implementation templates
   - Best practices

2. **Code Generation**
   - Creates C# effect classes
   - Updates AudioEngine
   - Modifies UI files
   - Generates documentation

3. **Integration**
   - Registers in MauiProgram
   - Adds to Settings UI
   - Updates preset picker
   - Builds and verifies

### File Locations

```
BluetoothMicrophoneApp/
├── .claude/
│   └── skills/
│       ├── create-voice-effect.md
│       ├── equalizer.md
│       ├── audio-preset.md
│       └── README.md (this file)
│
├── Audio/
│   └── DSP/
│       ├── [YourEffect].cs          ← Created by skills
│       ├── AudioEngine.cs           ← Updated by skills
│       └── EqualizerEffect.cs       ← Created by /equalizer
│
├── Pages/
│   └── SettingsPage.xaml(.cs)       ← Updated by skills
│
└── Services/
    └── PresetManager.cs              ← Created by /audio-preset
```

---

## Examples

### Example 1: Underwater Voice

```
User: /create-voice-effect underwater

Claude: Let me help you create an underwater voice effect!

What characteristics should it have?
1. Pitch: Lower (-2 semitones)
2. Formant: Lower (-10%)
3. Low-pass filter at 2kHz
4. Slow modulation (LFO at 0.5Hz)
5. Medium reverb

Does this sound right?

User: Yes, and add some bubbling sound

Claude: Great! I'll add:
- Ring modulation at 20Hz (bubbling)
- Slight distortion for water texture

Creating UnderwaterEffect.cs...
✓ Created Audio/DSP/UnderwaterEffect.cs
✓ Updated AudioEngine.cs (BuildUnderwaterPreset)
✓ Updated SettingsPage voice picker
✓ Generated UNDERWATER_EFFECT.md docs

Build succeeded! ✓

Try it:
audioEngine.SetPreset("underwater");
```

### Example 2: Broadcast EQ

```
User: /equalizer broadcast

Claude: Creating broadcast EQ preset!

Professional broadcast EQ typically has:
- High-pass at 80Hz (remove rumble)
- Bass warmth at 150Hz (+3dB)
- Body at 250Hz (+2dB)
- Presence at 3kHz (+4dB)
- Air at 10kHz (+2dB)

Creating EQ controls...
✓ Created EqualizerEffect.cs
✓ Added EQ UI to SettingsPage
✓ Created "Broadcast" preset
✓ Visual frequency response graph

Your EQ is ready! Access via Settings → EQ Presets
```

### Example 3: Complete Podcast Chain

```
User: /audio-preset create "Podcast Pro Max"

Claude: Let's build a professional podcast preset!

Recommended chain:
1. Noise Gate (remove silence)
2. Pre-EQ (shape input)
3. De-esser (reduce sibilance)
4. Compressor (control dynamics)
5. Post-EQ (final tone)
6. Limiter (safety)

Would you like to customize?

User: Add some warmth and make it punchier

Claude: Perfect! I'll:
- Increase low-mid warmth (+3dB @ 250Hz)
- Boost presence (+4dB @ 3kHz)
- Tighten compression (4:1 ratio)

Creating preset...
✓ Created preset JSON
✓ Saved to PresetManager
✓ Added to preset library
✓ Ready to use!

Apply with:
audioEngine.ApplyPreset("podcast-pro-max");
```

---

## Best Practices

### When to Use Each Skill

**Use `/create-voice-effect` when:**
- Creating new character voices
- Building themed effects
- Experimenting with transformations
- Need unique voice processing

**Use `/equalizer` when:**
- Tone shaping and fine-tuning
- Correcting frequency problems
- Enhancing specific ranges
- Professional mixing tasks

**Use `/audio-preset` when:**
- Combining multiple effects
- Creating complete workflows
- Saving favorite chains
- Sharing configurations

### Performance Tips

1. **Start simple** - Add complexity gradually
2. **Profile CPU** - Test on target device
3. **Order matters** - Follow recommended signal chain
4. **Test with real voice** - Not just sine waves
5. **Save often** - Iterate and refine

### Signal Chain Order

Standard order for best results:

1. ✅ Noise Gate (first!)
2. ✅ Pre-EQ
3. ✅ Pitch/Formant shifting
4. ✅ Distortion/Saturation
5. ✅ Post-EQ
6. ✅ Reverb/Delay
7. ✅ Compressor
8. ✅ Limiter (last!)

---

## Troubleshooting

### "Skill not found"

Make sure skill files are in `.claude/skills/` directory.

### "Effect won't compile"

Skills generate code that should compile, but check:
- Namespaces are correct
- All DSP components exist
- No syntax errors in generated code

### "Effect sounds bad"

Common issues:
- Too much gain/boost
- Extreme parameter values
- Wrong effect order
- CPU overload causing artifacts

Try:
- Reduce effect intensity
- Use recommended parameters
- Check signal chain order
- Profile CPU usage

### "Preset won't load"

Check:
- JSON format is valid
- All effect types exist
- Parameters are in valid range
- Preset saved correctly

---

## Advanced Usage

### Chaining Skills

Create complex setups by chaining skills:

```bash
# 1. Create base effect
/create-voice-effect horror-voice --pitch -6

# 2. Design matching EQ
/equalizer horror-dark --cut-highs --boost-lows

# 3. Combine into preset
/audio-preset create "Horror Master"
```

### Batch Processing

Create multiple variations:

```bash
/create-voice-effect monster-1 --pitch -8
/create-voice-effect monster-2 --pitch -6 --distortion 0.3
/create-voice-effect monster-3 --pitch -4 --ring-mod 30hz
```

### Preset Libraries

Build themed libraries:

```bash
# Sci-Fi Pack
/audio-preset create "Robot A" --ring-mod
/audio-preset create "Robot B" --vocoder
/audio-preset create "Alien" --weird-pitch

# Broadcast Pack
/audio-preset create "News Anchor"
/audio-preset create "Sports Commentator"
/audio-preset create "Radio DJ"
```

---

## Contributing

### Adding New Skills

To add a new skill:

1. Create `.md` file in `.claude/skills/`
2. Follow existing skill format
3. Include usage examples
4. Document parameters
5. Provide implementation templates
6. Test with Claude

### Skill Template

```markdown
# [Skill Name] Skill

## Description
[What this skill does]

## Usage
```
/skill-name [args]
```

## What This Skill Does
[Detailed explanation]

## Examples
[Usage examples]

## Implementation
[Code templates and patterns]

---
**Skill Version:** 1.0
**Last Updated:** YYYY-MM-DD
**Status:** Active
```

---

## Resources

### DSP Reference
- BiquadFilter types and calculations
- Pitch shifting algorithms
- Formant preservation techniques
- Compression curves
- Reverb algorithms

### Audio Engineering
- Frequency reference chart
- EQ best practices
- Signal chain order
- CPU optimization
- Real-time processing

### MAUI Integration
- AudioEngine architecture
- Effect interface (IAudioEffect)
- Settings page structure
- Preferences API
- Service registration

---

## Support

For help with skills:
1. Read skill documentation (`.md` files)
2. Check examples in this README
3. Review generated code
4. Test incrementally
5. Ask Claude for guidance

---

**Skills Version:** 1.1
**Last Updated:** 2026-02-21
**Total Skills:** 4 (discover-voice-effects, create-voice-effect, equalizer, audio-preset)
**Status:** ✅ Active and ready to use
