using BluetoothMicrophoneApp.Services;
using BluetoothMicrophoneApp.UI;

namespace BluetoothMicrophoneApp.Pages;

public partial class LoginPage : ContentPage
{
	private readonly IAuthService _authService;

	public LoginPage(IAuthService authService)
	{
		InitializeComponent();
		_authService = authService;

		// Initialize DialogService with root grid for custom dialogs
		DialogService.Initialize(RootGrid);
	}

	private async void OnPhoneLoginClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[LoginPage] Phone login clicked");

		try
		{
			// Show phone number input dialog
			var phoneNumber = await DialogService.ShowTextInputAsync(
				title: "Phone Login",
				message: "Enter your phone number:",
				icon: "📱",
				placeholder: "+1234567890",
				keyboard: Keyboard.Telephone);

			if (string.IsNullOrWhiteSpace(phoneNumber))
				return;

			LoadingIndicator.IsVisible = true;
			LoadingIndicator.IsRunning = true;

			// Send verification code
			var codeSent = await _authService.LoginWithPhoneNumberAsync(phoneNumber);

			if (codeSent)
			{
				// Show verification code input dialog
				var code = await DialogService.ShowTextInputAsync(
					title: "Verification Code",
					message: $"Enter the 6-digit code sent to {phoneNumber}:",
					icon: "🔐",
					placeholder: "123456",
					maxLength: 6,
					keyboard: Keyboard.Numeric);

				if (!string.IsNullOrWhiteSpace(code))
				{
					var user = await _authService.VerifyPhoneNumberAsync(phoneNumber, code);

					if (user != null)
					{
						await Navigation.PopModalAsync();
					}
					else
					{
						await DialogService.ShowErrorAsync("Error", "Invalid verification code. Please try again.");
					}
				}
			}
			else
			{
				await DialogService.ShowErrorAsync("Error", "Failed to send verification code. Please try again.");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LoginPage] Phone login error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"Login failed: {ex.Message}");
		}
		finally
		{
			LoadingIndicator.IsVisible = false;
			LoadingIndicator.IsRunning = false;
		}
	}

	private async void OnGoogleLoginClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[LoginPage] Google login clicked");

		try
		{
			LoadingIndicator.IsVisible = true;
			LoadingIndicator.IsRunning = true;

			var user = await _authService.LoginWithGoogleAsync();

			if (user != null)
			{
				await Navigation.PopModalAsync();
			}
			else
			{
				await DialogService.ShowErrorAsync("Error", "Google login failed. Please try again.");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LoginPage] Google login error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"Login failed: {ex.Message}");
		}
		finally
		{
			LoadingIndicator.IsVisible = false;
			LoadingIndicator.IsRunning = false;
		}
	}

	private async void OnAppleLoginClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[LoginPage] Apple login clicked");

		try
		{
			LoadingIndicator.IsVisible = true;
			LoadingIndicator.IsRunning = true;

			var user = await _authService.LoginWithAppleAsync();

			if (user != null)
			{
				await Navigation.PopModalAsync();
			}
			else
			{
				await DialogService.ShowErrorAsync("Error", "Apple login failed. Please try again.");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LoginPage] Apple login error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"Login failed: {ex.Message}");
		}
		finally
		{
			LoadingIndicator.IsVisible = false;
			LoadingIndicator.IsRunning = false;
		}
	}

	private async void OnGuestClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[LoginPage] Guest login clicked");

		try
		{
			LoadingIndicator.IsVisible = true;
			LoadingIndicator.IsRunning = true;

			var user = await _authService.ContinueAsGuestAsync();

			await Navigation.PopModalAsync();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LoginPage] Guest login error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"Login failed: {ex.Message}");
		}
		finally
		{
			LoadingIndicator.IsVisible = false;
			LoadingIndicator.IsRunning = false;
		}
	}
}
