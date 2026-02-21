using BluetoothMicrophoneApp.Models;
using BluetoothMicrophoneApp.Services;
using Microsoft.Maui.Storage;

namespace BluetoothMicrophoneApp.Pages;

public partial class SettingsPage : ContentPage
{
	// Preferences keys
	private const string MicGainKey = "mic_gain";
	private const string NoiseReductionKey = "noise_reduction";
	private const string EchoKey = "echo_enabled";
	private const string VoicePresetKey = "voice_preset";
	private const string LimiterKey = "limiter_enabled";
	private const string LatencyModeKey = "latency_mode";
	private const string PreferredDeviceKey = "preferred_device";
	private const string AutoReconnectKey = "auto_reconnect";
	private const string ScanOnLaunchKey = "scan_on_launch";
	private const string ThemeKey = "theme";
	private const string LanguageKey = "language";
	private const string HapticsKey = "haptics_enabled";
	private const string AnimationsKey = "animations_enabled";

	private readonly IAuthService _authService;
	private User? _currentUser;

	public SettingsPage(IAuthService authService)
	{
		InitializeComponent();
		_authService = authService;
		_currentUser = _authService.CurrentUser;

		LoadAllSettings();
		UpdateUserStatusCard();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		UpdateUserStatusCard();
	}

	#region Initialization

	private void LoadAllSettings()
	{
		System.Diagnostics.Debug.WriteLine("[SettingsPage] Loading all settings");

		// Audio & Microphone
		int micGain = Preferences.Get(MicGainKey, 100);
		MicGainSlider.Value = micGain;
		MicGainLabel.Text = $"{micGain}%";

		NoiseReductionSwitch.IsToggled = Preferences.Get(NoiseReductionKey, true);
		EchoSwitch.IsToggled = Preferences.Get(EchoKey, false);
		VoicePresetLabel.Text = Preferences.Get(VoicePresetKey, "Normal");
		LimiterSwitch.IsToggled = Preferences.Get(LimiterKey, true);
		LatencyModeLabel.Text = Preferences.Get(LatencyModeKey, "Balanced");

		// Bluetooth & Devices
		string preferredDevice = Preferences.Get(PreferredDeviceKey, "None");
		PreferredDeviceLabel.Text = preferredDevice;
		AutoReconnectSwitch.IsToggled = Preferences.Get(AutoReconnectKey, true);
		ScanOnLaunchSwitch.IsToggled = Preferences.Get(ScanOnLaunchKey, false);

		// App Preferences
		ThemeLabel.Text = Preferences.Get(ThemeKey, "Dark");
		LanguageLabel.Text = Preferences.Get(LanguageKey, "English");
		HapticsSwitch.IsToggled = Preferences.Get(HapticsKey, true);
		AnimationsSwitch.IsToggled = Preferences.Get(AnimationsKey, true);

		System.Diagnostics.Debug.WriteLine("[SettingsPage] All settings loaded");
	}

	private void UpdateUserStatusCard()
	{
		if (_currentUser == null)
		{
			UserNameLabel.Text = "Guest";
			UserEmailLabel.Text = "Not signed in";
			SignOutSection.IsVisible = false;
			return;
		}

		UserNameLabel.Text = _currentUser.Name;

		if (_currentUser.IsGuest)
		{
			UserEmailLabel.Text = "Guest user";
			SignOutSection.IsVisible = false;
		}
		else
		{
			// Show email or phone number
			if (!string.IsNullOrEmpty(_currentUser.Email))
				UserEmailLabel.Text = _currentUser.Email;
			else if (!string.IsNullOrEmpty(_currentUser.PhoneNumber))
				UserEmailLabel.Text = _currentUser.PhoneNumber;
			else
				UserEmailLabel.Text = $"Signed in with {_currentUser.Provider}";

			SignOutSection.IsVisible = true;
		}
	}

	#endregion

	#region Navigation

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}

	private async void OnManageAccountClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[SettingsPage] Manage account clicked");

		// TODO: Implement account management page
		// For now, show info dialog
		if (_currentUser == null)
		{
			await DisplayAlert("Account", "You are not signed in. Sign in to access account settings.", "OK");
		}
		else if (_currentUser.IsGuest)
		{
			var result = await DisplayAlert("Guest Account",
				"You're using a guest account. Would you like to sign in to save your data?",
				"Sign In",
				"Cancel");

			if (result)
			{
				// Navigate to login page
				await _authService.LogoutAsync();
				Application.Current!.MainPage = new NavigationPage(new LoginPage(_authService));
			}
		}
		else
		{
			await DisplayAlert("Account",
				$"Account: {_currentUser.Name}\nProvider: {_currentUser.Provider}\n\nAccount management coming soon!",
				"OK");
		}
	}

	#endregion

	#region Audio & Microphone

	private void OnMicGainChanged(object? sender, ValueChangedEventArgs e)
	{
		int gain = (int)e.NewValue;
		MicGainLabel.Text = $"{gain}%";
		Preferences.Set(MicGainKey, gain);

		System.Diagnostics.Debug.WriteLine($"[SettingsPage] Mic gain changed: {gain}%");

		// TODO: Apply to audio engine
	}

	private void OnNoiseReductionToggled(object? sender, EventArgs e)
	{
		bool isEnabled = NoiseReductionSwitch.IsToggled;
		Preferences.Set(NoiseReductionKey, isEnabled);

		System.Diagnostics.Debug.WriteLine($"[SettingsPage] Noise reduction: {(isEnabled ? "ON" : "OFF")}");

		// TODO: Apply to audio engine
	}

	private void OnEchoToggled(object? sender, EventArgs e)
	{
		bool isEnabled = EchoSwitch.IsToggled;
		Preferences.Set(EchoKey, isEnabled);

		System.Diagnostics.Debug.WriteLine($"[SettingsPage] Echo: {(isEnabled ? "ON" : "OFF")}");

		// TODO: Apply to audio engine
	}

	private async void OnVoicePresetClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[SettingsPage] Voice preset clicked");

		string[] presets = { "Normal", "Deep", "High", "Robot", "Echo", "Chipmunk", "Underwater" };

		string? selected = await DisplayActionSheet("Select Voice Preset", "Cancel", null, presets);

		if (selected != null && selected != "Cancel")
		{
			VoicePresetLabel.Text = selected;
			Preferences.Set(VoicePresetKey, selected);

			System.Diagnostics.Debug.WriteLine($"[SettingsPage] Voice preset changed: {selected}");

			// TODO: Apply to audio engine
		}
	}

	private void OnLimiterToggled(object? sender, EventArgs e)
	{
		bool isEnabled = LimiterSwitch.IsToggled;
		Preferences.Set(LimiterKey, isEnabled);

		System.Diagnostics.Debug.WriteLine($"[SettingsPage] Limiter: {(isEnabled ? "ON" : "OFF")}");

		// TODO: Apply to audio engine
	}

	private async void OnLatencyModeClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[SettingsPage] Latency mode clicked");

		string[] modes = { "Low Latency", "Balanced", "Stable" };

		string? selected = await DisplayActionSheet("Select Latency Mode", "Cancel", null, modes);

		if (selected != null && selected != "Cancel")
		{
			LatencyModeLabel.Text = selected;
			Preferences.Set(LatencyModeKey, selected);

			System.Diagnostics.Debug.WriteLine($"[SettingsPage] Latency mode changed: {selected}");

			// TODO: Apply to audio engine
		}
	}

	#endregion

	#region Bluetooth & Devices

	private async void OnPreferredDeviceClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[SettingsPage] Preferred device clicked");

		// TODO: Show list of paired devices
		await DisplayAlert("Preferred Device",
			"Device selection coming soon!\n\nThis will show your paired Bluetooth devices.",
			"OK");
	}

	private void OnAutoReconnectToggled(object? sender, EventArgs e)
	{
		bool isEnabled = AutoReconnectSwitch.IsToggled;
		Preferences.Set(AutoReconnectKey, isEnabled);

		System.Diagnostics.Debug.WriteLine($"[SettingsPage] Auto reconnect: {(isEnabled ? "ON" : "OFF")}");
	}

	private void OnScanOnLaunchToggled(object? sender, EventArgs e)
	{
		bool isEnabled = ScanOnLaunchSwitch.IsToggled;
		Preferences.Set(ScanOnLaunchKey, isEnabled);

		System.Diagnostics.Debug.WriteLine($"[SettingsPage] Scan on launch: {(isEnabled ? "ON" : "OFF")}");
	}

	private async void OnOpenBluetoothSettingsClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[SettingsPage] Open Bluetooth settings clicked");

		try
		{
#if ANDROID
			// Open Android Bluetooth settings
			var intent = new Android.Content.Intent(Android.Provider.Settings.ActionBluetoothSettings);
			intent.SetFlags(Android.Content.ActivityFlags.NewTask);
			Android.App.Application.Context.StartActivity(intent);
#else
			await DisplayAlert("Bluetooth Settings",
				"Please open Bluetooth settings from your device's Settings app.",
				"OK");
#endif
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error opening Bluetooth settings: {ex.Message}");
			await DisplayAlert("Error", "Could not open Bluetooth settings.", "OK");
		}
	}

	#endregion

	#region App Preferences

	private async void OnThemeClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[SettingsPage] Theme clicked");

		string[] themes = { "Dark", "Light", "Auto" };

		string? selected = await DisplayActionSheet("Select Theme", "Cancel", null, themes);

		if (selected != null && selected != "Cancel")
		{
			ThemeLabel.Text = selected;
			Preferences.Set(ThemeKey, selected);

			System.Diagnostics.Debug.WriteLine($"[SettingsPage] Theme changed: {selected}");

			await DisplayAlert("Theme",
				$"Theme changed to {selected}.\n\nNote: Theme switching will be implemented in a future update.",
				"OK");
		}
	}

	private async void OnLanguageClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[SettingsPage] Language clicked");

		string[] languages = { "English", "Spanish", "French", "German", "Hebrew" };

		string? selected = await DisplayActionSheet("Select Language", "Cancel", null, languages);

		if (selected != null && selected != "Cancel")
		{
			LanguageLabel.Text = selected;
			Preferences.Set(LanguageKey, selected);

			System.Diagnostics.Debug.WriteLine($"[SettingsPage] Language changed: {selected}");

			await DisplayAlert("Language",
				$"Language changed to {selected}.\n\nNote: Localization will be implemented in a future update.",
				"OK");
		}
	}

	private void OnHapticsToggled(object? sender, EventArgs e)
	{
		bool isEnabled = HapticsSwitch.IsToggled;
		Preferences.Set(HapticsKey, isEnabled);

		System.Diagnostics.Debug.WriteLine($"[SettingsPage] Haptics: {(isEnabled ? "ON" : "OFF")}");

		// Trigger haptic feedback if enabled
		if (isEnabled)
		{
			try
			{
				HapticFeedback.Default.Perform(HapticFeedbackType.Click);
			}
			catch { }
		}
	}

	private void OnAnimationsToggled(object? sender, EventArgs e)
	{
		bool isEnabled = AnimationsSwitch.IsToggled;
		Preferences.Set(AnimationsKey, isEnabled);

		System.Diagnostics.Debug.WriteLine($"[SettingsPage] Animations: {(isEnabled ? "ON" : "OFF")}");
	}

	#endregion

	#region Sign Out

	private async void OnSignOutClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[SettingsPage] Sign out clicked");

		var confirmed = await DisplayAlert(
			"Sign Out",
			"Are you sure you want to sign out?",
			"Sign Out",
			"Cancel");

		if (confirmed)
		{
			await _authService.LogoutAsync();

			// Navigate to login page
			Application.Current!.MainPage = new NavigationPage(new LoginPage(_authService));

			System.Diagnostics.Debug.WriteLine("[SettingsPage] User signed out");
		}
	}

	#endregion
}
