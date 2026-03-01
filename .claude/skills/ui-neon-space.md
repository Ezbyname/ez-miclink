# UI Skill — Neon-Space Design System (E-z MicLink)

## 0) Purpose
This skill defines the **visual + layout system** for E-z MicLink so all screens, popups, alerts, and control panels stay consistent with the **Neon-Space** aesthetic.
Claude must follow this skill and must not fall back to generic Material/iOS defaults.

Target stack: **C# / .NET MAUI** (XAML + MVVM).
(If a platform-native UI is required later, keep the same component contracts and spacing rules.)

---

## 1) Core Visual Identity (Non-Negotiable)

### Background
- Default screen background: vertical cosmic gradient
  - Top: `#0F1320`
  - Bottom: `#1A1630`
- Optional subtle star/noise texture at **3–6% opacity** only (never heavy).
- Optional radial glow behind hero elements (icons/characters).

### Glass Surfaces
Used for cards, panels, sheets:
- Fill: `rgba(255,255,255,0.04)` to `rgba(255,255,255,0.07)`
- Border: 1px neon gradient border (see Neon Border)
- Corner radius: **20–24**
- Shadow: soft blur **16–24**, low opacity

### Neon Palette
- Electric Blue: `#00D2FF`
- Purple Accent: `#8B5CF6`
- Vivid Magenta: `#FF00FF`
- Solar Orange: `#FF8C00`
- Soft Error Accent: `#FB7185`

### Typography
- Font: **Inter** preferred (or Montserrat)
- Screen title: **24–28 SemiBold**, white
- Card title: **18 SemiBold**, white
- Secondary text: **13–14 Regular**, white @ 60% opacity

---

## 2) Spacing & Alignment Rules (8pt Grid)
Use ONLY: **8 / 16 / 24 / 32** for spacing.

### Standard Screen Padding
- Horizontal: **24**
- Top: **24–32** (account for status bar)
- Bottom: **16–24** (safe area)

### Alignment
- No "floating" icons via random margins.
- Avoid absolute positioning.
- Prefer Grid/Flex layouts with consistent columns.

---

## 3) Component Contracts (Exact UI Behavior)

### 3.1 Top Bar (Every Screen)
- Height: **64**
- Horizontal padding: **24**
- 3-zone layout:
  - **Left:** Back button (40×40 touch target)
  - **Center:** Title (or empty if title is below)
  - **Right:** Actions (e.g., Save / Save As)
- Back button style:
  - Circular glass background + subtle neon border on press
  - Icon centered (no offsets)

### 3.2 Neon Gradient Border
All primary containers:
- 1px gradient border, top-left → bottom-right
- Default gradient: Blue → Purple → Magenta
- Optional warm variant: Orange → Magenta → Purple
- Glow: same hue at 25–40% opacity, blur 12–20

### 3.3 Buttons
- Primary: gradient fill, height 52–56, radius 18–22, white text
- Secondary: glass fill + neon border
- Destructive (Sign out / Delete):
  - No solid red blocks
  - Outline border `rgba(251,113,133,0.45)` + text `#FB7185`
  - Pressed background `rgba(251,113,133,0.10)`

---

## 4) Voice Effects Screens — Required Layout & Flow

### 4.1 Sound Effects List Screen
- Scroll list/grid of **glass cards**
- Each card: icon + name + short description + chevron
- Tap card → opens Voice Detail screen
- Optional "Current Effect" glass panel at bottom

### 4.2 Voice Detail / Voice Lab Screen
Structure must follow:
1. Top bar (Back on left, Save/Save As on right)
2. Title row (Premium badge + effect name aligned to grid)
3. Center hero (character/icon centered with glow circle)
4. Pitch slider row (value left, label+icon right, slider full width)
5. Voice Controls region (4 vertical sliders)
6. Waveform strip region
7. Reset button (full width aligned to padding)

---

## 5) CRITICAL RULE — Voice Controls Must Be INSIDE Slider Holder Regions
This rule prevents the issue shown in broken layouts.

### What this means
Each of the 4 controls (Tone / Bright / Character / Space) must be rendered as a **single self-contained slider column block**.

**A slider column block MUST contain:**
- Value label (top)
- Vertical slider track + neon fill (center)
- Handle/knob (centered on the track)
- **Control label button INSIDE the slider holder area** (bottom)

✅ The label button must be **inside** the column container, aligned to the slider's centerline.
❌ The label button must NOT be outside, floating below, or misaligned using margins.

### Column layout contract
Each slider column is:
- Equal width (all 4 columns identical width)
- Content aligned center
- No absolute positioning
- No manual offsets to "fix" alignment

**Column internal structure:**
- Top: numeric value (e.g., -10 / +3)
- Middle: vertical slider track (fixed height)
- Bottom: label button (glass/outlined) that sits **within the column's bounds**

### 4 sliders must form a single grid row
- Parent container uses equal distribution across width (Grid with 4 equal columns)
- Consistent spacing between columns (16)
- All label buttons share the same baseline

---

## 6) Custom Neon Vertical Slider (Visual Contract)
Use a premium slider style (rectangular handle, neon fill):
- Track: dark glass track
- Fill: neon gradient fill from bottom to value
- Tick marks: subtle lines left/right
- Handle: rectangular "metal" block with a small neon strip inside
- Glow increases slightly while dragging

(If implementation is simplified, keep the same proportions and alignment.)

---

## 7) Popups / Alerts / Warnings (Neon-Space Only)
Never use default system dialogs for core UX.

### Modal Style
- Backdrop: **80% opaque dark** (`#CC000000`) for full visibility
- Container: **glass card with 20% opacity** (`#33FFFFFF`), radius 24, neon border, padding 20–24
- Header/Footer: darker glass (`#1A000000`) for better contrast
- Structure:
  1) Icon badge (neon cyan with glow)
  2) Title
  3) Message
  4) Actions: Primary + Secondary + optional Cancel

### Text Input Dialogs (Keyboard Handling)
For dialogs with text input fields (e.g., rename device, phone login, verification code):

**CRITICAL REQUIREMENTS:**
- **Wrap dialog in ScrollView** to allow automatic repositioning when keyboard appears
- **Add vertical margin**: 40px top/bottom to ensure clearance above keyboard
- **Input field styling**: darker background (`#33000000`) for better visibility
- **Auto-focus**: Focus input field after dialog animation completes (350ms delay)
- **Keyboard dismissal**: Unfocus input when dialog closes to dismiss keyboard

**Implementation Pattern:**
```xml
<Grid x:Name="OverlayGrid" BackgroundColor="#CC000000">
    <ScrollView VerticalOptions="Center">
        <Border x:Name="DialogBorder" Margin="24,40">
            <!-- Dialog content with text input -->
        </Border>
    </ScrollView>
</Grid>
```

**Why this matters:**
- Prevents keyboard from blocking text input fields
- Maintains visibility of all dialog content
- Provides smooth user experience on mobile devices
- Follows platform conventions while keeping Neon-Space design

---

## 8) Light Mode Strategy (Still Neon-Space)
Light mode must remain premium:
- Background: `#F0F2F8` with subtle "sky" tint
- Glass cards: white glass with inner shadow
- Neon borders become pastel neon (lower saturation), still gradient
- Maintain contrast and readability

---

## 9) Implementation Guidance (MAUI)
- Prefer reusable components:
  - `NeonCard`
  - `NeonButton`
  - `NeonBottomSheet`
  - `NeonVerticalSlider`
- Use MVVM (CommunityToolkit.Mvvm recommended)
- Avoid absolute layout; use Grid and consistent paddings
- Keep 8pt spacing; no random values

---

## 10) How to Use This Skill (Prompt Template)
When requesting UI changes, always start with:
"Read `.claude/skills/ui-neon-space.md` and follow it strictly."

Examples:
- "Build the Voice Detail screen. Ensure the 4 Voice Controls labels are INSIDE the slider holder regions, aligned per the skill."
- "Replace system alert with Neon-Space bottom sheet and matching actions."
- "Audit this screen: list mismatches vs this skill and fix them."

---
