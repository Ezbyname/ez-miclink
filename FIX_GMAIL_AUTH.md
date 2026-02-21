# Fix Gmail Authentication Error

**Error:** `530-5.7.0 Authentication Required`

This means Gmail is rejecting the login. Let's fix it step-by-step.

---

## ✅ Solution Steps

### Step 1: Verify Gmail App Password is Created Correctly

1. **Go to Google Account Security:**
   - https://myaccount.google.com/security

2. **Enable 2-Step Verification** (if not already):
   - Scroll to "2-Step Verification"
   - Click it and follow setup
   - **This MUST be enabled before app passwords work!**

3. **Create App Password:**
   - After 2-Step is enabled, refresh the security page
   - Scroll down to "App passwords" (should now be visible)
   - Click "App passwords"
   - Select app: **Mail**
   - Select device: **Other (Custom name)**
   - Enter name: **"Voice Discovery Bot"**
   - Click **Generate**
   - You'll see a **16-character password** like: `abcd efgh ijkl mnop`
   - **COPY IT NOW** (remove spaces: `abcdefghijklmnop`)

### Step 2: Update GitHub Secrets

1. **Go to GitHub Secrets:**
   - https://github.com/Ezbyname/ez-miclink/settings/secrets/actions

2. **Check/Update SENDER_EMAIL:**
   - If it exists, click it → "Update"
   - If not, click "New repository secret"
   - Name: `SENDER_EMAIL`
   - Value: **Your full Gmail address** (e.g., `youremail@gmail.com`)
   - Click "Add secret" or "Update secret"

3. **Check/Update SENDER_APP_PASSWORD:**
   - If it exists, click it → "Update"
   - If not, click "New repository secret"
   - Name: `SENDER_APP_PASSWORD`
   - Value: **The 16-character code WITHOUT SPACES**
     - Example: `abcdefghijklmnop` ✅
     - NOT: `abcd efgh ijkl mnop` ❌
   - Click "Add secret" or "Update secret"

### Step 3: Push Updated Workflow

The workflow has been fixed. Let's push it:

```powershell
cd "C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp"

git add .github/workflows/voice-discovery.yml
git commit -m "fix: Update email workflow with secure connection"
git push
```

### Step 4: Test Again

1. **Go to Actions:**
   - https://github.com/Ezbyname/ez-miclink/actions

2. **Click:** "Voice Effect Discovery" (left sidebar)

3. **Click:** "Run workflow" button (top right)

4. **Click:** Green "Run workflow" button

5. **Wait ~30 seconds**, then refresh page

6. **Check the run:**
   - Should show green checkmark ✅
   - Click it to see details
   - If still fails, check error message

7. **Check your email:**
   - Go to: yahalom.assets@gmail.com
   - Look for email from "Voice Discovery Bot"
   - Check spam folder if not in inbox

---

## 🔍 Common Issues & Solutions

### Issue 1: "2-Step Verification not enabled"

**Solution:**
- Go to: https://myaccount.google.com/security
- Click "2-Step Verification"
- Follow setup wizard (verify phone number)
- After enabling, "App passwords" option will appear

### Issue 2: "App passwords option not visible"

**Solution:**
- Make sure 2-Step Verification is fully enabled
- Wait 5 minutes after enabling 2-Step
- Refresh the security page
- Sign out and sign back in to Google
- Try in incognito/private browsing mode

### Issue 3: "Wrong username or password"

**Possible causes:**
1. **SENDER_EMAIL** is wrong
   - Must be full email: `user@gmail.com`
   - Not just username: `user`
2. **SENDER_APP_PASSWORD** has spaces
   - Remove ALL spaces: `abcdefghijklmnop`
   - Not: `abcd efgh ijkl mnop`
3. **Using regular password instead of app password**
   - Must use the 16-char app password
   - NOT your regular Gmail password

**Fix:**
- Delete and recreate both secrets
- Make sure to copy app password exactly (no spaces!)

### Issue 4: "Less secure app access"

Gmail no longer supports "less secure apps". You MUST use:
- ✅ 2-Step Verification + App Password
- ❌ NOT regular password

### Issue 5: "Account access blocked"

**Solution:**
1. Check Gmail for security alert email
2. Click "Yes, it was me" if asked
3. Or go to: https://myaccount.google.com/notifications
4. Review recent security events
5. Approve any blocked sign-ins

---

## 🧪 Test Email Manually

Want to test if your credentials work? Create a quick test:

**Create test-email.py:**

```python
import smtplib
from email.mime.text import MIMEText

sender = "YOUR_EMAIL@gmail.com"  # Replace
password = "YOUR_16_CHAR_PASSWORD"  # Replace (no spaces!)
receiver = "yahalom.assets@gmail.com"

msg = MIMEText("Test email from Voice Discovery Bot!")
msg['Subject'] = 'Test Email'
msg['From'] = sender
msg['To'] = receiver

try:
    server = smtplib.SMTP('smtp.gmail.com', 587)
    server.starttls()
    print("Connecting to Gmail...")
    server.login(sender, password)
    print("✅ Login successful!")
    server.send_message(msg)
    print("✅ Email sent!")
    server.quit()
except Exception as e:
    print(f"❌ Error: {e}")
```

**Run:**
```powershell
python test-email.py
```

**Expected:**
```
Connecting to Gmail...
✅ Login successful!
✅ Email sent!
```

If this works, your credentials are correct. Problem might be in GitHub secrets formatting.

---

## 📋 Checklist

Before running workflow again:

- [ ] 2-Step Verification is **enabled**
- [ ] App Password is **generated** (16 characters)
- [ ] `SENDER_EMAIL` secret contains **full email address**
- [ ] `SENDER_APP_PASSWORD` secret contains **16 chars, NO SPACES**
- [ ] Workflow file is **updated and pushed**
- [ ] Test email script works (optional)

---

## 🎯 Alternative: Use Different Email Service

If Gmail continues to be problematic, you can use:

### Option 1: Outlook/Hotmail

**SMTP Settings:**
```yaml
server_address: smtp-mail.outlook.com
server_port: 587
username: your-email@outlook.com
password: your-outlook-password
```

**No app password needed!** Just use your regular password.

### Option 2: SendGrid (Free Tier)

1. Sign up: https://sendgrid.com/ (Free: 100 emails/day)
2. Get API key
3. Update workflow to use SendGrid API

---

## 💡 Pro Tip: Use Dedicated Email

For better organization:

1. **Create new Gmail:** `ez-miclink-bot@gmail.com`
2. **Use only for automation**
3. **Forward to your main email:**
   - Gmail → Settings → Forwarding
   - Add: yahalom.assets@gmail.com
4. **Update secrets** with new email

**Benefits:**
- Cleaner main inbox
- Better tracking
- Can revoke anytime

---

## ✅ Success Indicators

You'll know it's working when:

1. **GitHub Actions:** Green checkmark ✅
2. **Action logs:** "Email sent successfully"
3. **Your inbox:** Email received at yahalom.assets@gmail.com
4. **Email subject:** "🎤 Voice Effect Discovery Report - [date]"
5. **Attachment:** Markdown report file

---

## 🆘 Still Not Working?

1. **Check Gmail is not blocking:**
   - https://myaccount.google.com/device-activity
   - Look for blocked sign-in attempts

2. **Try from different service:**
   - Use Outlook instead of Gmail (simpler)

3. **Use SendGrid API:**
   - More reliable for automation
   - Free tier available

4. **Manually send test email:**
   - Run the Python script above
   - Confirms credentials work

---

**Need more help?** Let me know which step is failing and I'll help troubleshoot! 🚀
