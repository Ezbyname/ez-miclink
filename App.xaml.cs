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
		var window = new Window(new AppShell());

		// Check authentication on startup
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await CheckAuthenticationAsync();
		});

		return window;
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