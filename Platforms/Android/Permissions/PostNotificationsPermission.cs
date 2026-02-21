using Android.Content.PM;

namespace BluetoothMicrophoneApp;

/// <summary>
/// Custom permission for POST_NOTIFICATIONS (Android 13+)
/// Required for showing foreground service notifications
/// </summary>
public class PostNotificationsPermission : Permissions.BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		new (string androidPermission, bool isRuntime)[]
		{
			(global::Android.Manifest.Permission.PostNotifications!, true)
		};

	public override Task<PermissionStatus> CheckStatusAsync()
	{
		if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.Tiramisu)
		{
			return Task.FromResult(PermissionStatus.Granted);
		}

		return Task.FromResult(GetPermissionStatus(global::Android.Manifest.Permission.PostNotifications));
	}

	private PermissionStatus GetPermissionStatus(string permission)
	{
		var context = Platform.AppContext;
		var result = global::AndroidX.Core.Content.ContextCompat.CheckSelfPermission(context, permission);
		return result == Permission.Granted ? PermissionStatus.Granted : PermissionStatus.Denied;
	}
}
