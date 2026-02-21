# Audio Preset Management Skill

## Description
This skill helps you create, manage, and combine multiple audio effects into complete presets for professional voice processing.

## Usage
```
/audio-preset create [name]
/audio-preset edit [name]
/audio-preset list
/audio-preset export [name]
/audio-preset import [file]
```

## What This Skill Does

When invoked, this skill will:

1. **Create preset chains** - Combine multiple effects in series
2. **Manage presets** - Save, load, edit, delete presets
3. **Export/Import** - Share presets with others
4. **Preset library** - Browse professional presets
5. **Real-time preview** - Test presets before saving

## Preset Structure

A preset is an **effect chain** - a series of effects applied in order:

```
Input → Effect 1 → Effect 2 → Effect 3 → Output
```

### Example Preset Chain
```
Raw Voice
   ↓
Noise Gate (remove silence)
   ↓
Pre-EQ (shape input)
   ↓
Voice Effect (pitch, formant)
   ↓
Post-EQ (final tone)
   ↓
Compressor (control dynamics)
   ↓
Limiter (prevent clipping)
   ↓
Output
```

## Professional Preset Templates

### 🎙️ Podcast Voice
```json
{
  "name": "Podcast Pro",
  "description": "Professional podcast sound",
  "chain": [
    {
      "type": "NoiseGate",
      "params": { "threshold": -50, "ratio": 10 }
    },
    {
      "type": "EQ",
      "params": {
        "bands": [
          { "freq": 80, "gain": -6, "q": 0.7 },
          { "freq": 250, "gain": 2, "q": 1.0 },
          { "freq": 3000, "gain": 3, "q": 2.0 }
        ]
      }
    },
    {
      "type": "Compressor",
      "params": { "ratio": 3, "threshold": -18, "attack": 10, "release": 100 }
    },
    {
      "type": "Limiter",
      "params": { "threshold": -1, "release": 50 }
    }
  ]
}
```

### 🎤 Live Performance
```json
{
  "name": "Live Stage",
  "description": "Bold vocal with presence",
  "chain": [
    {
      "type": "NoiseGate",
      "params": { "threshold": -40 }
    },
    {
      "type": "EQ",
      "params": {
        "bands": [
          { "freq": 100, "gain": -3 },
          { "freq": 2500, "gain": 4, "q": 1.5 },
          { "freq": 8000, "gain": 2 }
        ]
      }
    },
    {
      "type": "Compressor",
      "params": { "ratio": 4, "threshold": -20 }
    },
    {
      "type": "Reverb",
      "params": { "roomSize": 0.3, "wetDry": 0.25 }
    },
    {
      "type": "Limiter",
      "params": { "threshold": -0.5 }
    }
  ]
}
```

### 🤖 Sci-Fi Robot
```json
{
  "name": "Sci-Fi Robot",
  "description": "Robotic voice with modulation",
  "chain": [
    {
      "type": "RingModulation",
      "params": { "frequency": 120 }
    },
    {
      "type": "PitchShift",
      "params": { "semitones": -3 }
    },
    {
      "type": "EQ",
      "params": {
        "bands": [
          { "freq": 200, "gain": -6 },
          { "freq": 1500, "gain": 5, "q": 2.0 }
        ]
      }
    },
    {
      "type": "Distortion",
      "params": { "amount": 0.3, "type": "soft" }
    },
    {
      "type": "Limiter",
      "params": { "threshold": -1 }
    }
  ]
}
```

### 📻 Radio Announcer
```json
{
  "name": "Radio Announcer",
  "description": "Classic radio voice",
  "chain": [
    {
      "type": "NoiseGate",
      "params": { "threshold": -45 }
    },
    {
      "type": "PitchShift",
      "params": { "semitones": -2 }
    },
    {
      "type": "FormantShift",
      "params": { "shift": -8 }
    },
    {
      "type": "EQ",
      "params": {
        "bands": [
          { "freq": 150, "gain": 4, "q": 1.0 },
          { "freq": 3000, "gain": -3, "q": 1.5 },
          { "freq": 5000, "gain": -6, "type": "lowpass" }
        ]
      }
    },
    {
      "type": "Compressor",
      "params": { "ratio": 6, "threshold": -15 }
    },
    {
      "type": "Limiter",
      "params": { "threshold": -0.5 }
    }
  ]
}
```

## Preset Manager Implementation

### Data Model
```csharp
namespace BluetoothMicrophoneApp.Audio;

public class AudioPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Custom";
    public List<EffectConfig> Chain { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    public string Author { get; set; } = "User";
    public bool IsFavorite { get; set; }
}

public class EffectConfig
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
}
```

### Preset Manager Class
```csharp
public class PresetManager
{
    private const string PresetsKey = "audio_presets";
    private List<AudioPreset> _presets = new();

    public PresetManager()
    {
        LoadPresets();
    }

    public void LoadPresets()
    {
        string json = Preferences.Get(PresetsKey, "[]");
        _presets = JsonSerializer.Deserialize<List<AudioPreset>>(json) ?? new();
    }

    public void SavePresets()
    {
        string json = JsonSerializer.Serialize(_presets);
        Preferences.Set(PresetsKey, json);
    }

    public void CreatePreset(AudioPreset preset)
    {
        _presets.Add(preset);
        SavePresets();
    }

    public void UpdatePreset(string id, AudioPreset preset)
    {
        var index = _presets.FindIndex(p => p.Id == id);
        if (index >= 0)
        {
            preset.ModifiedAt = DateTime.Now;
            _presets[index] = preset;
            SavePresets();
        }
    }

    public void DeletePreset(string id)
    {
        _presets.RemoveAll(p => p.Id == id);
        SavePresets();
    }

    public AudioPreset? GetPreset(string id)
    {
        return _presets.FirstOrDefault(p => p.Id == id);
    }

    public List<AudioPreset> GetAllPresets()
    {
        return _presets.OrderBy(p => p.Name).ToList();
    }

    public List<AudioPreset> GetPresetsByCategory(string category)
    {
        return _presets.Where(p => p.Category == category).ToList();
    }

    public List<AudioPreset> GetFavorites()
    {
        return _presets.Where(p => p.IsFavorite).ToList();
    }

    public string ExportPreset(string id)
    {
        var preset = GetPreset(id);
        return preset != null ? JsonSerializer.Serialize(preset) : string.Empty;
    }

    public void ImportPreset(string json)
    {
        var preset = JsonSerializer.Deserialize<AudioPreset>(json);
        if (preset != null)
        {
            preset.Id = Guid.NewGuid().ToString(); // New ID to avoid conflicts
            CreatePreset(preset);
        }
    }
}
```

### Apply Preset to Audio Engine
```csharp
public class AudioEngine
{
    private PresetManager _presetManager;

    public void ApplyPreset(string presetId)
    {
        var preset = _presetManager.GetPreset(presetId);
        if (preset == null) return;

        _currentChain.Clear();

        foreach (var effectConfig in preset.Chain.OrderBy(e => e.Order))
        {
            if (!effectConfig.Enabled) continue;

            var effect = CreateEffect(effectConfig);
            if (effect != null)
            {
                effect.Initialize(_sampleRate);
                _currentChain.Add(effect);
            }
        }
    }

    private IAudioEffect? CreateEffect(EffectConfig config)
    {
        return config.Type switch
        {
            "NoiseGate" => CreateNoiseGate(config.Parameters),
            "EQ" => CreateEQ(config.Parameters),
            "PitchShift" => CreatePitchShift(config.Parameters),
            "FormantShift" => CreateFormantShift(config.Parameters),
            "Compressor" => CreateCompressor(config.Parameters),
            "Limiter" => CreateLimiter(config.Parameters),
            "Reverb" => CreateReverb(config.Parameters),
            "RingModulation" => CreateRingMod(config.Parameters),
            "Distortion" => CreateDistortion(config.Parameters),
            _ => null
        };
    }
}
```

## Preset Categories

### Voice Enhancement
- Clean Vocal
- Podcast Pro
- Interview
- Voice-Over
- Narration

### Performance
- Live Stage
- Karaoke
- Stadium
- Theater

### Voice Effects
- Chipmunk (Helium)
- Deep Voice
- Robot
- Alien
- Monster
- Cartoon

### Communication
- Radio
- Telephone
- Walkie-Talkie
- Megaphone
- CB Radio

### Ambient
- Cave
- Cathedral
- Underwater
- Space
- Wind Tunnel

## UI for Preset Management

### Preset Library Page
```xml
<ContentPage Title="Preset Library">
    <Grid RowDefinitions="Auto,*">

        <!-- Search and Filter -->
        <Grid Grid.Row="0" Padding="16">
            <SearchBar Placeholder="Search presets..." />
            <Picker x:Name="CategoryPicker"
                    Title="Category"
                    SelectedIndexChanged="OnCategoryChanged" />
        </Grid>

        <!-- Preset List -->
        <CollectionView Grid.Row="1"
                       ItemsSource="{Binding Presets}">
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <Border Margin="8" Padding="12">
                        <Grid ColumnDefinitions="*,Auto,Auto,Auto">

                            <!-- Preset Info -->
                            <VerticalStackLayout Grid.Column="0">
                                <Label Text="{Binding Name}"
                                       FontSize="16"
                                       FontAttributes="Bold" />
                                <Label Text="{Binding Description}"
                                       FontSize="14"
                                       TextColor="Gray" />
                                <Label Text="{Binding Category}"
                                       FontSize="12" />
                            </VerticalStackLayout>

                            <!-- Favorite -->
                            <Label Grid.Column="1"
                                   Text="⭐"
                                   IsVisible="{Binding IsFavorite}" />

                            <!-- Apply -->
                            <Button Grid.Column="2"
                                    Text="Apply"
                                    Command="{Binding ApplyCommand}" />

                            <!-- Options -->
                            <Button Grid.Column="3"
                                    Text="⋮"
                                    Command="{Binding OptionsCommand}" />
                        </Grid>
                    </Border>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
    </Grid>
</ContentPage>
```

### Preset Editor Page
```xml
<ContentPage Title="Edit Preset">
    <ScrollView>
        <VerticalStackLayout Padding="16" Spacing="16">

            <!-- Preset Info -->
            <Entry x:Name="NameEntry" Placeholder="Preset Name" />
            <Editor x:Name="DescriptionEditor"
                    Placeholder="Description"
                    HeightRequest="80" />
            <Picker x:Name="CategoryPicker" Title="Category" />

            <!-- Effect Chain -->
            <Label Text="Effect Chain" FontSize="18" FontAttributes="Bold" />

            <CollectionView x:Name="EffectChain"
                           ItemsSource="{Binding Effects}">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <Border Margin="4" Padding="12">
                            <Grid ColumnDefinitions="Auto,*,Auto,Auto,Auto">

                                <!-- Drag Handle -->
                                <Label Grid.Column="0" Text="☰" />

                                <!-- Effect Name -->
                                <Label Grid.Column="1"
                                       Text="{Binding Type}"
                                       VerticalOptions="Center" />

                                <!-- Enable/Disable -->
                                <Switch Grid.Column="2"
                                        IsToggled="{Binding Enabled}" />

                                <!-- Edit -->
                                <Button Grid.Column="3"
                                        Text="⚙️"
                                        Command="{Binding EditCommand}" />

                                <!-- Remove -->
                                <Button Grid.Column="4"
                                        Text="✕"
                                        Command="{Binding RemoveCommand}" />
                            </Grid>
                        </Border>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>

            <!-- Add Effect -->
            <Button Text="+ Add Effect"
                    Clicked="OnAddEffectClicked" />

            <!-- Actions -->
            <Grid ColumnDefinitions="*,*" ColumnSpacing="8">
                <Button Grid.Column="0"
                        Text="Preview"
                        Clicked="OnPreviewClicked" />
                <Button Grid.Column="1"
                        Text="Save Preset"
                        Clicked="OnSaveClicked" />
            </Grid>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

## Preset Sharing

### Export Format (JSON)
```json
{
  "preset": {
    "version": "1.0",
    "name": "My Custom Voice",
    "description": "Deep voice with reverb",
    "category": "Custom",
    "author": "John Doe",
    "created": "2026-02-21T15:30:00Z",
    "chain": [
      {
        "type": "PitchShift",
        "enabled": true,
        "order": 0,
        "params": {
          "semitones": -4
        }
      },
      {
        "type": "FormantShift",
        "enabled": true,
        "order": 1,
        "params": {
          "shift": -8
        }
      }
    ]
  }
}
```

### Share via QR Code
```csharp
public string GenerateShareCode(string presetId)
{
    var json = _presetManager.ExportPreset(presetId);
    var compressed = Compress(json);
    var base64 = Convert.ToBase64String(compressed);
    return base64; // Can be encoded in QR code
}

public void ImportFromShareCode(string code)
{
    var compressed = Convert.FromBase64String(code);
    var json = Decompress(compressed);
    _presetManager.ImportPreset(json);
}
```

## Best Practices

### Effect Order (Signal Chain)
1. **Noise Gate** - Remove silence/noise FIRST
2. **Pre-EQ** - Shape input before processing
3. **Pitch/Formant** - Voice transformation
4. **Distortion** - If needed
5. **Post-EQ** - Final tone shaping
6. **Reverb/Delay** - Spatial effects
7. **Compressor** - Control dynamics
8. **Limiter** - Final safety (ALWAYS LAST)

### CPU Management
- Disable unused effects in chain
- Use simpler effects when possible
- Profile on target device
- Keep chains under 10 effects

### Testing Presets
1. Test with different voices (male/female)
2. Test at different volumes
3. A/B compare with similar presets
4. Get feedback from others
5. Iterate and refine

## Advanced Features

### Preset Morphing
- Interpolate between two presets
- Smooth transitions
- Create dynamic effects

### Adaptive Presets
- Adjust based on input level
- Frequency-aware processing
- Smart ducking

### Preset Recommendations
- Suggest presets based on:
  - Voice type
  - Use case
  - Similar presets liked

## Integration with Settings

Add to SettingsPage:
```xml
<!-- Preset Management -->
<Border>
    <VerticalStackLayout>
        <Label Text="Audio Presets" />

        <!-- Current Preset -->
        <Grid>
            <Label Text="Current Preset:" />
            <Label x:Name="CurrentPresetLabel" Text="Podcast Pro" />
            <Button Text="Change" Clicked="OnChangePresetClicked" />
        </Grid>

        <!-- Quick Access -->
        <Label Text="Favorites" />
        <HorizontalStackLayout>
            <Button Text="Podcast" />
            <Button Text="Radio" />
            <Button Text="Robot" />
        </HorizontalStackLayout>

        <!-- Manage -->
        <Button Text="Manage Presets"
                Clicked="OnManagePresetsClicked" />
    </VerticalStackLayout>
</Border>
```

## Related Skills

- `/create-voice-effect` - Create individual effects
- `/equalizer` - Design custom EQ curves
- `/test-audio` - Test presets with sample audio

---

**Skill Version:** 1.0
**Last Updated:** 2026-02-21
**Status:** Active
