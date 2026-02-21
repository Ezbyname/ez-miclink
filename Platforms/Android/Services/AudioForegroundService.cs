using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace BluetoothMicrophoneApp.Platforms.Android.Services;

/// <summary>
/// Foreground service that keeps the app running in background.
/// Required for continuous audio processing when app is minimized.
/// </summary>
[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMicrophone)]
public class AudioForegroundService : Service
{
	private const int NotificationId = 1001;
	private const string ChannelId = "audio_service_channel";
	private const string ChannelName = "Microphone Service";

	public override IBinder? OnBind(Intent? intent)
	{
		return null;
	}

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		System.Diagnostics.Debug.WriteLine("[AudioForegroundService] Starting foreground service");

		// Create notification channel (required for Android 8.0+)
		CreateNotificationChannel();

		// Build notification
		var notification = CreateNotification();

		// Start as foreground service
		StartForeground(NotificationId, notification);

		System.Diagnostics.Debug.WriteLine("[AudioForegroundService] Foreground service started");

		// Return sticky so service restarts if killed by system
		return StartCommandResult.Sticky;
	}

	public override void OnDestroy()
	{
		System.Diagnostics.Debug.WriteLine("[AudioForegroundService] Stopping foreground service");
		base.OnDestroy();
	}

	private void CreateNotificationChannel()
	{
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			var channel = new NotificationChannel(
				ChannelId,
				ChannelName,
				NotificationImportance.Low) // Low importance = no sound
			{
				Description = "Keeps microphone active when app is in background"
			};

			var notificationManager = GetSystemService(NotificationService) as NotificationManager;
			notificationManager?.CreateNotificationChannel(channel);

			System.Diagnostics.Debug.WriteLine("[AudioForegroundService] Notification channel created");
		}
	}

	private Notification CreateNotification()
	{
		// Intent to open app when notification is tapped
		var intent = new Intent(this, typeof(MainActivity));
		intent.SetFlags(ActivityFlags.SingleTop);

		var pendingIntent = PendingIntent.GetActivity(
			this,
			0,
			intent,
			Build.VERSION.SdkInt >= BuildVersionCodes.S
				? PendingIntentFlags.Immutable
				: PendingIntentFlags.UpdateCurrent);

		// Build notification
		var builder = new NotificationCompat.Builder(this, ChannelId)
			.SetContentTitle("E-z MicLink")
			.SetContentText("Microphone is active")
			.SetSmallIcon(Resource.Mipmap.appicon) // Use app icon
			.SetContentIntent(pendingIntent)
			.SetOngoing(true) // Cannot be dismissed by user
			.SetCategory(NotificationCompat.CategoryService)
			.SetPriority(NotificationCompat.PriorityLow) // Low priority = less intrusive
			.SetAutoCancel(false);

		return builder.Build();
	}
}
