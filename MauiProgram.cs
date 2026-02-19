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
#if ANDROID
		builder.Services.AddSingleton<IBluetoothService, Platforms.Android.Services.BluetoothService>();
		builder.Services.AddSingleton<IAudioService, Platforms.Android.Services.AudioService>();
		builder.Services.AddSingleton<IConnectivityDiagnostics, Platforms.Android.Services.ConnectivityDiagnostics>();
#elif IOS
		builder.Services.AddSingleton<IBluetoothService, Platforms.iOS.Services.BluetoothService>();
		builder.Services.AddSingleton<IAudioService, Platforms.iOS.Services.AudioService>();
#endif

		// Register pages
		builder.Services.AddSingleton<MainPage>();

		return builder.Build();
	}
}
