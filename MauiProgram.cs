using Microsoft.Extensions.Logging;
using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		// Register App for DI
		builder.Services.AddSingleton<App>();

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
#if ANDROID
		builder.Services.AddSingleton<IBluetoothService, Platforms.Android.Services.BluetoothService>();
		builder.Services.AddSingleton<IAudioService, Platforms.Android.Services.AudioService>();
		builder.Services.AddSingleton<IConnectivityDiagnostics, Platforms.Android.Services.ConnectivityDiagnostics>();
#elif IOS
		builder.Services.AddSingleton<IBluetoothService, Platforms.iOS.Services.BluetoothService>();
		builder.Services.AddSingleton<IAudioService, Platforms.iOS.Services.AudioService>();
#endif

		// Register authentication service (platform-independent)
		builder.Services.AddSingleton<IAuthService, AuthService>();

		// Register pages
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<Pages.LoginPage>();
		builder.Services.AddTransient<Pages.SettingsPage>();

		return builder.Build();
	}
}
