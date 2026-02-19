# E-z MicLink - Design Guide

## 🎨 Design Philosophy

E-z MicLink features a **modern glassmorphic UI** with:
- Deep blue/purple gradient backgrounds
- Neon glow effects (blue, pink, purple)
- Smooth animations and transitions
- Custom-designed dialogs (no standard alerts)
- Bluetooth connection visualization

---

## Color Palette

### Backgrounds
```
Dark Background:     #0F0F1E
Card Background:     #1A1A2E
Glass Background:    #1E1E38
Dialog Background:   #0F0F1E
Dialog Overlay:      #000000CC (80% opacity)
```

### Accent Colors
```
Primary Blue:   #4A90E2
Neon Blue:      #5B9CFF
Neon Pink:      #FF5B9C
Neon Purple:    #A855F7
Success Green:  #4CAF50
Warning Orange: #FF9800
Error Red:      #FF5252
```

### Text Colors
```
Primary:    #FFFFFF (White)
Secondary:  #8E8E93 (Light Gray)
Tertiary:   #666666 (Gray)
Disabled:   #CCCCCC
```

### Gradients

**Primary Button Gradient:**
```xaml
<LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
    <GradientStop Color="#4A90E2" Offset="0.0" />
    <GradientStop Color="#8B5CF6" Offset="1.0" />
</LinearGradientBrush>
```

**Background Gradient:**
```xaml
<LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
    <GradientStop Color="#0F0F1E" Offset="0.0" />
    <GradientStop Color="#1A1A2E" Offset="0.5" />
    <GradientStop Color="#0F0F1E" Offset="1.0" />
</LinearGradientBrush>
```

---

## Typography

### Font Sizes
```csharp
Title Large:    32
Title Medium:   24
Title Small:    20
Body Large:     18
Body Medium:    16
Body Small:     14
Caption Large:  13
Caption Small:  12
Tiny:           10
```

### Usage
- **Title Large (32)**: Main app title
- **Title Small (20)**: Dialog titles
- **Body Medium (16)**: Buttons, important text
- **Body Small (14)**: Body text, descriptions
- **Caption Small (12)**: Labels, hints

---

## Spacing

```csharp
XXSmall:  4px
XSmall:   8px
Small:    12px
Medium:   16px
Large:    20px
XLarge:   24px
XXLarge:  32px
```

### Usage Guidelines
- **Padding**: Use Medium (16-20px) for most containers
- **Spacing between items**: Small to Medium (12-16px)
- **Section spacing**: Large to XLarge (20-24px)
- **Page margins**: Large (20px)

---

## Corner Radius

```csharp
Small:   12px  (Small UI elements)
Medium:  15px  (Buttons, inputs)
Large:   20px  (Cards, dialogs)
XLarge:  25px  (Large buttons)
Round:   30px  (Circular icons)
```

---

## Shadows & Glows

### Standard Shadow
```xaml
<Shadow Brush="#000000"
        Offset="0,4"
        Radius="12"
        Opacity="0.3" />
```

### Neon Glow (Blue)
```xaml
<Shadow Brush="#5B9CFF"
        Offset="0,0"
        Radius="20"
        Opacity="0.8" />
```

### Neon Glow (Pink)
```xaml
<Shadow Brush="#FF5B9C"
        Offset="0,0"
        Radius="20"
        Opacity="0.6" />
```

### Usage
- **Cards**: Subtle shadow with blue glow
- **Buttons**: Strong glow matching button color
- **Active elements**: Pulsing glow effect
- **Dialogs**: Strong shadow + glow for emphasis

---

## Component Styles

### Glassmorphic Card

```xaml
<Frame BackgroundColor="#1E1E38"
       BorderColor="#4A90E2"
       CornerRadius="20"
       HasShadow="True"
       Padding="20">
    <Frame.Shadow>
        <Shadow Brush="#5B9CFF"
                Offset="0,4"
                Radius="20"
                Opacity="0.5" />
    </Frame.Shadow>
    <!-- Content here -->
</Frame>
```

### Gradient Button

```xaml
<Border StrokeThickness="0"
        BackgroundColor="Transparent"
        StrokeShape="RoundRectangle 15">
    <Border.Background>
        <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
            <GradientStop Color="#4A90E2" Offset="0.0" />
            <GradientStop Color="#8B5CF6" Offset="1.0" />
        </LinearGradientBrush>
    </Border.Background>
    <Border.Shadow>
        <Shadow Brush="#5B9CFF"
                Offset="0,4"
                Radius="20"
                Opacity="0.8" />
    </Border.Shadow>
    <Button Text="Click Me"
            BackgroundColor="Transparent"
            TextColor="White"
            FontSize="16"
            FontAttributes="Bold"
            HeightRequest="50" />
</Border>
```

### Bullet Point Item

```xaml
<Frame BackgroundColor="#1E1E38"
       BorderColor="#4A90E2"
       CornerRadius="10"
       Padding="12">
    <Grid ColumnDefinitions="Auto,*" ColumnSpacing="10">
        <Label Text="✓"
               TextColor="#4CAF50"
               FontSize="16"
               FontAttributes="Bold" />
        <Label Text="Your text here"
               TextColor="#CCCCCC"
               FontSize="14"
               Grid.Column="1" />
    </Grid>
</Frame>
```

---

## Custom Dialogs

### Never Use Standard Alerts!

❌ **Don't do this:**
```csharp
await DisplayAlert("Title", "Message", "OK");
```

✅ **Do this instead:**
```csharp
await DialogService.ShowSuccessAsync("Title", "Message");
```

### Available Dialog Methods

```csharp
// Info dialog
await DialogService.ShowInfoAsync(title, message, bulletPoints);

// Success dialog (green checkmark)
await DialogService.ShowSuccessAsync(title, message, bulletPoints);

// Error dialog (red X)
await DialogService.ShowErrorAsync(title, message, bulletPoints);

// Warning dialog (yellow warning)
await DialogService.ShowWarningAsync(title, message, bulletPoints);

// Confirmation dialog (returns bool)
bool result = await DialogService.ShowConfirmationAsync(
    title, message, confirmText, cancelText, bulletPoints);

// Connection dialog (with Bluetooth animation)
await DialogService.ShowConnectionDialogAsync(title, message, bulletPoints);

// Connection success (with Bluetooth animation)
await DialogService.ShowConnectedAsync(deviceName);

// Connection failed (with diagnostics option)
bool runDiagnostics = await DialogService.ShowConnectionFailedAsync(deviceName);

// Custom dialog (full control)
await DialogService.ShowCustomDialogAsync(
    title: "Custom",
    message: "Your message",
    icon: "🎉",
    primaryButtonText: "OK",
    secondaryButtonText: "Cancel",
    bulletPoints: new List<string> { "Point 1", "Point 2" },
    showBluetoothGraphic: false
);
```

---

## Animations

### Timing
```csharp
Fast:      150ms (Quick feedback)
Normal:    250ms (Standard transitions)
Slow:      350ms (Emphasis)
Very Slow: 500ms (Dramatic effect)
```

### Common Animations

**Pulse Effect:**
```csharp
await element.ScaleTo(1.08, 500, Easing.SinInOut);
await element.ScaleTo(1.0, 500, Easing.SinInOut);
```

**Fade In:**
```csharp
element.Opacity = 0;
await element.FadeTo(1, 250, Easing.CubicOut);
```

**Scale Up (Entry):**
```csharp
element.Scale = 0.8;
element.Opacity = 0;
await Task.WhenAll(
    element.FadeTo(1, 250, Easing.CubicOut),
    element.ScaleTo(1, 250, Easing.CubicOut)
);
```

**Scale Down (Exit):**
```csharp
await Task.WhenAll(
    element.FadeTo(0, 200, Easing.CubicIn),
    element.ScaleTo(0.9, 200, Easing.CubicIn)
);
```

---

## Icons

Use emojis for consistency:

```csharp
Bluetooth:          🔵
BluetoothConnected: 🔗
Scan:               🔍
Speaker:            🔊
Microphone:         🎤
Battery:            🔋
Signal:             📶
Warning:            ⚠️
Error:              ❌
Success:            ✓
Info:               ℹ️
Disconnect:         ⛓️‍💥
Home:               🏠
Settings:           ⚙️
Diagnostics:        🔧
Logs:               📋
```

---

## Bluetooth Connection Graphic

The animated Bluetooth connection graphic is a key design element.

### When to Use
- Connection success dialogs
- Connection failed dialogs
- Reconnecting dialogs
- Any Bluetooth-related modal

### How to Use

**In Custom Dialogs:**
```csharp
await DialogService.ShowCustomDialogAsync(
    title: "Connecting...",
    message: "Please wait",
    showBluetoothGraphic: true  // ← Enable animation
);
```

**Standalone Component:**
```xaml
<ui:BluetoothConnectionGraphic HeightRequest="200"
                              WidthRequest="200" />
```

---

## Design System Access

### In Code-Behind
```csharp
using BluetoothMicrophoneApp.UI;

// Colors
var blue = Color.FromArgb(DesignSystem.Colors.PrimaryBlue);
var bg = Color.FromArgb(DesignSystem.Colors.CardBackground);

// Typography
var titleSize = DesignSystem.Typography.TitleLarge;

// Spacing
var padding = DesignSystem.Spacing.Medium;

// Corner radius
var radius = DesignSystem.CornerRadius.Large;

// Icons
var icon = DesignSystem.Icons.Bluetooth;

// Animation
var duration = DesignSystem.Animation.Normal;
```

### In XAML
```xaml
<!-- Use direct hex values -->
<Frame BackgroundColor="#1E1E38"
       BorderColor="#4A90E2"
       CornerRadius="20"
       Padding="20">
```

---

## UI State Guidelines

### Connection States

**Scanning:**
- Button text: "🔍 Scanning..."
- Disable button
- Show loading indicator

**Device Selected:**
- Highlight card border: `#4CAF50` (Green)
- Show "Connect" button
- Enable tap feedback

**Connecting:**
- Button text: "Connecting..."
- Card border: `#FF9800` (Orange)
- Disable button

**Connected:**
- Card border: `#4CAF50` (Green)
- Show success dialog with animation
- Enable audio controls

**Connection Failed:**
- Card border: `#FF5252` (Red)
- Show error dialog with diagnostics option
- Reset to selected state

**Disconnected:**
- Reset all UI states
- Show device list
- Clear selection

---

## Best Practices

### ✅ DO
- Use custom dialogs for ALL user notifications
- Include bullet points for complex information
- Animate important state changes
- Use gradient buttons for primary actions
- Show Bluetooth graphic for connection dialogs
- Provide visual feedback for all interactions
- Use consistent spacing from DesignSystem
- Test animations on actual devices

### ❌ DON'T
- Use DisplayAlert() - EVER!
- Mix standard Material/Cupertino controls with custom design
- Forget to add shadows/glows to cards and buttons
- Use flat colors for primary buttons (always use gradients)
- Skimp on animations - they're key to the premium feel
- Use inconsistent corner radius values
- Forget to disable buttons during async operations

---

## Accessibility Considerations

While maintaining the beautiful design:
- Ensure text contrast meets WCAG standards
- Provide haptic feedback for important actions
- Use semantic labels for screen readers
- Ensure touch targets are at least 44x44 points
- Support dark mode (already built-in)
- Test with large text sizes

---

## Implementation Checklist

When adding a new screen or feature:

- [ ] Use gradient background
- [ ] All cards have glassmorphic styling (bg + border + glow)
- [ ] Buttons use gradient + glow effect
- [ ] No standard alerts (use DialogService)
- [ ] Animations on state changes
- [ ] Consistent spacing (use DesignSystem values)
- [ ] Icons use emoji (from DesignSystem.Icons)
- [ ] Text colors follow hierarchy (white > gray > dark gray)
- [ ] Touch feedback on interactive elements
- [ ] Loading states handled gracefully
- [ ] Error states show helpful information

---

## Maintenance

### DialogService Initialization
Every page that uses dialogs must initialize DialogService in the constructor:

```csharp
public MyPage()
{
    InitializeComponent();
    DialogService.Initialize(RootGrid);  // RootGrid must exist in XAML
}
```

### Adding New Dialog Types
To add a new specialized dialog:

1. Create method in `UI/DialogService.cs`
2. Follow naming pattern: `Show[Type]Async()`
3. Use appropriate icon and colors
4. Include bullet points for complex info
5. Consider whether to show Bluetooth graphic

---

## Examples from the App

### MainPage Device Cards
```xaml
<Frame BackgroundColor="#2D2D44"
       BorderColor="#4A90E2"
       CornerRadius="15"
       Padding="15"
       Margin="0,5"
       HasShadow="True">
    <!-- Device info -->
</Frame>
```

### Audio Controls Card
```xaml
<Frame BackgroundColor="#2D2D44"
       CornerRadius="15"
       Padding="15"
       HasShadow="True">
    <!-- Microphone controls -->
</Frame>
```

### Connection Status Bar
```xaml
<Frame BackgroundColor="#2D2D44"
       BorderColor="#4CAF50"
       CornerRadius="15"
       Padding="12"
       HasShadow="True">
    <!-- Connected device info -->
</Frame>
```

---

**Version**: 1.0
**Last Updated**: February 19, 2026
**Maintained by**: Development Team

---

## Quick Reference

**Dialog Service Init:**
```csharp
DialogService.Initialize(RootGrid);
```

**Show Success:**
```csharp
await DialogService.ShowSuccessAsync("Title", "Message");
```

**Show Error:**
```csharp
await DialogService.ShowErrorAsync("Title", "Message");
```

**Show Confirmation:**
```csharp
bool result = await DialogService.ShowConfirmationAsync("Title", "Message");
```

**Show Connection:**
```csharp
await DialogService.ShowConnectedAsync(deviceName);
```

---

This design guide ensures consistency across the entire E-z MicLink application. Follow these guidelines for any new features or screens.
