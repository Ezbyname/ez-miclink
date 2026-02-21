using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;

namespace BluetoothMicrophoneApp;

[Activity(
	Theme = "@style/Maui.SplashTheme",
	MainLauncher = true,
	LaunchMode = LaunchMode.SingleTop,
	ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		System.Diagnostics.Debug.WriteLine("[MainActivity] OnCreate");

		// Request notification permission for Android 13+
		RequestNotificationPermission();
	}

	protected override void OnResume()
	{
		base.OnResume();
		System.Diagnostics.Debug.WriteLine("[MainActivity] OnResume - App returned to foreground");
	}

	protected override void OnPause()
	{
		base.OnPause();
		System.Diagnostics.Debug.WriteLine("[MainActivity] OnPause - App going to background");
	}

	protected override void OnDestroy()
	{
		System.Diagnostics.Debug.WriteLine("[MainActivity] OnDestroy");
		base.OnDestroy();
	}

	private async void RequestNotificationPermission()
	{
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
		{
			try
			{
				var status = await Permissions.CheckStatusAsync<PostNotificationsPermission>();
				if (status != PermissionStatus.Granted)
				{
					await Permissions.RequestAsync<PostNotificationsPermission>();
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[MainActivity] Notification permission error: {ex.Message}");
			}
		}
	}
}
