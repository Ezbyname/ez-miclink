# Email Delivery Setup for Voice Discovery Reports

**Recipient:** yahalom.assets@gmail.com

---

## Quick Setup Options

### Option 1: GitHub Actions with Email (Recommended)

**Step 1:** Update GitHub workflow to send email

Create/Update `.github/workflows/voice-discovery.yml`:

```yaml
name: Voice Effect Discovery with Email

on:
  schedule:
    - cron: '0 0 */3 * *'  # Every 3 days
  workflow_dispatch:

jobs:
  discover:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v3

      - name: Run Voice Discovery
        id: discovery
        run: |
          # Discovery logic here
          # Generate report: VOICE_DISCOVERY_$(date +%Y%m%d).md
          echo "REPORT_DATE=$(date +%Y%m%d)" >> $GITHUB_OUTPUT

      - name: Send Email with Report
        uses: dawidd6/action-send-mail@v3
        with:
          server_address: smtp.gmail.com
          server_port: 587
          username: ${{ secrets.SENDER_EMAIL }}
          password: ${{ secrets.SENDER_PASSWORD }}
          subject: '🎤 New Voice Effect Suggestions - ${{ steps.discovery.outputs.REPORT_DATE }}'
          to: yahalom.assets@gmail.com
          from: Voice Discovery Bot
          body: |
            Hi!

            The voice effect discovery agent has completed its research and found new trending effects!

            📊 Discovery Report Attached
            📅 Date: ${{ steps.discovery.outputs.REPORT_DATE }}

            Quick Action:
            1. Review the attached report
            2. Select 1-2 effects to implement
            3. Run: /create-voice-effect [effect-name]

            The report includes:
            - Top 10 trending effects
            - DSP implementation specs
            - Popularity rankings
            - Quick win recommendations

            Happy coding! 🎵
          attachments: VOICE_DISCOVERY_*.md
          content_type: text/plain
```

**Step 2:** Configure Gmail App Password

1. Go to Google Account: https://myaccount.google.com/
2. Security → 2-Step Verification (enable if not enabled)
3. App passwords → Generate new password
4. Copy the 16-character password

**Step 3:** Add Secrets to GitHub

1. Go to your GitHub repo
2. Settings → Secrets and variables → Actions
3. Add these secrets:
   - `SENDER_EMAIL`: Your Gmail address (e.g., your-email@gmail.com)
   - `SENDER_PASSWORD`: The app password from step 2

**Result:** Every 3 days, you'll receive an email at yahalom.assets@gmail.com with the discovery report!

---

## Option 2: SendGrid (Free Tier)

**Step 1:** Create SendGrid account

1. Sign up: https://sendgrid.com/ (Free: 100 emails/day)
2. Get API key from Settings → API Keys

**Step 2:** Create Python script

Create `send_discovery_email.py`:

```python
import os
from sendgrid import SendGridAPIClient
from sendgrid.helpers.mail import Mail, Attachment
from datetime import datetime
import base64

def send_discovery_report(report_path):
    # Read report
    with open(report_path, 'r') as f:
        report_content = f.read()

    # Create HTML version
    html_content = f"""
    <html>
    <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333;">
        <div style="max-width: 800px; margin: 0 auto; padding: 20px;">
            <h1 style="color: #4A90E2;">🎤 Voice Effect Discovery Report</h1>
            <p><strong>Date:</strong> {datetime.now().strftime('%Y-%m-%d')}</p>

            <div style="background: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;">
                <h2>Quick Action Items:</h2>
                <ol>
                    <li>Review the attached report</li>
                    <li>Select 1-2 effects to implement</li>
                    <li>Run: <code>/create-voice-effect [effect-name]</code></li>
                </ol>
            </div>

            <h2>Report Preview:</h2>
            <pre style="background: #f9f9f9; padding: 15px; overflow-x: auto;">
{report_content[:500]}...
            </pre>

            <p>Full report attached as markdown file.</p>

            <p style="margin-top: 30px; color: #666;">
                <em>This is an automated discovery report generated every 3 days.</em>
            </p>
        </div>
    </body>
    </html>
    """

    # Create attachment
    with open(report_path, 'rb') as f:
        data = f.read()
    encoded = base64.b64encode(data).decode()

    attachment = Attachment()
    attachment.file_content = encoded
    attachment.file_type = "text/markdown"
    attachment.file_name = os.path.basename(report_path)
    attachment.disposition = "attachment"

    # Send email
    message = Mail(
        from_email='discovery@yourapp.com',
        to_emails='yahalom.assets@gmail.com',
        subject=f'🎤 New Voice Effect Suggestions - {datetime.now().strftime("%Y-%m-%d")}',
        html_content=html_content
    )
    message.attachment = attachment

    try:
        sg = SendGridAPIClient(os.environ.get('SENDGRID_API_KEY'))
        response = sg.send(message)
        print(f"Email sent! Status: {response.status_code}")
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    # Find latest discovery report
    import glob
    reports = glob.glob("VOICE_DISCOVERY_*.md")
    if reports:
        latest = max(reports)
        send_discovery_report(latest)
```

**Step 3:** Install dependencies

```bash
pip install sendgrid
```

**Step 4:** Set environment variable

```bash
export SENDGRID_API_KEY='your-api-key-here'
```

**Step 5:** Add to automation

```yaml
# In GitHub Actions
- name: Send Email via SendGrid
  env:
    SENDGRID_API_KEY: ${{ secrets.SENDGRID_API_KEY }}
  run: python send_discovery_email.py
```

---

## Option 3: Gmail API (Most Reliable)

**Step 1:** Enable Gmail API

1. Go to: https://console.cloud.google.com/
2. Create new project: "VoiceDiscovery"
3. Enable Gmail API
4. Create OAuth 2.0 credentials
5. Download `credentials.json`

**Step 2:** Create C# email sender

Create `Services/EmailService.cs`:

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MimeKit;

namespace BluetoothMicrophoneApp.Services
{
    public class EmailService
    {
        private const string RecipientEmail = "yahalom.assets@gmail.com";

        public async Task SendDiscoveryReport(string reportPath)
        {
            // Authenticate
            UserCredential credential;
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { GmailService.Scope.GmailSend },
                    "user",
                    CancellationToken.None);
            }

            // Create Gmail service
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Voice Discovery Bot",
            });

            // Read report
            var reportContent = await File.ReadAllTextAsync(reportPath);
            var reportName = Path.GetFileName(reportPath);

            // Create email
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Voice Discovery Bot", "me"));
            message.To.Add(new MailboxAddress("", RecipientEmail));
            message.Subject = $"🎤 New Voice Effect Suggestions - {DateTime.Now:yyyy-MM-dd}";

            var builder = new BodyBuilder();
            builder.TextBody = $@"
Hi!

The voice effect discovery agent has completed its research!

📊 Discovery Report: {reportName}
📅 Date: {DateTime.Now:yyyy-MM-dd}

Quick Action:
1. Review the attached report
2. Select 1-2 effects to implement
3. Run: /create-voice-effect [effect-name]

See attachment for full details.

Happy coding! 🎵
            ";

            // Attach report
            builder.Attachments.Add(reportPath);
            message.Body = builder.ToMessageBody();

            // Send
            using (var stream = new MemoryStream())
            {
                await message.WriteToAsync(stream);
                var rawMessage = Convert.ToBase64String(stream.ToArray())
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

                var gmailMessage = new Message { Raw = rawMessage };
                await service.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
            }

            Console.WriteLine($"✓ Report sent to {RecipientEmail}");
        }
    }
}
```

**Step 3:** Install NuGet packages

```bash
dotnet add package Google.Apis.Gmail.v1
dotnet add package MimeKit
```

**Step 4:** Use in automation

```csharp
var emailService = new EmailService();
await emailService.SendDiscoveryReport("VOICE_DISCOVERY_20260221.md");
```

---

## Option 4: Azure Logic Apps (No Code)

**Step 1:** Create Logic App

1. Azure Portal → Create Logic App
2. Choose "Consumption" tier (pay-per-use)

**Step 2:** Configure trigger

- Trigger: Recurrence
- Interval: 3 days

**Step 3:** Add actions

1. **HTTP Request** - Call discovery API
2. **Compose** - Format email HTML
3. **Send Email (Gmail)** - Native Gmail connector
   - To: yahalom.assets@gmail.com
   - Subject: "🎤 Voice Discovery Report"
   - Body: @{outputs('Compose')}
   - Attachments: Discovery report

**Step 4:** Authorize Gmail

- Click "Sign in to Gmail"
- Authorize access

**Result:** Fully visual, no-code automation!

---

## Option 5: Zapier/Make (Easiest)

### Zapier Setup

**Step 1:** Create Zap

1. Trigger: **Schedule by Zapier**
   - Every 3 days

2. Action: **Gmail by Zapier** → Send Email
   - To: yahalom.assets@gmail.com
   - Subject: 🎤 Voice Discovery Report - {{zap_meta_human_now}}
   - Body:
     ```
     Hi!

     It's time to discover new voice effects!

     Action Items:
     1. Open project in Claude Code
     2. Run: /discover-voice-effects
     3. Review suggestions
     4. Implement 1-2 effects

     This reminder is sent every 3 days.
     ```

**Step 2:** Turn on Zap

**Result:** Automated email reminders (manual discovery run)

### Make.com Setup

Similar to Zapier but more powerful:

1. **Schedule** → Every 3 days
2. **HTTP** → Make request (if you have discovery API)
3. **Gmail** → Send email to yahalom.assets@gmail.com

---

## Option 6: Simple SMTP Script

**Create send_email.py:**

```python
import smtplib
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from email.mime.base import MIMEBase
from email import encoders
import os
from datetime import datetime

def send_report_email(report_path):
    # Email configuration
    sender_email = "your-gmail@gmail.com"
    sender_password = "your-app-password"  # Gmail app password
    recipient_email = "yahalom.assets@gmail.com"

    # Read report
    with open(report_path, 'r', encoding='utf-8') as f:
        report_preview = f.read()[:500]

    # Create message
    msg = MIMEMultipart()
    msg['From'] = "Voice Discovery Bot"
    msg['To'] = recipient_email
    msg['Subject'] = f'🎤 Voice Effect Discovery - {datetime.now().strftime("%Y-%m-%d")}'

    # Email body
    body = f"""
Hi!

The voice effect discovery agent has found new trending effects!

📊 Report Date: {datetime.now().strftime("%Y-%m-%d")}
📎 Report attached

Quick Preview:
{report_preview}...

Action Items:
1. Review the attached report
2. Select 1-2 effects to implement
3. Use /create-voice-effect to implement

See attachment for full details!

🎵 Voice Discovery Bot
    """

    msg.attach(MIMEText(body, 'plain'))

    # Attach report
    with open(report_path, 'rb') as attachment:
        part = MIMEBase('application', 'octet-stream')
        part.set_payload(attachment.read())
        encoders.encode_base64(part)
        part.add_header(
            'Content-Disposition',
            f'attachment; filename= {os.path.basename(report_path)}'
        )
        msg.attach(part)

    # Send email
    try:
        server = smtplib.SMTP('smtp.gmail.com', 587)
        server.starttls()
        server.login(sender_email, sender_password)
        server.send_message(msg)
        server.quit()
        print(f"✓ Email sent to {recipient_email}")
    except Exception as e:
        print(f"✗ Error sending email: {e}")

if __name__ == "__main__":
    import glob
    reports = glob.glob("VOICE_DISCOVERY_*.md")
    if reports:
        latest = max(reports)
        send_report_email(latest)
    else:
        print("No discovery reports found")
```

**Usage:**

```bash
# Install (if needed)
# No external packages needed - uses Python stdlib

# Configure
# Edit sender_email and sender_password in script

# Run
python send_email.py
```

**Integrate with cron:**

```bash
# Add to crontab
0 0 */3 * * cd /path/to/project && python send_email.py
```

---

## Recommended Setup for You

### Best Option: GitHub Actions + Email

**Why:**
- ✓ Fully automated (no computer needed)
- ✓ Runs in cloud (always on)
- ✓ Free for public repos
- ✓ Report delivered to your Gmail
- ✓ Can review via email on phone

**Setup time:** 10 minutes

**Steps:**
1. Create `.github/workflows/voice-discovery.yml` (shown above)
2. Get Gmail app password
3. Add secrets to GitHub
4. Done!

**Result:** Every 3 days, you'll receive:
```
To: yahalom.assets@gmail.com
Subject: 🎤 New Voice Effect Suggestions - 2026-02-24

Hi!

The voice effect discovery agent found 12 new trending effects!

Top 5:
1. ⭐⭐⭐⭐⭐ Anime Voice - High priority
2. ⭐⭐⭐⭐ Demon Voice - Easy to implement
3. ⭐⭐⭐⭐ Auto-Tune - High demand
4. ⭐⭐⭐ Ghost Voice - Quick win
5. ⭐⭐⭐ Baby Voice - Very easy

[Full report attached]
```

---

## Email Template Customization

### HTML Email Template

Create beautiful HTML emails:

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                  color: white; padding: 30px; text-align: center; border-radius: 10px; }
        .effect-card { background: #f9f9f9; padding: 15px; margin: 10px 0;
                       border-left: 4px solid #667eea; border-radius: 5px; }
        .priority-high { border-left-color: #ff6b6b; }
        .priority-medium { border-left-color: #ffd93d; }
        .stars { color: #ffd700; }
        .button { background: #667eea; color: white; padding: 12px 24px;
                  text-decoration: none; border-radius: 5px; display: inline-block; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🎤 Voice Effect Discovery</h1>
            <p>New trending effects discovered!</p>
            <p><strong>Date:</strong> {date}</p>
        </div>

        <h2>🔥 Top 5 Trending Effects</h2>

        <div class="effect-card priority-high">
            <h3>1. Anime Voice Filter</h3>
            <p class="stars">⭐⭐⭐⭐⭐</p>
            <p><strong>Popularity:</strong> Very High (TikTok trending)</p>
            <p><strong>Difficulty:</strong> Medium (2-3 hours)</p>
            <p><strong>Quick specs:</strong> Pitch +5, Formant +15%, EQ boost</p>
        </div>

        <!-- More effect cards... -->

        <div style="text-align: center; margin: 30px 0;">
            <a href="#" class="button">View Full Report</a>
        </div>

        <p style="color: #666; font-size: 12px;">
            This is an automated report sent every 3 days.
            Reply to this email if you have questions!
        </p>
    </div>
</body>
</html>
```

---

## Testing Email Delivery

### Test Immediately

```bash
# Create test report
echo "# Test Discovery Report" > TEST_REPORT.md

# Send test email
python send_email.py TEST_REPORT.md
```

Check yahalom.assets@gmail.com for test email!

---

## Email Frequency Options

You can change the frequency:

```yaml
# Daily
cron: '0 0 * * *'

# Every 2 days
cron: '0 0 */2 * *'

# Every 3 days (recommended)
cron: '0 0 */3 * *'

# Weekly
cron: '0 0 * * 0'  # Every Sunday

# Monthly
cron: '0 0 1 * *'  # 1st of every month
```

---

## Email Features

### Current Implementation

✓ Plain text body
✓ Markdown attachment
✓ Subject line with date
✓ Automated delivery

### Future Enhancements

- [ ] HTML formatted emails
- [ ] Inline effect previews
- [ ] Click-to-implement links
- [ ] Priority highlighting
- [ ] Mobile-optimized layout
- [ ] Reply-to feedback collection

---

## Troubleshooting

### Email not received

**Check:**
1. Spam/junk folder
2. Gmail app password is correct
3. Secrets configured in GitHub
4. Workflow ran successfully (Actions tab)

### Gmail blocks sign-in

**Solution:**
- Use App Password (not regular password)
- Enable 2-Step Verification first
- Generate app-specific password

### Attachment too large

If report > 25MB:
- Upload to Google Drive
- Include link in email instead

---

## Security Best Practices

**DO:**
- ✓ Use app passwords (not regular password)
- ✓ Store credentials in secrets/env variables
- ✓ Never commit credentials to git
- ✓ Use HTTPS/TLS for email

**DON'T:**
- ✗ Hardcode passwords in scripts
- ✗ Share app passwords
- ✗ Commit credentials.json
- ✗ Use personal email for automation (create dedicated account)

---

## Cost

**Free Options:**
- GitHub Actions email action: FREE
- Gmail SMTP: FREE (within limits)
- Zapier free tier: 100 tasks/month

**Paid Options:**
- SendGrid: $15/month (40k emails)
- Azure Logic Apps: ~$0.01 per run
- Make.com: $9/month (10k operations)

**Recommendation:** Start with GitHub Actions (100% free!)

---

**Setup Complete!**

Once configured, you'll receive voice discovery reports at:
**yahalom.assets@gmail.com**

Every 3 days with:
- 📊 Full discovery report
- 🎯 Top trending effects
- 💡 Implementation suggestions
- 📈 Priority rankings

---

**Configuration Version:** 1.0
**Last Updated:** 2026-02-21
**Recipient:** yahalom.assets@gmail.com
**Status:** Ready to configure
