# Voice Discovery Automation Setup

**Goal:** Automatically run voice effect discovery every 3 days to stay current with trends.

---

## Option 1: Manual Reminders (Simplest)

### Set Calendar Reminder

**Every 3 days:**
1. Open project in Claude Code
2. Run: `/discover-voice-effects`
3. Review report
4. Implement 1-2 new effects

**Pros:** Simple, no setup
**Cons:** Requires manual action

---

## Option 2: GitHub Actions (Recommended)

### Setup Instructions

**Step 1:** Create workflow file

Create `.github/workflows/voice-discovery.yml`:

```yaml
name: Voice Effect Discovery

on:
  # Run every 3 days at midnight UTC
  schedule:
    - cron: '0 0 */3 * *'

  # Allow manual trigger
  workflow_dispatch:

jobs:
  discover:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Run Voice Discovery
        env:
          ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
        run: |
          # Install Claude CLI (if needed)
          # npm install -g @anthropic-ai/claude-cli

          # Run discovery
          echo "Running voice effect discovery..."
          # claude run /discover-voice-effects

          # For now, create placeholder report
          echo "# Voice Discovery - $(date)" > VOICE_DISCOVERY_$(date +%Y%m%d).md
          echo "Discovery would run here with API access" >> VOICE_DISCOVERY_$(date +%Y%m%d).md

      - name: Create Pull Request
        uses: peter-evans/create-pull-request@v5
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          commit-message: 'docs: Add voice effect discovery report'
          title: '🎤 New Voice Effect Suggestions - $(date +%Y-%m-%d)'
          body: |
            ## Voice Effect Discovery Report

            Automated discovery has found new trending voice effects.

            ### What to do next:
            1. Review the report in `VOICE_DISCOVERY_[DATE].md`
            2. Select 1-2 effects to implement
            3. Use `/create-voice-effect [name]` to implement
            4. Test and merge

            ### Quick Stats:
            - Discovery date: $(date)
            - Sources analyzed: TBD
            - New effects found: TBD
            - Priority effects: TBD

          branch: voice-discovery-${{ github.run_number }}
          delete-branch: true

      - name: Notify (Optional)
        if: success()
        run: |
          echo "✓ Discovery complete! Check the Pull Request for new suggestions."
```

**Step 2:** Add API key to GitHub Secrets

1. Go to GitHub repo → Settings → Secrets and variables → Actions
2. Click "New repository secret"
3. Name: `ANTHROPIC_API_KEY`
4. Value: Your Anthropic API key
5. Click "Add secret"

**Step 3:** Enable workflows

1. Go to Actions tab
2. Enable workflows if disabled
3. Workflow will run automatically every 3 days

**Step 4:** Test manual trigger

1. Go to Actions → Voice Effect Discovery
2. Click "Run workflow"
3. Verify it creates a pull request

**Result:** Every 3 days, a PR is automatically created with new voice effect suggestions.

**Pros:**
- Fully automated
- Creates PR for review
- Version controlled
- Free for public repos

**Cons:**
- Requires GitHub
- Needs API key setup

---

## Option 3: Azure Function (Cloud)

### Setup Instructions

**Step 1:** Create Azure Function

```csharp
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace VoiceDiscovery
{
    public static class DiscoveryFunction
    {
        [FunctionName("VoiceDiscovery")]
        public static void Run(
            [TimerTrigger("0 0 */3 * * *")] TimerInfo timer,
            ILogger log)
        {
            log.LogInformation($"Voice Discovery triggered at: {DateTime.Now}");

            // Call Claude API
            var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            var client = new Anthropic.SDK.AnthropicClient(apiKey);

            var response = client.Messages.CreateAsync(new()
            {
                Model = "claude-sonnet-4.5-20250929",
                MaxTokens = 4096,
                Messages = new[]
                {
                    new Message
                    {
                        Role = "user",
                        Content = "Run the /discover-voice-effects skill and research trending voice effects..."
                    }
                }
            }).Result;

            // Save report to Azure Blob Storage or send email
            log.LogInformation("Discovery complete!");
        }
    }
}
```

**Step 2:** Deploy to Azure

```bash
# Install Azure Functions Core Tools
npm install -g azure-functions-core-tools@4

# Create function app
func init VoiceDiscoveryApp --dotnet

# Deploy
func azure functionapp publish YourFunctionAppName
```

**Step 3:** Configure timer

Timer expression: `0 0 */3 * * *`
- Runs every 3 days at midnight UTC

**Pros:**
- Cloud-hosted
- Reliable execution
- Can send email notifications
- Azure Free Tier available

**Cons:**
- Requires Azure account
- More complex setup

---

## Option 4: Local Scheduled Task

### Windows Task Scheduler

**Step 1:** Create PowerShell script

Create `RunDiscovery.ps1`:

```powershell
# Voice Discovery Automation Script
$projectPath = "C:\path\to\BluetoothMicrophoneApp"
$date = Get-Date -Format "yyyy-MM-dd"

Write-Host "Running Voice Effect Discovery - $date"

# Change to project directory
Set-Location $projectPath

# Run Claude Code with discovery command
# Assuming claude-code CLI is installed
# claude-code run "/discover-voice-effects"

# For manual version:
Write-Host "Please run: /discover-voice-effects in Claude Code"
Start-Process "code" -ArgumentList $projectPath

# Log execution
Add-Content -Path "discovery_log.txt" -Value "Discovery triggered: $date"
```

**Step 2:** Create Scheduled Task

```powershell
# Create scheduled task (run as Administrator)
$action = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-File `"C:\path\to\RunDiscovery.ps1`""

$trigger = New-ScheduledTaskTrigger -Daily -At 9:00AM -DaysInterval 3

$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

Register-ScheduledTask -TaskName "VoiceEffectDiscovery" `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Description "Runs voice effect discovery every 3 days"
```

**Pros:**
- Local execution
- No cloud dependencies
- Simple to debug

**Cons:**
- Computer must be on
- Manual if computer off

### macOS/Linux Cron

**Step 1:** Create shell script

Create `run_discovery.sh`:

```bash
#!/bin/bash
PROJECT_PATH="/path/to/BluetoothMicrophoneApp"
DATE=$(date +%Y-%m-%d)

echo "Running Voice Discovery - $DATE"

cd "$PROJECT_PATH"

# Run discovery
# claude-code run "/discover-voice-effects"

# Log
echo "$DATE - Discovery triggered" >> discovery_log.txt
```

Make executable:
```bash
chmod +x run_discovery.sh
```

**Step 2:** Add to crontab

```bash
# Edit crontab
crontab -e

# Add this line (runs every 3 days at 9 AM)
0 9 */3 * * /path/to/run_discovery.sh
```

**Pros:**
- Native to Unix systems
- Reliable
- Simple

**Cons:**
- Computer must be running
- No GUI feedback

---

## Option 5: Email Notification Service

### Setup with Zapier/Make/n8n

**Step 1:** Create workflow

1. **Trigger:** Schedule (every 3 days)
2. **Action:** Send email to yourself
3. **Email content:**
   ```
   Subject: 🎤 Time for Voice Discovery!

   Hey! It's been 3 days since the last voice effect discovery.

   Action needed:
   1. Open project in Claude Code
   2. Run: /discover-voice-effects
   3. Review new suggestions
   4. Implement 1-2 effects

   Last discovery: [date]
   Next discovery: [date + 3 days]
   ```

**Pros:**
- Super simple
- Just get reminded
- No code required

**Cons:**
- Still requires manual run
- Not truly automated

---

## Option 6: Discord/Slack Bot

### Create reminder bot

**Discord Bot Example:**

```javascript
const Discord = require('discord.js');
const client = new Discord.Client();

// Every 3 days at 9 AM
const schedule = require('node-schedule');

schedule.scheduleJob('0 9 */3 * *', function(){
  const channel = client.channels.cache.get('YOUR_CHANNEL_ID');
  channel.send(`
🎤 **Voice Effect Discovery Time!**

It's been 3 days - time to research new trending voice effects!

**Action Items:**
1. Run \`/discover-voice-effects\` in Claude Code
2. Review the report
3. Implement 1-2 new effects

**Quick Start:**
\`\`\`
cd BluetoothMicrophoneApp
claude-code
/discover-voice-effects
\`\`\`
  `);
});
```

**Pros:**
- Team collaboration
- Visible reminders
- Can discuss findings

**Cons:**
- Requires bot setup
- Still manual execution

---

## Recommended Setup

### For Solo Developer:
**Option 1** (Manual) + **Option 5** (Email reminder)
- Simple and reliable
- No infrastructure needed
- Just get reminded every 3 days

### For Team:
**Option 2** (GitHub Actions)
- Automated discovery
- Creates PR for team review
- Version controlled
- Professional workflow

### For Enterprise:
**Option 3** (Azure Function)
- Cloud-hosted
- Scalable
- Integration with existing infrastructure
- Email/Slack notifications

---

## Testing Your Setup

### Manual Test
```
/discover-voice-effects
```

Verify:
- ✓ Agent launches
- ✓ Searches websites
- ✓ Generates report
- ✓ Report saved to file

### Automation Test

**GitHub Actions:**
1. Go to Actions tab
2. Manually trigger workflow
3. Check for PR creation

**Azure Function:**
1. Run function locally
2. Check logs
3. Verify report generation

**Cron Job:**
```bash
# Test run
./run_discovery.sh

# Check log
tail -f discovery_log.txt
```

---

## Report Delivery Options

### 1. Local File
```
Audio/VOICE_DISCOVERY_2026-02-21.md
```

### 2. Email
Send HTML formatted report via:
- SendGrid
- AWS SES
- Mailgun
- SMTP

### 3. Slack/Discord
Post summary to team channel

### 4. GitHub Issue
Create GitHub issue with suggestions

### 5. Notion/Confluence
Auto-create page with report

---

## Monitoring

### Track Discovery Health

Create `discovery_health.json`:
```json
{
  "lastRun": "2026-02-21",
  "nextRun": "2026-02-24",
  "totalRuns": 15,
  "effectsDiscovered": 127,
  "effectsImplemented": 23,
  "successRate": 100
}
```

### Alerts

Set up alerts for:
- Discovery failures
- No new effects found (stale trends)
- API rate limits
- Scheduling failures

---

## Cost Considerations

### API Usage

Each discovery run:
- **Claude API calls:** ~10-20 messages
- **Cost per run:** ~$0.10-$0.30
- **Monthly cost:** ~$1-$3 (every 3 days = ~10 runs/month)

Very affordable for the value!

### Free Alternatives

- Manual runs (free)
- GitHub Actions (free for public repos)
- Azure Free Tier (free for low usage)

---

## Privacy & Security

### API Key Protection

**DO:**
- ✓ Use environment variables
- ✓ Store in GitHub Secrets
- ✓ Use Azure Key Vault
- ✓ Never commit to git

**DON'T:**
- ✗ Hardcode in scripts
- ✗ Commit to repository
- ✗ Share publicly

### Data Privacy

Discovery agent:
- ✓ Only searches public websites
- ✓ No user data collection
- ✓ No PII storage
- ✓ Trend analysis only

---

## Troubleshooting

### Discovery Agent Fails

**Check:**
1. API key valid
2. Internet connection
3. Rate limits
4. Website accessibility

### No New Effects Found

**Possible causes:**
- Trends are stable (normal)
- Search queries need updating
- Websites changed structure
- Seasonal lull

**Solution:**
- Expand search queries
- Add new sources
- Run with different focus

### Report Not Generated

**Check:**
1. File permissions
2. Disk space
3. Path correctness
4. Agent completion

---

## Best Practices

### 1. Regular Cadence
- Stick to 3-day schedule
- Consistency builds knowledge base
- Tracks trend evolution

### 2. Review & Act
- Don't just collect reports
- Implement at least 1 effect per run
- Iterate based on user feedback

### 3. Track Results
- Which effects are popular?
- Which took too long to implement?
- User satisfaction metrics

### 4. Adjust Focus
- Seasonal: Halloween (scary), holidays
- Events: Gaming releases, movie launches
- User requests: Top asked features

---

## Integration with Development Workflow

### Agile Sprint Integration

**Every Sprint:**
1. **Sprint Planning:** Review latest discovery report
2. **Backlog Grooming:** Prioritize new effects
3. **Sprint Goal:** Implement 2-3 effects
4. **Demo:** Show new effects to team

### Kanban Board

**Columns:**
- Discovered (from reports)
- Prioritized (team decision)
- In Progress (implementing)
- Testing (QA)
- Done (released)

---

## Success Metrics

Track:
- **Effects discovered:** Total count
- **Effects implemented:** Completion rate
- **Time to implement:** Speed metric
- **User engagement:** Usage stats
- **Feature requests filled:** User satisfaction

**Goal:** Implement 50% of high-priority discoveries within 2 weeks

---

## Future Enhancements

### Planned Features

1. **Smart Prioritization**
   - ML-based ranking
   - User preference learning
   - Market opportunity scoring

2. **Auto-Implementation**
   - Generate code automatically
   - Create PR with implementation
   - Require only review/approval

3. **A/B Testing Integration**
   - Deploy to subset of users
   - Measure engagement
   - Auto-promote winners

4. **Competitive Intelligence**
   - Track competitor releases
   - Feature gap analysis
   - Pricing intelligence

---

**Setup Guide Version:** 1.0
**Last Updated:** 2026-02-21
**Status:** Ready to deploy
**Recommended:** GitHub Actions for most users
