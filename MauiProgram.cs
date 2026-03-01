using Microsoft.Extensions.Logging;
using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register services
		// AuthService is shared across all platforms
		builder.Services.AddSingleton<IAuthService, AuthService>();

#if ANDROID
		builder.Services.AddSingleton<IBluetoothService, Platforms.Android.Services.BluetoothService>();

		// AudioService is Singleton (not Transient) because:
		// 1. Audio hardware state must be shared across all pages
		// 2. Start/Stop methods manage resource allocation during use
		// 3. IDisposable.Dispose() handles final cleanup at app shutdown
		builder.Services.AddSingleton<IAudioService, Platforms.Android.Services.AudioService>();

		builder.Services.AddSingleton<IConnectivityDiagnostics, Platforms.Android.Services.ConnectivityDiagnostics>();
#elif IOS
		builder.Services.AddSingleton<IBluetoothService, Platforms.iOS.Services.BluetoothService>();
		builder.Services.AddSingleton<IAudioService, Platforms.iOS.Services.AudioService>();
#endif

		// Register pages
		builder.Services.AddTransient<Pages.SplashPage>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<Pages.SettingsPage>();

		return builder.Build();
	}
}
