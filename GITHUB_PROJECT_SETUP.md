# GitHub Project Setup - E-z MicLink

**Project Board:** https://github.com/users/Ezbyname/projects/1/views/1

This guide will help you:
1. Create GitHub repository
2. Push code to GitHub
3. Create issues for voice discovery automation
4. Link issues to your project board

---

## Step 1: Create GitHub Repository

### Option A: Via GitHub Website (Easiest)

1. **Go to:** https://github.com/new

2. **Repository settings:**
   - Repository name: `BluetoothMicrophoneApp` or `ez-miclink`
   - Description: "E-z MicLink - Voice changer app with automated effect discovery"
   - Visibility: Choose **Public** or **Private**
   - ❌ **Do NOT** initialize with README (we have existing code)
   - ❌ **Do NOT** add .gitignore (we have existing code)
   - ❌ **Do NOT** add license yet

3. **Click:** "Create repository"

4. **Copy the repository URL:**
   - Should look like: `https://github.com/Ezbyname/BluetoothMicrophoneApp.git`

### Option B: Via GitHub CLI (If installed later)

```bash
gh repo create BluetoothMicrophoneApp --public --source=. --remote=origin --push
```

---

## Step 2: Connect Local Repository to GitHub

Open PowerShell in project directory and run:

```powershell
cd "C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp"

# Add all new files
git add .

# Commit everything
git commit -m "Initial commit: E-z MicLink voice changer with discovery automation

- Premium settings page with glass-morphism UI
- Voice discovery automation (/discover-voice-effects skill)
- Email delivery to yahalom.assets@gmail.com every 3 days
- Professional voice effects (Helium, Deep, Robot, etc.)
- Authentication system (Phone, Google, Apple, Guest)
- 16 sanity tests + 33 integration tests
- 4 audio skills for creating effects, EQ, and presets
- Complete documentation and automation setup"

# Add GitHub remote (replace with YOUR repository URL)
git remote add origin https://github.com/Ezbyname/BluetoothMicrophoneApp.git

# Rename branch to main (if needed)
git branch -M main

# Push to GitHub
git push -u origin main
```

**Your code is now on GitHub!** ✅

---

## Step 3: Create Issues for Project Board

### Issue #1: Setup Voice Discovery Email Automation

**Title:** 🔧 Setup Voice Discovery Email Automation

**Description:**
```markdown
## Goal
Configure automated voice discovery to send reports to yahalom.assets@gmail.com every 3 days.

## Tasks
- [ ] Generate Gmail App Password
  - Enable 2-Step Verification
  - Create app password for "Voice Discovery Bot"
- [ ] Add GitHub Secrets
  - Add `SENDER_EMAIL` secret
  - Add `SENDER_APP_PASSWORD` secret
- [ ] Test workflow
  - Manually trigger workflow in Actions tab
  - Verify email received at yahalom.assets@gmail.com
- [ ] Confirm automation
  - Verify workflow runs every 3 days
  - Check email delivery success

## Documentation
- `QUICK_EMAIL_SETUP.md` - Step-by-step setup guide
- `EMAIL_DELIVERY_SETUP.md` - Technical documentation
- `.github/workflows/voice-discovery.yml` - Workflow file

## Success Criteria
✅ Email received at yahalom.assets@gmail.com
✅ Workflow runs automatically every 3 days
✅ Reports include trending voice effects with DSP specs

## Priority
🔴 High - Needed for continuous feature discovery

## Estimated Time
⏱️ 10 minutes
```

**Labels:** `automation`, `setup`, `high-priority`

---

### Issue #2: Test Voice Discovery Agent

**Title:** 🧪 Test Voice Discovery Agent First Run

**Description:**
```markdown
## Goal
Run the voice discovery agent for the first time and verify it discovers trending effects.

## Tasks
- [ ] Run discovery command
  - `/discover-voice-effects` in Claude Code
  - Or trigger GitHub Action manually
- [ ] Review generated report
  - Check `VOICE_DISCOVERY_[DATE].md` file
  - Verify 10-15 effects discovered
  - Confirm DSP specs are included
- [ ] Validate quality
  - Effects are actually trending (check TikTok, YouTube)
  - DSP implementation is feasible
  - Difficulty ratings make sense
- [ ] Prioritize effects
  - Select top 3 to implement
  - Create issues for implementation

## Documentation
- `.claude/skills/discover-voice-effects.md` - Skill documentation
- `SKILLS_CREATED.md` - Overview of all skills

## Success Criteria
✅ Discovery report generated
✅ 10+ effects discovered with specs
✅ Top 3 effects prioritized
✅ Implementation issues created

## Priority
🟡 Medium - Needed before implementing new effects

## Estimated Time
⏱️ 30 minutes (including agent run time)
```

**Labels:** `testing`, `research`

---

### Issue #3: Implement Anime Voice Effect

**Title:** 🎌 Implement Anime Voice Effect (High Priority Discovery)

**Description:**
```markdown
## Goal
Implement anime/chipmunk voice effect based on discovery research showing very high popularity on TikTok (2.4M posts with #animefilter).

## DSP Specifications
Based on discovery research:

**Parameters:**
- Pitch Shift: +5 semitones
- Formant Shift: +15%
- EQ Boost: 3-8kHz (+4dB for brightness)
- High Shelf: 8-12kHz (+3dB for air)
- Mid Cut: 200-400Hz (-3dB, less body)
- Compression: 3:1 ratio, -15dB threshold
- Harmonic Exciter: +10% 2nd harmonic (optional)

**Performance:**
- CPU: Medium (8-10%)
- Latency: ~100ms
- Implementation Time: 2-3 hours

## Implementation Steps
- [ ] Create `AnimeVoiceEffect.cs` in `Audio/DSP/`
  - Use existing `SimplePitchShifter` for pitch
  - Create formant shifter using biquad filters
  - Add EQ chain (3 bands)
  - Add compression
- [ ] Update `AudioEngine.cs`
  - Add `BuildAnimeVoicePreset()` method
  - Register preset with key "anime"
- [ ] Update `SettingsPage.xaml`
  - Add "Anime Voice" to voice preset picker
- [ ] Test
  - Test with real voice input
  - Verify pitch and formant shifting
  - Check CPU usage on device
  - Validate sound quality

## Use Cases
- Gaming: Role-play as anime characters
- Streaming: Entertainment value for streams
- Social Media: TikTok/Instagram content creation
- Casual: Fun for friends/family

## Success Criteria
✅ Effect sounds like anime character voice
✅ CPU usage < 10%
✅ No audio artifacts or glitches
✅ Users can select in settings

## Priority
🔴 High - Very popular effect with high user demand

## Skill to Use
`/create-voice-effect anime` - Will guide through implementation

## Related
- #2 (Test Voice Discovery) - This is a discovered effect
```

**Labels:** `enhancement`, `voice-effect`, `high-priority`, `discovered`

---

### Issue #4: Implement Demon Voice Effect

**Title:** 👹 Implement Demon/Devil Voice Effect (Quick Win)

**Description:**
```markdown
## Goal
Implement demon/devil voice effect - popular in gaming and easy to implement (1-2 hours).

## DSP Specifications
Based on discovery research:

**Parameters:**
- Pitch Shift: -6 semitones
- Formant Shift: -10%
- Ring Modulation: 20Hz (growl)
- Distortion: 0.3-0.4 soft clip
- Bass Boost: 80-150Hz (+6dB for rumble)
- Presence Cut: 3-5kHz (-4dB, remove clarity)
- Reverb: Large room, 50% wet

**Performance:**
- CPU: Low-Medium (6-8%)
- Latency: ~100ms
- Implementation Time: 1-2 hours ⚡ Quick Win!

## Implementation Steps
- [ ] Use `/create-voice-effect demon` skill
- [ ] Claude will generate `DemonVoiceEffect.cs`
- [ ] Add preset to AudioEngine
- [ ] Add to settings UI
- [ ] Test and refine

## Use Cases
- Gaming: Horror games, villains in Among Us
- Entertainment: Pranks, scary content
- Halloween: Seasonal content
- YouTube/TikTok: Horror videos

## Success Criteria
✅ Deep, menacing voice with growl
✅ Easy to implement (< 2 hours)
✅ Popular in gaming community

## Priority
🟡 Medium - Quick win, good ROI

## Estimated Time
⏱️ 1-2 hours

**Labels:** `enhancement`, `voice-effect`, `quick-win`, `discovered`

---

### Issue #5: Create Custom Equalizer UI

**Title:** 🎚️ Create Custom Equalizer UI in Settings

**Description:**
```markdown
## Goal
Implement visual parametric equalizer in Settings page for advanced audio control.

## Features
- [ ] Parametric EQ (5-band)
  - Frequency slider (20Hz - 20kHz, log scale)
  - Gain slider (-12dB to +12dB)
  - Q slider (0.3 to 10)
  - Filter type selector (Peak, Low Shelf, High Shelf)
- [ ] Visual frequency response
  - Display curve showing EQ shape
  - Real-time updates as sliders move
- [ ] Preset management
  - Load built-in presets (Vocal Clarity, Bass Boost, etc.)
  - Save custom presets
  - Export/import presets
- [ ] Real-time preview
  - Apply EQ changes immediately
  - A/B comparison (bypass button)

## Implementation
- [ ] Create `EqualizerEffect.cs` in `Audio/DSP/`
- [ ] Create `EqualizerManager.cs` for preset management
- [ ] Add EQ section to `SettingsPage.xaml`
- [ ] Implement visual frequency response using MAUI Graphics
- [ ] Add professional presets

## Use `/equalizer` Skill
This will guide through complete implementation!

## Success Criteria
✅ 5-band parametric EQ functional
✅ Visual frequency response displays
✅ Presets save/load correctly
✅ Real-time audio processing

## Priority
🟢 Low-Medium - Nice to have, enhances pro users

## Estimated Time
⏱️ 4-6 hours
```

**Labels:** `enhancement`, `ui`, `audio`

---

### Issue #6: Setup Continuous Voice Discovery

**Title:** 🤖 Setup Continuous Voice Discovery (Every 3 Days)

**Description:**
```markdown
## Goal
Ensure voice discovery runs automatically every 3 days and reports are delivered via email.

## Prerequisites
- #1 (Setup Email Automation) must be completed

## Verification Tasks
- [ ] Confirm workflow schedule
  - Check `.github/workflows/voice-discovery.yml`
  - Verify cron: `0 9 */3 * *` (every 3 days at 9 AM UTC)
- [ ] Monitor first automated run
  - Wait 3 days after manual test
  - Check Actions tab for automatic trigger
  - Verify email delivery
- [ ] Review discovery quality
  - Check if effects are actually trending
  - Verify DSP specs are accurate
  - Assess implementation feasibility
- [ ] Create implementation pipeline
  - When report received → Review effects
  - Select 1-2 to implement
  - Create GitHub issues
  - Implement using skills

## Documentation
- `AUTOMATION_SETUP.md` - Complete automation guide
- `.claude/skills/discover-voice-effects.md` - Discovery skill docs

## Success Criteria
✅ Workflow runs every 3 days automatically
✅ Email delivered reliably to yahalom.assets@gmail.com
✅ Reports contain actionable suggestions
✅ Implementation pipeline established

## Priority
🟡 Medium - Ensures continuous innovation

## Estimated Time
⏱️ Ongoing monitoring (5 min per report)
```

**Labels:** `automation`, `monitoring`, `ongoing`

---

## Step 4: Link Issues to Project Board

### Method 1: Via GitHub UI (Easiest)

1. **Open your project board:**
   - Go to: https://github.com/users/Ezbyname/projects/1

2. **For each issue created:**
   - Open the issue
   - On the right sidebar, find "Projects"
   - Click the ⚙️ icon
   - Select your project from dropdown
   - Issue is now linked!

3. **Organize in board:**
   - Drag issues to appropriate columns (To Do, In Progress, Done)
   - Set priorities
   - Add labels

### Method 2: Via Issue Creation

When creating issues on GitHub:
1. Write issue title and description
2. Before clicking "Submit new issue"
3. On right sidebar → Click "Projects"
4. Select "Project #1" from dropdown
5. Submit issue (automatically linked!)

### Method 3: Via GitHub CLI (If installed)

```bash
# Install GitHub CLI first
# Windows: winget install GitHub.cli
# Then login: gh auth login

# Create issue and add to project
gh issue create --title "Setup Voice Discovery" --body "..." --project "Project #1"
```

---

## Step 5: Create Project Columns (If Needed)

Your project board should have these columns:

1. **📋 Backlog** - Future features and ideas
2. **📅 To Do** - Ready to work on
3. **🚧 In Progress** - Currently working
4. **🧪 Testing** - Needs testing/review
5. **✅ Done** - Completed

To add columns:
1. Go to project board
2. Click "+" on the right
3. Name column
4. Drag to reorder

---

## Step 6: Suggested Issue Organization

### Sprint 1: Foundation (This Week)
- ✅ #1 - Setup Email Automation (HIGH)
- ✅ #2 - Test Voice Discovery (MEDIUM)
- 🚧 #3 - Implement Anime Voice (HIGH)

### Sprint 2: Quick Wins (Next Week)
- 📋 #4 - Implement Demon Voice (MEDIUM)
- 📋 #5 - Create Equalizer UI (LOW)

### Ongoing:
- 🔄 #6 - Monitor Continuous Discovery

---

## Quick Commands Reference

### Create Repository & Push

```bash
# Stage all files
git add .

# Commit
git commit -m "Initial commit: Voice discovery automation"

# Add remote
git remote add origin https://github.com/Ezbyname/BluetoothMicrophoneApp.git

# Push
git push -u origin main
```

### Create Issue via GitHub CLI

```bash
gh issue create \
  --title "🔧 Setup Voice Discovery Email" \
  --body "Configure automation..." \
  --label "automation,high-priority" \
  --project "Project #1"
```

### Link Existing Issue to Project

```bash
gh issue edit 1 --add-project "Project #1"
```

---

## Project Board Best Practices

### 1. Label Strategy

Use consistent labels:
- **Priority:** `high-priority`, `medium-priority`, `low-priority`
- **Type:** `bug`, `enhancement`, `documentation`
- **Category:** `voice-effect`, `ui`, `automation`, `testing`
- **Status:** `quick-win`, `discovered`, `blocked`

### 2. Issue Templates

Create issue templates for:
- New voice effect implementation
- Bug reports
- Feature requests
- Discovery findings

### 3. Milestones

Create milestones:
- **v1.0 - Core Features** (Current work)
- **v1.1 - Voice Discovery** (Automation setup)
- **v1.2 - Advanced Effects** (Discovered effects)
- **v2.0 - Premium Features** (Equalizer, presets)

### 4. Automation Rules

GitHub Projects supports automation:
- Auto-move to "In Progress" when assigned
- Auto-move to "Done" when closed
- Auto-add labels based on keywords

---

## Next Steps

1. **✅ Create GitHub repository** (Step 1)
2. **✅ Push code to GitHub** (Step 2)
3. **✅ Create 6 issues above** (Step 3)
4. **✅ Link issues to project board** (Step 4)
5. **🚀 Start with Issue #1** (Email automation setup)

---

## Issues Summary

| # | Title | Priority | Time | Status |
|---|-------|----------|------|--------|
| 1 | Setup Email Automation | 🔴 High | 10m | To Do |
| 2 | Test Voice Discovery | 🟡 Medium | 30m | To Do |
| 3 | Implement Anime Voice | 🔴 High | 2-3h | To Do |
| 4 | Implement Demon Voice | 🟡 Medium | 1-2h | To Do |
| 5 | Create Equalizer UI | 🟢 Low | 4-6h | Backlog |
| 6 | Continuous Discovery | 🟡 Medium | Ongoing | To Do |

**Total Estimated Work:** ~10-15 hours + ongoing monitoring

---

## Support

### GitHub Documentation
- Projects: https://docs.github.com/en/issues/planning-and-tracking-with-projects
- Issues: https://docs.github.com/en/issues/tracking-your-work-with-issues

### Need Help?
- GitHub CLI: https://cli.github.com/manual/
- Project Board Tutorial: https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects

---

**Setup Guide Version:** 1.0
**Last Updated:** 2026-02-21
**Status:** Ready to execute
**Next Action:** Create GitHub repository and push code
