# Quick Email Setup - Voice Discovery Reports

**Your Email:** yahalom.assets@gmail.com

Get voice effect discovery reports automatically every 3 days! 📧

---

## ✅ What's Already Done

I've created:
- ✓ GitHub Actions workflow (`.github/workflows/voice-discovery.yml`)
- ✓ Email delivery configuration
- ✓ Scheduled to run every 3 days
- ✓ Sends to: yahalom.assets@gmail.com

---

## 🚀 Setup (5 Minutes)

### Step 1: Get Gmail App Password

1. Go to your Google Account: https://myaccount.google.com/security
2. Click **"2-Step Verification"** (enable if not already enabled)
3. Scroll down and click **"App passwords"**
4. Select app: **"Mail"**
5. Select device: **"Other (Custom name)"**
6. Enter name: **"Voice Discovery Bot"**
7. Click **"Generate"**
8. **Copy the 16-character password** (looks like: `abcd efgh ijkl mnop`)

### Step 2: Add Secrets to GitHub

1. Go to your GitHub repository
2. Click **Settings** (top navigation)
3. Click **Secrets and variables** → **Actions** (left sidebar)
4. Click **"New repository secret"**

**Add Secret #1:**
- Name: `SENDER_EMAIL`
- Secret: Your Gmail address (the one you'll send FROM)
  - Example: `your-email@gmail.com`
- Click **"Add secret"**

**Add Secret #2:**
- Name: `SENDER_APP_PASSWORD`
- Secret: The 16-character password from Step 1
  - Example: `abcdefghijklmnop` (no spaces!)
- Click **"Add secret"**

### Step 3: Push Workflow to GitHub

```bash
cd "C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp"

# Add the workflow file
git add .github/workflows/voice-discovery.yml

# Commit
git commit -m "feat: Add automated voice discovery with email delivery"

# Push to GitHub
git push
```

### Step 4: Test It!

1. Go to your repo on GitHub
2. Click **"Actions"** tab
3. Click **"Voice Effect Discovery"** workflow
4. Click **"Run workflow"** button
5. Click **"Run workflow"** (green button)
6. Wait ~1 minute
7. Check **yahalom.assets@gmail.com** for email!

---

## 📧 What You'll Receive

Every 3 days, you'll get an email like this:

```
From: Voice Discovery Bot
To: yahalom.assets@gmail.com
Subject: 🎤 Voice Effect Discovery Report - 20260224

Hi!

The voice effect discovery has completed! 🎵

📊 Discovery Report: VOICE_DISCOVERY_20260224.md
📅 Date: 2026-02-24
🔍 Effects Analyzed: 10+

🔥 Top Trending:
1. ⭐⭐⭐⭐⭐ Anime Voice Filter (Very High Priority)
2. ⭐⭐⭐⭐ Demon/Devil Voice (Easy Implementation)
3. ⭐⭐⭐⭐ Auto-Tune Effect (High Demand)
4. ⭐⭐⭐ Ghost Voice (Quick Win)
5. ⭐⭐⭐ Baby Voice (Very Easy)

💡 Quick Action Items:
1. Review the attached report
2. Select 1-2 effects to implement
3. Run: /create-voice-effect [effect-name]

📎 Full report attached!

Next discovery: 2026-02-27

Happy coding! 🎤✨
```

**Attached:** Complete markdown report with:
- 10+ voice effect suggestions
- DSP implementation specs
- Popularity rankings
- Difficulty ratings
- Implementation time estimates

---

## 📅 Schedule

**Frequency:** Every 3 days at 9 AM UTC (11 AM Israel time)

**Next Runs:**
- 1st run: When you manually trigger (for testing)
- Auto runs: Every 3 days after that

**Want to change frequency?**
Edit `.github/workflows/voice-discovery.yml`, line 5:
```yaml
# Every day:
- cron: '0 9 * * *'

# Every 2 days:
- cron: '0 9 */2 * *'

# Every week (Sunday):
- cron: '0 9 * * 0'
```

---

## ✅ Checklist

Before everything works, make sure:

- [ ] **2-Step Verification** enabled on Gmail
- [ ] **App Password** generated
- [ ] **SENDER_EMAIL** secret added to GitHub
- [ ] **SENDER_APP_PASSWORD** secret added to GitHub
- [ ] **Workflow file** pushed to GitHub
- [ ] **Test run** completed successfully
- [ ] **Email received** at yahalom.assets@gmail.com

---

## 🔧 Troubleshooting

### "Email not received"

**Check:**
1. ✓ Spam/junk folder in Gmail
2. ✓ Secrets are correct (go to Settings → Secrets)
3. ✓ Workflow ran successfully (Actions tab, check for green checkmark)
4. ✓ App password has no spaces

**Fix:**
- Regenerate app password
- Update `SENDER_APP_PASSWORD` secret
- Run workflow again

### "Workflow failed"

**Check Actions tab:**
1. Click the failed run
2. Click "Send Email with Report"
3. Read error message

**Common issues:**
- Wrong app password → Regenerate and update secret
- Wrong email format → Check SENDER_EMAIL secret
- Missing secret → Add both secrets

### "App Password option not visible"

**Fix:**
1. Enable 2-Step Verification first
2. Wait a few minutes
3. Refresh page
4. App passwords should now be visible

---

## 🎯 Quick Commands

### Test Workflow Now
```bash
# Via GitHub UI:
Actions → Voice Effect Discovery → Run workflow

# Or push a commit to trigger:
git commit --allow-empty -m "test: Trigger voice discovery"
git push
```

### Check Email Delivery Logs
```bash
# Go to GitHub Actions
# Click latest run
# Check "Send Email with Report" step
```

### View Report in Repository
```bash
# Reports are auto-committed to repo
# Look for: VOICE_DISCOVERY_*.md files
```

---

## 💡 Pro Tips

### 1. Set Email Filter

Create Gmail filter to:
- Star emails from "Voice Discovery Bot"
- Apply label "Voice Effects"
- Never send to spam

### 2. Mobile Notifications

Enable Gmail notifications for this email on your phone to get instant alerts!

### 3. Quick Implementation

When you receive an email:
1. Open project in Claude Code
2. Run: `/create-voice-effect [name-from-report]`
3. Claude will implement it automatically!

### 4. Share with Team

Forward discovery emails to your team:
- Developers can pick effects to implement
- Designers can plan UI updates
- Marketing can prepare feature announcements

---

## 🔐 Security Notes

**Your secrets are safe:**
- ✓ Stored encrypted in GitHub
- ✓ Never visible in logs
- ✓ Not accessible to public
- ✓ Only used by your workflows

**App passwords:**
- ✓ Can be revoked anytime
- ✓ Only for this specific use
- ✓ Doesn't give full account access
- ✓ Can generate multiple for different apps

**Best practices:**
- Don't share app passwords
- Regenerate if compromised
- Use dedicated email for automation (optional)

---

## 📞 Support

### Still stuck?

1. **Check GitHub Actions logs:**
   - Go to Actions tab
   - Click failed run
   - Read error messages

2. **Verify Gmail settings:**
   - 2-Step Verification: ON
   - App Password: Generated
   - No typos in secrets

3. **Test email manually:**
   ```python
   # Create test_email.py:
   import smtplib

   sender = "your-email@gmail.com"
   password = "your-app-password"

   server = smtplib.SMTP('smtp.gmail.com', 587)
   server.starttls()
   server.login(sender, password)
   print("✓ Login successful!")
   server.quit()
   ```

---

## 🎉 You're All Set!

Once configured, you'll automatically receive:

✅ Voice effect discovery reports
✅ Every 3 days
✅ At yahalom.assets@gmail.com
✅ With complete implementation specs
✅ Priority recommendations
✅ DSP parameters ready to use

Just review and implement! 🎤✨

---

**Setup Guide Version:** 1.0
**Last Updated:** 2026-02-21
**Your Email:** yahalom.assets@gmail.com
**Status:** Ready to configure
**Time to Setup:** 5 minutes
