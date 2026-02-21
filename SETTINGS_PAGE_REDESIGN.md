# Settings Page - Premium Design Specification

**Version:** 2.0
**Date:** 2026-02-21
**Status:** ✅ Implemented

---

## 📐 Design Overview

The Settings page has been completely redesigned with a **premium dark glass-morphism UI** featuring:

- Deep navy to dark indigo gradient background
- Glass-style cards with subtle neon blue glow accents
- Clean vertical layout with clear section hierarchy
- Smooth animations and haptic feedback
- Comprehensive audio, device, and app preferences
- Professional user experience with balanced spacing

---

## 🎨 Visual Design

### Color Palette

| Element | Color | Hex Code |
|---------|-------|----------|
| Background Gradient Start | Deep Navy | `#0A0E27` |
| Background Gradient Mid | Dark Indigo | `#1A1A3E` |
| Background Gradient End | Indigo | `#1E1E4A` |
| Primary Accent (Neon Blue) | Electric Blue | `#4A90E2` |
| Card Background | Semi-transparent Dark | `#1E1E3888` |
| Card Border | Glowing Blue | `#4A90E233` |
| Text Primary | White | `#FFFFFF` |
| Text Secondary | Light Gray | `#9999AA` |
| Danger Accent | Soft Red | `#FF5252` |
| Divider | Dark Gray | `#333355` |

### Glass-Morphism Effects

**Card Style:**
```xml
<Border StrokeThickness="1"
        Stroke="#4A90E233"
        BackgroundColor="#1E1E3888">
    <Border.Shadow>
        <Shadow Brush="#4A90E222"
                Radius="16"
                Opacity="0.4"
                Offset="0,2" />
    </Border.Shadow>
</Border>
```

**Features:**
- Semi-transparent backgrounds (`88` alpha)
- Subtle glow borders (`33` alpha)
- Soft drop shadows with blur
- Rounded corners (16px radius)
- Elevated appearance

---

## 🧭 Page Structure

### Header Area

**Components:**
1. **Back Arrow** (left) - Returns to main page
2. **Title** - "Settings" (large, bold, centered)
3. **Info Icon** (right) - Optional, currently hidden
4. **Subtitle** - "Manage audio, devices, and preferences"

**Styling:**
- Header padding: 20px horizontal, 40px top
- Title font size: 28pt
- Subtitle font size: 14pt (gray)
- Clear visual separation from content

---

### User Status Card

**Location:** Top of content, directly below header

**Design:**
- Strongest glow of all cards (more prominent)
- Circular avatar with neon blue border
- Two-line text layout
- "Manage" chevron on right

**Content States:**

**1. Logged In (Regular User):**
```
👤  John Doe                    ›
    john@example.com
```

**2. Guest User:**
```
👤  Guest                       ›
    Guest user
```

**3. Not Signed In:**
```
👤  Guest                       ›
    Not signed in
```

**Behavior:**
- Tappable entire card
- Opens account management dialog
- Prompts guest users to sign in
- Smooth press animation (scale 0.98)
- Glow intensifies on press

---

### Section 1: Audio & Microphone 🎤

**6 Settings:**

#### 1. Mic Gain 🎚️
- **Type:** Slider
- **Range:** 0% - 200%
- **Default:** 100%
- **Display:** Real-time percentage label
- **Key:** `mic_gain`
- **Purpose:** Adjust microphone input volume

#### 2. Noise Reduction 🔇
- **Type:** Toggle Switch
- **Default:** ON
- **Key:** `noise_reduction`
- **Purpose:** Reduce background noise

#### 3. Echo / Reverb 🔊
- **Type:** Toggle Switch
- **Default:** OFF
- **Key:** `echo_enabled`
- **Purpose:** Add echo/reverb effect

#### 4. Voice Preset 🎭
- **Type:** Navigation Row
- **Options:** Normal, Deep, High, Robot, Echo, Chipmunk, Underwater
- **Default:** "Normal"
- **Key:** `voice_preset`
- **Purpose:** Apply voice effect preset

#### 5. Limiter Safety 🛡️
- **Type:** Toggle Switch
- **Default:** ON
- **Key:** `limiter_enabled`
- **Purpose:** Prevent audio clipping

#### 6. Latency Mode ⚡
- **Type:** Navigation Row
- **Options:** Low Latency, Balanced, Stable
- **Default:** "Balanced"
- **Key:** `latency_mode`
- **Purpose:** Adjust audio processing latency

**Visual:**
- Section header with icon and label
- Each setting row with emoji icon (left)
- Controls on right (toggle/slider/chevron)
- Dividers between rows
- Consistent padding (16px)

---

### Section 2: Bluetooth & Devices 🔵

**4 Settings:**

#### 1. Preferred Device 📱
- **Type:** Navigation Row
- **Default:** "None"
- **Key:** `preferred_device`
- **Purpose:** Auto-connect to favorite device
- **Future:** Opens device selector

#### 2. Auto Reconnect 🔄
- **Type:** Toggle Switch
- **Default:** ON
- **Key:** `auto_reconnect`
- **Purpose:** Automatically reconnect to last device

#### 3. Scan on Launch 🔍
- **Type:** Toggle Switch
- **Default:** OFF
- **Key:** `scan_on_launch`
- **Purpose:** Start Bluetooth scan when app opens

#### 4. Open Bluetooth Settings ⚙️
- **Type:** Navigation Row
- **Purpose:** Opens system Bluetooth settings
- **Platform:** Android only (direct intent)

---

### Section 3: App Preferences 🎨

**4 Settings:**

#### 1. Theme 🌙
- **Type:** Navigation Row
- **Options:** Dark, Light, Auto
- **Default:** "Dark"
- **Key:** `theme`
- **Purpose:** Change app theme
- **Status:** Coming soon (placeholder)

#### 2. Language 🌐
- **Type:** Navigation Row
- **Options:** English, Spanish, French, German, Hebrew
- **Default:** "English"
- **Key:** `language`
- **Purpose:** Change app language
- **Status:** Coming soon (placeholder)

#### 3. Haptics 📳
- **Type:** Toggle Switch
- **Default:** ON
- **Key:** `haptics_enabled`
- **Purpose:** Enable/disable haptic feedback
- **Behavior:** Triggers haptic when toggled

#### 4. Animations ✨
- **Type:** Toggle Switch
- **Default:** ON
- **Key:** `animations_enabled`
- **Purpose:** Enable/disable UI animations

---

### Section 4: Sign Out (Isolated) 🚪

**Location:** Bottom of page, visually separated

**Design:**
- Separate glass card
- Soft danger accent (not bright red)
- Outline border with subtle red glow
- Transparent background
- More vertical spacing above (16px margin)

**Behavior:**
- Hidden for guest users
- Shows confirmation dialog
- Signs out and returns to login page
- Smooth press animation

**Confirmation Dialog:**
```
Title: "Sign Out"
Message: "Are you sure you want to sign out?"
Buttons: "Sign Out" (destructive) | "Cancel"
```

---

## 🎯 User Experience Features

### Interaction Design

**1. Press Feedback:**
- Slight scale down (0.98) on tap
- Glow accent intensifies briefly
- Duration: 150ms
- Smooth easing

**2. Toggle Switches:**
- Instant state change
- Saves to preferences immediately
- ON color: Neon blue (`#4A90E2`)
- OFF color: Gray
- White thumb

**3. Sliders:**
- Real-time value updates
- Percentage label updates as you drag
- Smooth thumb movement
- Track colors: Blue (min), Gray (max)

**4. Navigation Rows:**
- Chevron icon (›) indicates more options
- Current value shown in gray
- Opens action sheet or new page
- Smooth transition

**5. Haptic Feedback:**
- Triggers on button press (if enabled)
- Light click sensation
- Enhances premium feel

### Scroll Behavior

- Smooth elastic scrolling
- Bounces at top/bottom (iOS style)
- Maintains scroll position on return
- No overscroll glow (Android)

### Animations

All animations (if enabled):
- Fade in on page load (200ms)
- Slide up cards (staggered)
- Press scale animations
- Toggle switch animations
- Smooth page transitions

---

## 💾 Settings Persistence

All settings are saved using **MAUI Preferences API**:

### Storage Format

```csharp
// Boolean settings
Preferences.Set("noise_reduction", true);
bool noiseReduction = Preferences.Get("noise_reduction", true);

// Integer settings
Preferences.Set("mic_gain", 100);
int micGain = Preferences.Get("mic_gain", 100);

// String settings
Preferences.Set("voice_preset", "Normal");
string preset = Preferences.Get("voice_preset", "Normal");
```

### Storage Keys

All keys are defined as constants in `SettingsPage.xaml.cs`:

```csharp
private const string MicGainKey = "mic_gain";
private const string NoiseReductionKey = "noise_reduction";
private const string EchoKey = "echo_enabled";
// ... etc
```

### Persistence Behavior

- ✅ Settings save **immediately** when changed
- ✅ Settings persist across app restarts
- ✅ Settings survive app updates
- ✅ Settings stored securely on device
- ✅ No cloud sync (local only)

---

## 🔧 Implementation Details

### File Structure

**XAML:** `Pages/SettingsPage.xaml`
- Complete UI layout
- Glass card components
- Toggle switches, sliders, navigation rows
- Premium styling with gradients and shadows

**Code-Behind:** `Pages/SettingsPage.xaml.cs`
- Event handlers for all settings
- Preferences loading/saving
- Navigation logic
- User status updates
- Account management

### Dependencies

```csharp
using BluetoothMicrophoneApp.Models;
using BluetoothMicrophoneApp.Services;
using Microsoft.Maui.Storage;
```

### Key Methods

**Initialization:**
- `LoadAllSettings()` - Loads all preferences on page load
- `UpdateUserStatusCard()` - Updates user info display

**Event Handlers:**
- `OnMicGainChanged()` - Slider value change
- `OnNoiseReductionToggled()` - Toggle switch
- `OnVoicePresetClicked()` - Opens action sheet
- `OnLatencyModeClicked()` - Opens action sheet
- `OnThemeClicked()` - Opens action sheet
- `OnLanguageClicked()` - Opens action sheet
- `OnSignOutClicked()` - Shows confirmation, logs out

**Navigation:**
- `OnBackClicked()` - Returns to previous page
- `OnManageAccountClicked()` - Opens account management

---

## 📱 Platform-Specific Features

### Android

**Bluetooth Settings:**
```csharp
#if ANDROID
var intent = new Android.Content.Intent(
    Android.Provider.Settings.ActionBluetoothSettings);
intent.SetFlags(Android.Content.ActivityFlags.NewTask);
Android.App.Application.Context.StartActivity(intent);
#endif
```

**Haptic Feedback:**
```csharp
HapticFeedback.Default.Perform(HapticFeedbackType.Click);
```

### iOS

- Fallback message for Bluetooth settings (opens Settings app)
- Native haptic feedback
- Smooth scroll behavior

---

## 🚀 Future Enhancements

### Planned Features

**1. Saved Sound Presets**
- Create custom sound combinations
- Save effect + volume + preset
- Quick apply from settings
- Sync across devices (future)

**2. Theme Switching**
- Dark theme (current)
- Light theme
- Auto (system-based)
- Custom accent colors

**3. Localization**
- Hebrew support
- Spanish, French, German
- RTL layout for Hebrew
- Dynamic language switching

**4. Advanced Audio**
- Equalizer (5-band)
- Compressor settings
- Gate settings
- Custom effect chains

**5. Account Management**
- Edit profile
- Change password/phone
- Delete account
- Export settings

**6. Cloud Sync**
- Sync settings across devices
- Backup to cloud
- Restore from backup
- Share presets with others

---

## 🐛 Known Issues

None at this time.

---

## ✅ Testing Checklist

### Visual Testing

- [ ] Header displays correctly
- [ ] Back arrow returns to main page
- [ ] User status card shows correct info
- [ ] All sections render with glass effect
- [ ] Glow shadows visible
- [ ] Toggle switches styled correctly
- [ ] Sliders move smoothly
- [ ] Chevrons visible on navigation rows
- [ ] Sign out section isolated at bottom
- [ ] Scroll behavior smooth
- [ ] Premium gradient background visible

### Functional Testing

- [ ] Mic gain slider updates label
- [ ] Mic gain saves to preferences
- [ ] Toggle switches save state
- [ ] Voice preset opens action sheet
- [ ] Voice preset saves selection
- [ ] Latency mode opens action sheet
- [ ] Latency mode saves selection
- [ ] Preferred device click works
- [ ] Auto reconnect toggle works
- [ ] Scan on launch toggle works
- [ ] Open BT settings works (Android)
- [ ] Theme selection works
- [ ] Language selection works
- [ ] Haptics toggle triggers feedback
- [ ] Animations toggle works
- [ ] Sign out confirmation shows
- [ ] Sign out returns to login
- [ ] Settings persist across restarts
- [ ] Guest users don't see sign out
- [ ] Manage account opens dialog

### Interaction Testing

- [ ] Press animations work
- [ ] Glow intensifies on press
- [ ] Haptic feedback triggers
- [ ] Smooth transitions
- [ ] No lag on slider
- [ ] Toggle switches responsive
- [ ] Action sheets appear correctly
- [ ] Confirmation dialogs styled
- [ ] Back navigation works
- [ ] Page scroll smooth

---

## 📊 Settings Summary

| Category | Settings Count | Toggle | Slider | Navigation |
|----------|----------------|--------|--------|------------|
| Audio & Microphone | 6 | 3 | 1 | 2 |
| Bluetooth & Devices | 4 | 2 | 0 | 2 |
| App Preferences | 4 | 2 | 0 | 2 |
| **Total** | **14** | **7** | **1** | **6** |

**Plus:**
- 1 User Status Card (tappable)
- 1 Sign Out Section (conditional)

**Grand Total:** 16 interactive elements

---

## 🎓 Code Examples

### Adding a New Toggle Setting

```csharp
// In XAML:
<Grid Padding="16,14" ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
    <Grid.GestureRecognizers>
        <TapGestureRecognizer Tapped="OnMySettingToggled" />
    </Grid.GestureRecognizers>

    <Label Grid.Column="0" Text="🎵" FontSize="20" />
    <Label Grid.Column="1" Text="My Setting" TextColor="White" FontSize="16" />
    <Switch x:Name="MySettingSwitch" Grid.Column="2" IsToggled="False" />
</Grid>

// In C#:
private const string MySettingKey = "my_setting";

private void OnMySettingToggled(object? sender, EventArgs e)
{
    bool isEnabled = MySettingSwitch.IsToggled;
    Preferences.Set(MySettingKey, isEnabled);

    // Apply setting to your service
    _myService.SetMySetting(isEnabled);
}
```

### Adding a New Navigation Setting

```csharp
// In XAML:
<Grid Padding="16,14" ColumnDefinitions="Auto,*,Auto,Auto" ColumnSpacing="12">
    <Grid.GestureRecognizers>
        <TapGestureRecognizer Tapped="OnMySettingClicked" />
    </Grid.GestureRecognizers>

    <Label Grid.Column="0" Text="🎵" FontSize="20" />
    <Label Grid.Column="1" Text="My Setting" TextColor="White" FontSize="16" />
    <Label x:Name="MySettingLabel" Grid.Column="2" Text="Default" TextColor="#9999AA" />
    <Label Grid.Column="3" Text="›" FontSize="24" TextColor="#4A90E2" />
</Grid>

// In C#:
private const string MySettingKey = "my_setting";

private async void OnMySettingClicked(object? sender, EventArgs e)
{
    string[] options = { "Option 1", "Option 2", "Option 3" };
    string? selected = await DisplayActionSheet("Select Option", "Cancel", null, options);

    if (selected != null && selected != "Cancel")
    {
        MySettingLabel.Text = selected;
        Preferences.Set(MySettingKey, selected);

        // Apply setting
        _myService.SetMySetting(selected);
    }
}
```

---

## 📝 Summary

The Settings page has been completely redesigned to provide a **premium, professional user experience** with:

✅ **Premium Design** - Glass-morphism with neon glow accents
✅ **Comprehensive Settings** - 14 configurable options
✅ **User-Friendly** - Clear organization and visual hierarchy
✅ **Persistent** - All settings saved and restored
✅ **Interactive** - Smooth animations and haptic feedback
✅ **Scalable** - Easy to add new settings
✅ **Platform-Native** - Leverages Android/iOS features

The page successfully balances **aesthetic appeal** with **functional utility**, providing users with complete control over their audio experience while maintaining a cohesive, modern design language.

---

**Document Version:** 2.0
**Last Updated:** 2026-02-21
**Status:** ✅ Complete
**Build:** ✅ Passing (0 errors)
