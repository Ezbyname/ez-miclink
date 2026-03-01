using Android;
using Android.Content.PM;
using Android.OS;

namespace BluetoothMicrophoneApp.Platforms.Android.Services;

/// <summary>
/// Direct Android Bluetooth permission checker (bypasses MAUI abstraction)
/// </summary>
public static class AndroidBluetoothPermissions
{
    /// <summary>
    /// Check if Bluetooth permissions are granted at Android runtime level
    /// </summary>
    public static bool HasBluetoothPermissions()
    {
        try
        {
            var context = Platform.CurrentActivity;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] ❌ Context is null");
                return false;
            }

            System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] Checking Android runtime permissions...");
            System.Diagnostics.Debug.WriteLine($"[AndroidBluetoothPermissions] Android SDK: {Build.VERSION.SdkInt} (API {(int)Build.VERSION.SdkInt})");

            // Android 12+ (API 31+) requires BLUETOOTH_CONNECT and BLUETOOTH_SCAN
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // API 31
            {
                System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] Checking Android 12+ permissions...");

                // Check BLUETOOTH_CONNECT (required for accessing bonded devices)
                var connectPermission = context.CheckSelfPermission(Manifest.Permission.BluetoothConnect);
                System.Diagnostics.Debug.WriteLine($"[AndroidBluetoothPermissions] BLUETOOTH_CONNECT: {connectPermission}");

                // Check BLUETOOTH_SCAN (required for discovering devices)
                var scanPermission = context.CheckSelfPermission(Manifest.Permission.BluetoothScan);
                System.Diagnostics.Debug.WriteLine($"[AndroidBluetoothPermissions] BLUETOOTH_SCAN: {scanPermission}");

                bool hasConnect = connectPermission == Permission.Granted;
                bool hasScan = scanPermission == Permission.Granted;

                if (!hasConnect)
                {
                    System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] ❌ MISSING: BLUETOOTH_CONNECT");
                }
                if (!hasScan)
                {
                    System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] ❌ MISSING: BLUETOOTH_SCAN");
                }

                return hasConnect && hasScan;
            }
            else
            {
                // Android 11 and below - check legacy permissions
                System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] Checking legacy Bluetooth permissions...");

                var bluetoothPermission = context.CheckSelfPermission(Manifest.Permission.Bluetooth);
                var bluetoothAdminPermission = context.CheckSelfPermission(Manifest.Permission.BluetoothAdmin);
                var locationPermission = context.CheckSelfPermission(Manifest.Permission.AccessFineLocation);

                System.Diagnostics.Debug.WriteLine($"[AndroidBluetoothPermissions] BLUETOOTH: {bluetoothPermission}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBluetoothPermissions] BLUETOOTH_ADMIN: {bluetoothAdminPermission}");
                System.Diagnostics.Debug.WriteLine($"[AndroidBluetoothPermissions] ACCESS_FINE_LOCATION: {locationPermission}");

                return bluetoothPermission == Permission.Granted &&
                       bluetoothAdminPermission == Permission.Granted &&
                       locationPermission == Permission.Granted;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidBluetoothPermissions] ❌ Error checking permissions: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Request Bluetooth permissions using Android runtime API
    /// </summary>
    public static void RequestBluetoothPermissions()
    {
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity == null)
            {
                System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] ❌ Activity is null, cannot request permissions");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] Requesting Android runtime permissions...");

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // API 31+
            {
                // Request Android 12+ permissions
                var permissions = new[]
                {
                    Manifest.Permission.BluetoothConnect,
                    Manifest.Permission.BluetoothScan
                };

                activity.RequestPermissions(permissions, 1);
                System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] Requested BLUETOOTH_CONNECT and BLUETOOTH_SCAN");
            }
            else
            {
                // Request legacy permissions
                var permissions = new[]
                {
                    Manifest.Permission.Bluetooth,
                    Manifest.Permission.BluetoothAdmin,
                    Manifest.Permission.AccessFineLocation
                };

                activity.RequestPermissions(permissions, 1);
                System.Diagnostics.Debug.WriteLine("[AndroidBluetoothPermissions] Requested legacy Bluetooth permissions");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AndroidBluetoothPermissions] ❌ Error requesting permissions: {ex.Message}");
        }
    }
}
