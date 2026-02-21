#!/usr/bin/env python3
"""
Fallback email sender script for voice discovery reports
Uses standard Python SMTP library with Gmail
"""

import smtplib
import sys
import os
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from email.mime.base import MIMEBase
from email import encoders
from datetime import datetime

def send_discovery_email(report_file, sender_email, sender_password):
    """Send discovery report via Gmail SMTP"""

    recipient = "yahalom.assets@gmail.com"

    # Create message
    msg = MIMEMultipart()
    msg['From'] = f"Voice Discovery Bot <{sender_email}>"
    msg['To'] = recipient
    msg['Subject'] = f"🎤 Voice Effect Discovery Report - {datetime.now().strftime('%Y%m%d')}"

    # Email body
    body = """Hi!

The voice effect discovery has completed! 🎵

📊 Discovery Report: {report_file}
📅 Date: {date}
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

📎 Full report attached with complete DSP specifications!

Next discovery: 3 days from now

Happy coding! 🎤✨

---
Voice Discovery Bot
Automated every 3 days
""".format(report_file=os.path.basename(report_file), date=datetime.now().strftime('%Y-%m-%d'))

    msg.attach(MIMEText(body, 'plain'))

    # Attach report file
    try:
        with open(report_file, 'rb') as f:
            part = MIMEBase('application', 'octet-stream')
            part.set_payload(f.read())
            encoders.encode_base64(part)
            part.add_header('Content-Disposition', f'attachment; filename={os.path.basename(report_file)}')
            msg.attach(part)
    except Exception as e:
        print(f"Warning: Could not attach file: {e}")

    # Send email
    try:
        print("Connecting to Gmail SMTP server...")
        # Try port 465 with SSL
        server = smtplib.SMTP_SSL('smtp.gmail.com', 465)
        print("Connected! Logging in...")
        server.login(sender_email, sender_password)
        print("Logged in! Sending email...")
        server.send_message(msg)
        server.quit()
        print(f"✅ Email sent successfully to {recipient}!")
        return True
    except Exception as e:
        print(f"❌ Error sending email via SSL (port 465): {e}")
        print("Trying STARTTLS (port 587)...")
        try:
            server = smtplib.SMTP('smtp.gmail.com', 587)
            server.starttls()
            server.login(sender_email, sender_password)
            server.send_message(msg)
            server.quit()
            print(f"✅ Email sent successfully to {recipient} via STARTTLS!")
            return True
        except Exception as e2:
            print(f"❌ Error sending email via STARTTLS (port 587): {e2}")
            return False

if __name__ == "__main__":
    if len(sys.argv) != 4:
        print("Usage: send-email.py <report_file> <sender_email> <sender_password>")
        sys.exit(1)

    report_file = sys.argv[1]
    sender_email = sys.argv[2]
    sender_password = sys.argv[3]

    if not os.path.exists(report_file):
        print(f"❌ Report file not found: {report_file}")
        sys.exit(1)

    success = send_discovery_email(report_file, sender_email, sender_password)
    sys.exit(0 if success else 1)
