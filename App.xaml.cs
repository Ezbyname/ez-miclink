using BluetoothMicrophoneApp.Services;
using BluetoothMicrophoneApp.Pages;

namespace BluetoothMicrophoneApp;

public partial class App : Application
{
	private readonly IAuthService _authService;

	public App(IAuthService authService)
	{
		InitializeComponent();
		_authService = authService;

		// Subscribe to auth state changes
		_authService.AuthStateChanged += OnAuthStateChanged;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Start with splash screen
		var splashPage = new SplashPage();
		var window = new Window(splashPage);

		// After splash, show main shell and check authentication
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await ShowSplashScreenAsync(window);
			await CheckAuthenticationAsync();
		});

		return window;
	}

	private async Task ShowSplashScreenAsync(Window window)
	{
		try
		{
			// Wait for 4 seconds to show splash screen
			await Task.Delay(4000);

			// Switch to main shell
			window.Page = new AppShell();
			MainPage = window.Page;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[App] Error showing splash: {ex.Message}");
		}
	}

	private async Task CheckAuthenticationAsync()
	{
		System.Diagnostics.Debug.WriteLine("[App] Checking authentication status...");

		try
		{
			// Try to restore previous session
			var user = await _authService.RestoreSessionAsync();

			if (user != null)
			{
				System.Diagnostics.Debug.WriteLine($"[App] Session restored: {user.Name}");
				// User is authenticated, continue to MainPage
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("[App] No session found, showing login page");
				// No session, show login page
				await ShowLoginPageAsync();
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[App] Error checking authentication: {ex.Message}");
			// On error, show login page
			await ShowLoginPageAsync();
		}
	}

	private async Task ShowLoginPageAsync()
	{
		if (MainPage != null)
		{
			var loginPage = new LoginPage(_authService);
			await MainPage.Navigation.PushModalAsync(loginPage);
		}
	}

	private void OnAuthStateChanged(object? sender, Models.User? user)
	{
		System.Diagnostics.Debug.WriteLine($"[App] Auth state changed: {(user != null ? user.Name : "Logged out")}");

		if (user == null)
		{
			// User logged out, show login page
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				await ShowLoginPageAsync();
			});
		}
	}
}