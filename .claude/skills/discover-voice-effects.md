# Discover Voice Effects Skill

## Description
This skill uses an AI agent to explore websites, research trending voice effects, and suggest new fun voices to implement in the E-z MicLink app.

## Usage
```
/discover-voice-effects
/discover-voice-effects --focus gaming
/discover-voice-effects --focus streaming
/discover-voice-effects --category character-voices
```

## What This Skill Does

When invoked, this skill launches a specialized research agent that:

1. **Explores websites** - Searches popular audio/voice platforms
2. **Identifies trends** - Finds what's popular in voice effects
3. **Analyzes feasibility** - Determines if effects can be implemented
4. **Generates DSP specs** - Provides technical implementation details
5. **Prioritizes suggestions** - Ranks by popularity and difficulty
6. **Creates report** - Detailed markdown with implementation guides

## Research Sources

### Primary Sources

**Gaming & Streaming Platforms:**
- Discord voice changers and effects
- OBS Studio audio plugins
- Twitch streamer voice setups
- Gaming voice chat effects (Fortnite, Valorant, etc.)

**Social Media Trends:**
- TikTok audio effects and filters
- Instagram Reels voice effects
- YouTube Shorts trending sounds
- Twitter/X viral voice memes

**Voice Changer Apps:**
- Voicemod (most popular PC voice changer)
- Clownfish Voice Changer
- MorphVOX
- AV Voice Changer
- Mobile apps (Voice Changer Plus, etc.)

**Audio Software:**
- Plugin marketplaces (Plugin Boutique, Splice)
- DAW effect discussions (Ableton, FL Studio forums)
- Audio production subreddits (r/audioengineering, r/WeAreTheMusicMakers)

**Entertainment & Memes:**
- Movie/TV show voice effects (Star Wars, Marvel, etc.)
- Anime voice effects
- Cartoon character voices
- Celebrity voice impressions
- Meme sound effects

### Secondary Sources

**Technical Resources:**
- Audio DSP forums (KVR Audio, Gearslutz)
- Research papers on voice processing
- Open-source audio effect repositories (GitHub)
- Audio engineering blogs

**Market Research:**
- App store trending audio apps
- Google Trends for "voice changer" searches
- YouTube view counts for voice effect tutorials
- Reddit upvotes on voice effect posts

## Research Process

### Step 1: Web Search
```
Agent searches for:
- "trending voice effects 2026"
- "popular voice changer effects"
- "TikTok voice filters"
- "gaming voice effects"
- "streamer audio setups"
- "voice changer app features"
```

### Step 2: Website Exploration
```
Agent visits and analyzes:
- Voice changer product pages
- YouTube tutorials
- Reddit discussions
- Audio plugin demos
- App store reviews
```

### Step 3: Trend Analysis
```
Agent identifies:
- Most mentioned effects
- Highest engagement content
- Recurring requests
- Viral trends
- User complaints (missing features)
```

### Step 4: Feasibility Assessment
```
For each effect, agent evaluates:
- Technical complexity (can we implement?)
- CPU requirements (mobile-friendly?)
- DSP algorithms needed (do we have them?)
- Implementation time (quick win or major project?)
- User demand (how popular?)
```

### Step 5: DSP Specification
```
Agent generates:
- Effect description
- Required DSP components
- Parameter ranges
- Algorithm suggestions
- Code structure outline
```

### Step 6: Report Generation
```
Agent creates markdown report:
- Trending effects list (prioritized)
- Implementation specifications
- Difficulty ratings
- Popularity scores
- Code examples
```

## Example Output Report

The agent generates a report like this:

```markdown
# Voice Effect Discovery Report
**Date:** 2026-02-21
**Research Duration:** 15 minutes
**Sources Analyzed:** 27 websites
**Effects Discovered:** 15

---

## 🔥 Top 5 Trending Effects

### 1. ⭐ Anime Voice Filter (HIGH PRIORITY)
**Popularity:** 🔥🔥🔥🔥🔥 (Very High)
**Difficulty:** 🟡 Medium
**Implementation Time:** 2-3 hours

**What it is:**
Makes voice sound like an anime character - high-pitched, bright, and energetic.

**Where it's trending:**
- TikTok: 2.4M posts with #animefilter
- YouTube: 500+ tutorials with combined 10M+ views
- Discord: Frequently requested in voice changer servers

**DSP Implementation:**
```csharp
Pitch Shift: +4 to +6 semitones
Formant Shift: +12% to +18%
EQ:
  - Boost 2kHz-5kHz (+4dB) - brightness
  - Boost 8kHz-12kHz (+3dB) - air
  - Cut 200Hz-400Hz (-3dB) - less body
Compression: 3:1 ratio, -15dB threshold
Harmonic Exciter: Add subtle 2nd harmonic (+10%)
```

**Code Structure:**
- Class: AnimeVoiceEffect
- Components: PitchShifter, FormantShifter, EQ (3 bands), Compressor, Exciter

**User Appeal:**
- Gaming: Role-play as anime characters
- Streaming: Entertainment value
- Casual: Fun for social media content

---

### 2. ⭐ Demon/Devil Voice (HIGH PRIORITY)
**Popularity:** 🔥🔥🔥🔥 (High)
**Difficulty:** 🟢 Easy
**Implementation Time:** 1-2 hours

**What it is:**
Deep, menacing voice with growl - popular for gaming and Halloween.

**Where it's trending:**
- Gaming: Popular in horror games, Among Us
- Halloween: Seasonal spike every October
- YouTube: "demon voice changer" = 5M+ views

**DSP Implementation:**
```csharp
Pitch Shift: -6 to -8 semitones
Formant Shift: -10% to -15%
Ring Modulation: 18-25Hz (growl)
Distortion: Soft clip 0.3-0.4
EQ:
  - Boost 80Hz-150Hz (+6dB) - deep rumble
  - Cut 3kHz-5kHz (-4dB) - remove clarity
Reverb: Large room, 50% wet
```

**Code Structure:**
- Class: DemonVoiceEffect
- Components: PitchShifter, FormantShifter, RingMod, Distortion, EQ, Reverb

**User Appeal:**
- Gaming: Horror games, villains
- Entertainment: Pranks, Halloween
- Content: YouTube videos, TikTok

---

### 3. ⭐ Auto-Tune/T-Pain Effect (MEDIUM PRIORITY)
**Popularity:** 🔥🔥🔥🔥 (High)
**Difficulty:** 🔴 Hard
**Implementation Time:** 4-6 hours

**What it is:**
Robotic pitch correction - signature T-Pain/modern rap sound.

**Where it's trending:**
- Music: Extremely popular in rap/hip-hop
- TikTok: Used in countless music covers
- Karaoke: Requested feature

**DSP Implementation:**
```csharp
Pitch Detection: YIN or autocorrelation algorithm
Pitch Correction: Snap to nearest note in scale
  - Attack: 0-50ms (0ms = hard auto-tune)
  - Scale: Chromatic or custom
Formant Preservation: Yes (critical!)
Vibrato: Optional (add natural feel)
```

**Code Structure:**
- Class: AutoTuneEffect
- Components: PitchDetector, PitchCorrector, FormantPreserver
- Complexity: Requires real-time pitch detection

**Implementation Notes:**
- Most complex effect in this list
- May need external library (e.g., Essentia)
- Consider using simpler "hard pitch snap" first
- Full auto-tune is studio-grade feature

**User Appeal:**
- Music: Rap/singing enhancement
- Karaoke: Sound like a pro
- Entertainment: Comedy value

---

### 4. ⭐ Ghost/Ethereal Voice (MEDIUM PRIORITY)
**Popularity:** 🔥🔥🔥 (Medium-High)
**Difficulty:** 🟢 Easy
**Implementation Time:** 1 hour

**What it is:**
Haunting, otherworldly voice with shimmer and echo.

**Where it's trending:**
- Gaming: Horror games, paranormal themes
- Halloween: Seasonal content
- ASMR: Ethereal storytelling

**DSP Implementation:**
```csharp
Pitch Shift: +2 to +4 semitones
Whisper: Add breath noise (filtered white noise)
Reverb: Cathedral, 70% wet, 8s decay
EQ:
  - High-pass: 400Hz (remove body)
  - Boost: 6kHz-10kHz (+5dB) - shimmer
Delay: 200ms, 30% feedback
Modulation: Chorus, 0.3Hz rate
```

**Code Structure:**
- Class: GhostVoiceEffect
- Components: PitchShifter, Reverb, Chorus, Delay, EQ

**User Appeal:**
- Gaming: Horror atmosphere
- Entertainment: Spooky content
- Storytelling: Paranormal narratives

---

### 5. ⭐ Baby Voice (LOW-MEDIUM PRIORITY)
**Popularity:** 🔥🔥🔥 (Medium)
**Difficulty:** 🟢 Easy
**Implementation Time:** 30 minutes

**What it is:**
Cute, high-pitched baby/toddler voice.

**Where it's trending:**
- Social Media: Comedy skits
- Pet Videos: Voice-over for animals
- Memes: Baby voice is evergreen meme

**DSP Implementation:**
```csharp
Pitch Shift: +8 to +10 semitones
Formant Shift: +15% to +25%
EQ:
  - Boost: 3kHz-8kHz (+4dB) - brightness
  - Cut: Below 150Hz (-6dB) - no bass
Lisping: Resonant filter @ 8kHz (optional)
```

**Code Structure:**
- Class: BabyVoiceEffect
- Components: PitchShifter, FormantShifter, EQ

**User Appeal:**
- Comedy: Funny content
- Pet Content: Animal voice-overs
- Casual: Social media fun

---

## 📊 Full Discovery List (15 Effects)

| Rank | Effect | Popularity | Difficulty | Priority |
|------|--------|-----------|-----------|----------|
| 1 | Anime Voice | ⭐⭐⭐⭐⭐ | Medium | HIGH |
| 2 | Demon/Devil | ⭐⭐⭐⭐ | Easy | HIGH |
| 3 | Auto-Tune | ⭐⭐⭐⭐ | Hard | MEDIUM |
| 4 | Ghost/Ethereal | ⭐⭐⭐ | Easy | MEDIUM |
| 5 | Baby Voice | ⭐⭐⭐ | Easy | LOW-MED |
| 6 | Old Radio | ⭐⭐⭐ | Easy | MEDIUM |
| 7 | Drunk Voice | ⭐⭐⭐ | Medium | MEDIUM |
| 8 | Vocoder (Daft Punk) | ⭐⭐⭐ | Hard | LOW |
| 9 | Cave Echo | ⭐⭐ | Easy | LOW |
| 10 | Walkie-Talkie | ⭐⭐ | Easy | LOW |
| 11 | Darth Vader | ⭐⭐⭐⭐ | Medium | MEDIUM |
| 12 | Minion Voice | ⭐⭐⭐ | Medium | LOW-MED |
| 13 | Zombie | ⭐⭐ | Easy | LOW |
| 14 | Squirrel/Chipmunk | ⭐⭐⭐⭐ | Easy | DONE ✓ |
| 15 | Stadium Announcer | ⭐⭐ | Easy | LOW |

---

## 💡 Quick Win Recommendations

These effects are popular AND easy to implement (do these first):

1. **Demon Voice** - High popularity, easy implementation
2. **Baby Voice** - Medium popularity, very easy
3. **Ghost Voice** - Medium popularity, easy
4. **Old Radio** - Medium popularity, easy

---

## 🎯 Market Gap Analysis

**Missing Features (High Demand):**
- Real-time Auto-Tune (users frequently request this)
- Celebrity voice clones (high demand, legally complex)
- Voice mixing (combine two effects)
- Custom effect chains (partially implemented)

**Underserved Markets:**
- ASMR creators (need subtle effects)
- Podcast hosts (need professional processing)
- Language learners (need accent modification)

---

## 🔍 Competitive Analysis

**Voicemod** (market leader):
- 90+ effects
- Professional quality
- PC only ($$$)
- Heavy CPU usage

**Your Advantage:**
- Mobile-first
- Real-time processing
- Free or freemium
- Lightweight

**Opportunity:**
Implement top 10 Voicemod effects on mobile = significant competitive advantage

---

## 📈 Trending Topics (Next 3 Months)

Based on Google Trends and social media:

1. **AI Voice Cloning** - Extremely hot topic (legally risky)
2. **Anime Filters** - Growing rapidly
3. **Horror/Scary Effects** - Seasonal (Halloween)
4. **Gaming Voice Chat** - Steady demand
5. **Music Production** - Auto-Tune interest rising

---

## 🛠️ Implementation Roadmap

### Week 1: Quick Wins
- Demon Voice (1-2 hours)
- Baby Voice (30 min)
- Ghost Voice (1 hour)

### Week 2: Medium Complexity
- Anime Voice (2-3 hours)
- Old Radio (1 hour)
- Drunk Voice (2 hours)

### Week 3: Advanced
- Auto-Tune (4-6 hours)
- Vocoder (4-6 hours)

### Month 2: Polish & Testing
- User feedback
- Performance optimization
- A/B testing

---

## 📚 Technical Resources

**Libraries to Consider:**
- **Rubber Band Library** - Excellent pitch/time stretching
- **SoundTouch** - Fast pitch shifting
- **Essentia** - Music analysis & auto-tune
- **aubio** - Pitch detection

**Research Papers:**
- "A Smarter Way to Find Pitch" (YIN algorithm)
- "Phase Vocoder: Theory and Applications"
- "Formant Preservation in Pitch Shifting"

**Open Source References:**
- Audacity effects (C++)
- Ardour plugins (C++)
- JUCE audio library (C++)
- Web Audio API examples (JavaScript)

---

## 🎤 User Quotes (from research)

> "I wish there was a good anime voice filter for mobile" - Reddit r/voicemod

> "All the voice changers sound robotic, I want natural pitch shift" - TikTok comment

> "Voicemod is too expensive for casual use" - Twitter

> "I just want a simple demon voice for gaming" - Discord

> "Auto-tune for karaoke would be amazing" - App Store review

---

## 🚀 Next Steps

1. **Review this report** and select effects to implement
2. **Use /create-voice-effect** skill to implement chosen effects
3. **Test with users** and gather feedback
4. **Run discovery again in 3 days** for new trends

---

**Report Generated:** 2026-02-21 16:30:00
**Next Discovery:** 2026-02-24 (3 days)
**Research Agent:** Claude Sonnet 4.5
**Confidence Level:** High (27 sources analyzed)
```

## Agent Configuration

### Research Agent Parameters

```javascript
{
  "agentType": "general-purpose",
  "tools": ["WebSearch", "WebFetch", "Grep", "Read"],
  "maxTurns": 50,
  "thoroughness": "very thorough",
  "focus": [
    "trending voice effects",
    "popular audio filters",
    "gaming voice changers",
    "streaming audio effects",
    "social media voice trends"
  ],
  "sources": [
    "tiktok trends",
    "youtube tutorials",
    "reddit discussions",
    "voicemod effects",
    "discord voice changers",
    "gaming audio setups"
  ]
}
```

### Search Queries

The agent will automatically search for:

```
Trending:
- "trending voice effects 2026"
- "popular voice filters TikTok"
- "most used voice changer effects"
- "viral audio effects"

Gaming:
- "best gaming voice effects"
- "streamer voice setup"
- "discord voice changer popular"
- "fortnite voice effects"

Apps:
- "voicemod most popular effects"
- "voice changer app features"
- "clownfish voice changer effects"

Social:
- "tiktok audio filters trending"
- "instagram voice effects"
- "youtube voice effect tutorials"
```

## Scheduling (Automation)

### Option 1: Manual Invocation

Run manually every 3 days:
```
/discover-voice-effects
```

### Option 2: GitHub Actions (Recommended)

Create `.github/workflows/voice-discovery.yml`:

```yaml
name: Voice Effect Discovery

on:
  schedule:
    - cron: '0 0 */3 * *'  # Every 3 days at midnight
  workflow_dispatch:  # Manual trigger

jobs:
  discover:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Run Discovery Agent
        uses: anthropics/claude-code-action@v1
        with:
          command: '/discover-voice-effects'
          project-path: '.'

      - name: Create Pull Request
        uses: peter-evans/create-pull-request@v5
        with:
          title: 'New Voice Effect Suggestions'
          body: 'Auto-generated voice effect discovery report'
          branch: 'voice-discovery-${{ github.run_number }}'
```

### Option 3: Local Cron Job (Linux/Mac)

Add to crontab:
```bash
0 0 */3 * * cd /path/to/project && claude-code run /discover-voice-effects
```

### Option 4: Windows Task Scheduler

Create scheduled task:
```powershell
$action = New-ScheduledTaskAction -Execute 'claude-code' -Argument 'run /discover-voice-effects'
$trigger = New-ScheduledTaskTrigger -Daily -DaysInterval 3
Register-ScheduledTask -TaskName "VoiceDiscovery" -Action $action -Trigger $trigger
```

### Option 5: Cloud Function (Azure/AWS)

Deploy serverless function that runs every 3 days:

```csharp
[FunctionName("VoiceDiscovery")]
public static async Task Run(
    [TimerTrigger("0 0 */3 * * *")] TimerInfo timer,
    ILogger log)
{
    // Call Claude API with discovery prompt
    var client = new AnthropicClient(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
    // ... discovery logic
}
```

## Output Formats

### Markdown Report (Default)
- Complete analysis with all details
- Saved to `Audio/VOICE_DISCOVERY_[DATE].md`

### JSON Export
```json
{
  "date": "2026-02-21",
  "effects": [
    {
      "name": "Anime Voice",
      "popularity": 5,
      "difficulty": 2,
      "priority": "HIGH",
      "dsp": {
        "pitchShift": "+4 to +6",
        "formantShift": "+12% to +18%",
        "eq": [...]
      },
      "sources": [...]
    }
  ]
}
```

### Email Digest
- HTML formatted
- Top 5 suggestions
- Quick implementation links

## Categories for Focused Research

### Focus Options

```
/discover-voice-effects --focus gaming
/discover-voice-effects --focus streaming
/discover-voice-effects --focus social-media
/discover-voice-effects --focus music
/discover-voice-effects --focus character-voices
/discover-voice-effects --focus scary
/discover-voice-effects --focus funny
/discover-voice-effects --focus professional
```

### Gaming Focus
Research: Gaming voice changers, esports, Discord effects, gaming memes

### Streaming Focus
Research: Twitch/YouTube setups, OBS plugins, streamer tools

### Social Media Focus
Research: TikTok filters, Instagram effects, viral trends

### Music Focus
Research: Auto-tune, pitch correction, music production effects

## Implementation Workflow

After receiving discovery report:

1. **Review report** - Read top suggestions
2. **Select effects** - Choose 2-3 to implement
3. **Use creation skill** - `/create-voice-effect [name]`
4. **Test with users** - Get feedback
5. **Iterate** - Refine based on feedback
6. **Run discovery again** - Stay current with trends

## Metrics Tracked

The agent tracks:

- **Popularity score** (1-5 stars)
- **Difficulty rating** (Easy/Medium/Hard)
- **Implementation time** (hours)
- **Source count** (number of mentions)
- **Trend velocity** (growing/stable/declining)
- **User requests** (explicit asks)
- **Competitive gap** (what competitors don't have)

## Advanced Features

### Sentiment Analysis
Analyze user comments to understand:
- What users like/dislike
- Common complaints
- Feature requests
- Quality expectations

### Competitor Tracking
Monitor competitors for:
- New effect releases
- Feature updates
- Pricing changes
- User migration

### Predictive Trends
Use historical data to:
- Predict upcoming trends
- Identify seasonal patterns
- Forecast demand
- Plan roadmap

## Related Skills

- `/create-voice-effect` - Implement discovered effects
- `/equalizer` - Create EQ presets for effects
- `/audio-preset` - Build complete presets
- `/test-audio` - Test new effects

---

**Skill Version:** 1.0
**Last Updated:** 2026-02-21
**Status:** Active
**Requires:** Internet connection, WebSearch access
