using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.Pages;

public partial class LoginPage : ContentPage
{
	private readonly IAuthService _authService;

	public LoginPage(IAuthService authService)
	{
		InitializeComponent();
		_authService = authService;
	}

	private async void OnPhoneLoginClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[LoginPage] Phone login clicked");

		try
		{
			// Show phone number input dialog
			var phoneNumber = await DisplayPromptAsync(
				"Phone Login",
				"Enter your phone number:",
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
				var code = await DisplayPromptAsync(
					"Verification Code",
					$"Enter the 6-digit code sent to {phoneNumber}:",
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
						await DisplayAlert("Error", "Invalid verification code. Please try again.", "OK");
					}
				}
			}
			else
			{
				await DisplayAlert("Error", "Failed to send verification code. Please try again.", "OK");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LoginPage] Phone login error: {ex.Message}");
			await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
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
				await DisplayAlert("Error", "Google login failed. Please try again.", "OK");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LoginPage] Google login error: {ex.Message}");
			await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
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
				await DisplayAlert("Error", "Apple login failed. Please try again.", "OK");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LoginPage] Apple login error: {ex.Message}");
			await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
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
			await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
		}
		finally
		{
			LoadingIndicator.IsVisible = false;
			LoadingIndicator.IsRunning = false;
		}
	}
}
