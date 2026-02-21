# Quick Integration Test Checklist

**Tester:** ______________ | **Date:** ______________ | **Build:** ______________
**Device:** ______________ | **Android:** ______________ | **Speaker:** ______________

---

## ✅ Pre-Test Setup
- [ ] App installed
- [ ] Bluetooth speaker paired (Settings)
- [ ] Phone charged (>50%)
- [ ] Quiet environment

---

## 🔵 Bluetooth Enable Tests

### Android 12+
- [ ] Turn off Bluetooth
- [ ] Open app → Scan
- [ ] Tap "Turn On"
- [ ] System dialog appears
- [ ] Tap "Allow"
- [ ] Bluetooth turns on ✓

### Android 11-
- [ ] Turn off Bluetooth
- [ ] Open app → Scan
- [ ] Tap "Turn On"
- [ ] Bluetooth turns on (no dialog) ✓

---

## 🎤 Audio Routing Tests

### Basic Routing
- [ ] Scan → Select speaker → Connect
- [ ] Tap "Start Audio"
- [ ] Speak into **phone mic**
- [ ] Hear from **Bluetooth speaker** ✓

### Effects
- [ ] Select Robot effect
- [ ] Speak → Hear robot voice ✓
- [ ] Select Echo effect
- [ ] Speak → Hear echo ✓

### Volume
- [ ] Set volume to 50% → Quieter ✓
- [ ] Set volume to 150% → Louder ✓

---

## 🔐 Authentication Tests

- [ ] Guest login works
- [ ] Phone login works (any 6-digit code)
- [ ] Google login works
- [ ] Apple login works
- [ ] Close app → Reopen → Still logged in ✓
- [ ] Logout works

---

## 📱 Device Management

- [ ] Scan finds paired devices
- [ ] Connect to device works
- [ ] Rename device → Name persists ✓
- [ ] Delete device → Unpairs from phone ✓

---

## ⚙️ Settings

- [ ] Set default volume 150% → Restart → Still 150% ✓
- [ ] Create saved sound preset
- [ ] Use saved sound preset
- [ ] Delete saved sound preset
- [ ] Presets survive app restart ✓

---

## 🔄 Background Operation

- [ ] Start audio → Press Home
- [ ] Notification appears ✓
- [ ] Audio continues in background ✓
- [ ] Tap notification → Returns to app ✓
- [ ] Stop audio → Notification disappears ✓

---

## ⚠️ Edge Cases

- [ ] Turn off BT speaker during audio → Graceful ✓
- [ ] Rapid effect switching → No crash ✓

---

## 🎨 UI Checks

- [ ] Settings icon (⚙️) fully visible (not cut off) ✓
- [ ] No "Home" text in header ✓
- [ ] All dialogs display correctly ✓

---

## 📊 Results

**Pass:** ___ / 33 | **Fail:** ___ | **Pass Rate:** ____%

**Release Recommendation:** ⬜ YES | ⬜ NO | ⬜ WITH NOTES

---

## 🐛 Critical Issues

1. _________________________________
2. _________________________________

---

**Tester Signature:** _________________ | **Status:** ⬜ APPROVED | ⬜ REJECTED
