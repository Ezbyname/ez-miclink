# Debug: Device Name Persistence Issue - 2026-02-21

## Problem

Custom device names edited by the user are not persisting across app restarts. When the app is reopened or devices are rescanned, the original device names appear instead of the custom names.

## Investigation

Added comprehensive debug logging to trace the persistence issue through the entire flow.

---

## Debug Logging Added

### 1. **DeviceNameManager.cs** - All Methods Instrumented

#### GetDisplayName()
Logs when retrieving a device's display name:
```
[DeviceNameManager] GetDisplayName called:
  → Device Address: XX:XX:XX:XX:XX:XX
  → Original Name: 'Device Original Name'
  → Preferences Key: device_name_XX:XX:XX:XX:XX:XX
  → Has Custom Name: true/false
  → Custom Name Retrieved: 'Custom Name' or ''
  → Final Name Returned: 'Custom Name' or 'Original Name'
```

**What to Check:**
- Is the device address consistent between saves and loads?
- Does `Has Custom Name` return true after saving?
- Is the custom name retrieved correctly from Preferences?

#### SetCustomName()
Logs when saving a custom device name:
```
[DeviceNameManager] SetCustomName called:
  → Device Address: XX:XX:XX:XX:XX:XX
  → Custom Name: 'New Custom Name'
  → Preferences Key: device_name_XX:XX:XX:XX:XX:XX
  → Action: SAVING custom name
  → Verification: Value saved = 'New Custom Name'
  → Verification: ContainsKey = true
```

**What to Check:**
- Is the name being saved correctly?
- Does verification confirm the save worked?
- Does `ContainsKey` return true after saving?

#### RemoveCustomName()
Logs when removing a custom name:
```
[DeviceNameManager] RemoveCustomName called:
  → Device Address: XX:XX:XX:XX:XX:XX
  → Preferences Key: device_name_XX:XX:XX:XX:XX:XX
  → Verification: ContainsKey = false
```

### 2. **MainPage.xaml.cs** - Scan Flow

#### OnScanClicked() - Applying Custom Names
Logs when applying custom names after scanning:
```
[MainPage] ===== Applying Custom Names to 3 Devices =====
[MainPage] Processing device:
  → Original Name: 'JBL Flip 5'
  → Device Address: 'AA:BB:CC:DD:EE:FF'
  → Display Name Returned: 'My Speaker'
  → Device.Name Set To: 'My Speaker'
[MainPage] Processing device:
  → Original Name: 'Galaxy Buds'
  → Device Address: '11:22:33:44:55:66'
  → Display Name Returned: 'Galaxy Buds'
  → Device.Name Set To: 'Galaxy Buds'
[MainPage] ===== Custom Names Applied =====
```

**What to Check:**
- Are device addresses the same as when names were saved?
- Is GetDisplayName returning the custom name?
- Is device.Name being set correctly?

#### OnEditDeviceNameClicked() - User Editing Name
Logs the entire edit flow:
```
[MainPage] ===== OnEditDeviceNameClicked =====
[MainPage] Device Address: 'AA:BB:CC:DD:EE:FF'
[MainPage] Device Current Name: 'JBL Flip 5'
[MainPage] Current Display Name: 'JBL Flip 5'
[MainPage] User entered: 'My Speaker'
[MainPage] Calling SetCustomName...
[DeviceNameManager] SetCustomName called:
  → Device Address: AA:BB:CC:DD:EE:FF
  → Custom Name: 'My Speaker'
  → Preferences Key: device_name_AA:BB:CC:DD:EE:FF
  → Action: SAVING custom name
  → Verification: Value saved = 'My Speaker'
  → Verification: ContainsKey = true
[MainPage] SetCustomName completed
[MainPage] Updated device.Name to: 'My Speaker'
[MainPage] Collection view refreshed
[MainPage] Device renamed: AA:BB:CC:DD:EE:FF → My Speaker
[MainPage] ===== OnEditDeviceNameClicked END =====
```

**What to Check:**
- Is the device address correct?
- Does SetCustomName verify the save was successful?
- Is the UI updated correctly?

---

## How to Test

### Test 1: Edit Device Name
1. Build and install the updated app
2. Open the app
3. Scan for devices
4. Tap the edit button (✏️) on a device
5. Enter a custom name (e.g., "My Headphones")
6. Check the debug logs

**Expected Logs:**
```
[MainPage] ===== OnEditDeviceNameClicked =====
[MainPage] Device Address: 'XX:XX:XX:XX:XX:XX'
[MainPage] Calling SetCustomName...
[DeviceNameManager] SetCustomName called:
  → Device Address: XX:XX:XX:XX:XX:XX
  → Custom Name: 'My Headphones'
  → Action: SAVING custom name
  → Verification: Value saved = 'My Headphones'
  → Verification: ContainsKey = true
[MainPage] SetCustomName completed
```

**If Verification Fails:**
- MAUI Preferences API is not working on this device/Android version
- Possible permission issue
- Possible storage issue

### Test 2: Rescan Devices (Same Session)
1. After editing a device name, go back
2. Tap "Scan for Devices" again
3. Check if the custom name appears

**Expected Logs:**
```
[MainPage] ===== Applying Custom Names to X Devices =====
[MainPage] Processing device:
  → Original Name: 'Original Name'
  → Device Address: 'XX:XX:XX:XX:XX:XX'
[DeviceNameManager] GetDisplayName called:
  → Device Address: XX:XX:XX:XX:XX:XX
  → Has Custom Name: true
  → Custom Name Retrieved: 'My Headphones'
  → Final Name Returned: 'My Headphones'
[MainPage]   → Display Name Returned: 'My Headphones'
```

**If Custom Name Not Found:**
- Device address changed between edit and scan
- Preferences.Set() failed silently
- Preferences.Get() not reading from same storage

### Test 3: Restart App
1. Edit a device name
2. Close the app completely (swipe away from recent apps)
3. Reopen the app
4. Scan for devices
5. Check if the custom name appears

**Expected Logs:**
```
[MainPage] ===== Applying Custom Names to X Devices =====
[MainPage] Processing device:
  → Device Address: 'XX:XX:XX:XX:XX:XX'
[DeviceNameManager] GetDisplayName called:
  → Device Address: XX:XX:XX:XX:XX:XX
  → Has Custom Name: true
  → Custom Name Retrieved: 'My Headphones'
  → Final Name Returned: 'My Headphones'
```

**If Custom Name Lost After Restart:**
- Preferences.Set() is using temporary storage
- Android cleared app data
- Preferences storage path issue

---

## Potential Root Causes

### 1. Device Address Changing
**Symptom:** Custom name saves successfully but not found on rescan
**Logs to Check:**
- Compare device address in SetCustomName vs GetDisplayName
- Device address should be identical (MAC address format: XX:XX:XX:XX:XX:XX)

**Possible Causes:**
- Bluetooth scan returning devices with different address format
- Device has multiple addresses (BR/EDR vs BLE)

### 2. Preferences API Not Persisting
**Symptom:** Verification shows ContainsKey=true but GetDisplayName returns empty after restart
**Logs to Check:**
- SetCustomName verification shows success
- GetDisplayName shows Has Custom Name = false after restart

**Possible Causes:**
- MAUI Preferences using session storage instead of persistent storage
- Android SharedPreferences not being committed
- App data being cleared by system

### 3. Storage Permission Issue
**Symptom:** SetCustomName doesn't throw error but Verification shows ContainsKey=false
**Possible Causes:**
- Android 10+ scoped storage restrictions
- App doesn't have storage permission
- Preferences API using inaccessible storage location

### 4. Preferences Key Mismatch
**Symptom:** Save succeeds but retrieval uses different key
**Logs to Check:**
- Compare "Preferences Key" in SetCustomName vs GetDisplayName
- Keys should be identical: "device_name_XX:XX:XX:XX:XX:XX"

### 5. Device Name Overwrite
**Symptom:** Custom name found but then overwritten with original name
**Logs to Check:**
- Check if device.Name is being set multiple times
- Check if GetDisplayName is called after device.Name is already set

---

## Viewing Logs

### Method 1: Visual Studio Output Window
1. In Visual Studio, go to View → Output
2. Show output from: Debug
3. Look for lines starting with:
   - `[DeviceNameManager]`
   - `[MainPage]`

### Method 2: ADB Logcat
```bash
adb logcat | grep -E "(DeviceNameManager|MainPage)"
```

### Method 3: Android Studio Logcat
1. Open Android Studio
2. View → Tool Windows → Logcat
3. Filter: "DeviceNameManager OR MainPage"

---

## Next Steps Based on Logs

### If Verification Shows Success But Name Not Found:
1. Check device addresses match
2. Test Preferences API directly:
   ```csharp
   Preferences.Set("test_key", "test_value");
   var retrieved = Preferences.Get("test_key", "");
   Debug.WriteLine($"Test: {retrieved}"); // Should be "test_value"
   ```

### If Verification Shows Failure:
1. Preferences API not working
2. Try alternative storage: SecureStorage or file-based storage

### If Device Address Changes:
1. Use normalized address format
2. Remove colons/dashes: "AABBCCDDEEFF" instead of "AA:BB:CC:DD:EE:FF"
3. Convert to uppercase consistently

### If Names Lost After Restart:
1. MAUI Preferences bug on this Android version
2. Try using Android SharedPreferences directly
3. Implement custom file-based storage

---

## Testing Checklist

- [ ] Edit device name
- [ ] Check SetCustomName verification logs
- [ ] Rescan devices in same session
- [ ] Check GetDisplayName logs show custom name
- [ ] Close and reopen app
- [ ] Scan for devices
- [ ] Check if custom name persists
- [ ] Record all device addresses from logs
- [ ] Compare addresses between save and load
- [ ] Test with multiple devices

---

## Build Instructions

App has been built successfully with debug logging.

**To install:**
1. Connect your Android device via USB
2. Enable USB debugging
3. Run: `adb devices` (should show your device)
4. Run: `dotnet build -t:Install -f net9.0-android`

**To view logs while testing:**
```bash
adb logcat | grep -E "(DeviceNameManager|MainPage)"
```

---

## Expected Outcome

With these logs, we should be able to identify:
1. Whether Preferences.Set() is working
2. Whether Preferences.Get() is working
3. Whether device addresses are consistent
4. Whether custom names persist across app restarts
5. The exact point where the persistence chain breaks

Once we identify the root cause, we can implement the appropriate fix.

---

**Status:** ✅ Debug logging added, ready for testing
**Date:** 2026-02-21
**Next Step:** Test on device and review logs
