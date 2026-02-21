using BluetoothMicrophoneApp.Services;
using BluetoothMicrophoneApp.UI;
using BluetoothMicrophoneApp.Pages;

namespace BluetoothMicrophoneApp;

public partial class MainPage : ContentPage
{
	private readonly IBluetoothService _bluetoothService;
	private readonly IAudioService _audioService;
	private readonly IConnectivityDiagnostics _diagnostics;
	private readonly IAuthService _authService;

	private List<BluetoothDevice> _availableDevices = new();
	private BluetoothDevice? _selectedDevice;

	private enum UIState
	{
		Initial,        // Just scan button
		DeviceList,     // Showing device list
		DeviceSelected, // Device selected, ready to connect
		Connecting,     // Connecting animation
		Connected,      // Successfully connected
		Failed          // Connection failed
	}

	private UIState _currentState = UIState.Initial;

	public MainPage(IBluetoothService bluetoothService, IAudioService audioService, IConnectivityDiagnostics diagnostics, IAuthService authService)
	{
		InitializeComponent();

		_bluetoothService = bluetoothService;
		_audioService = audioService;
		_diagnostics = diagnostics;
		_authService = authService;

		_bluetoothService.DeviceConnected += OnDeviceConnected;
		_bluetoothService.DeviceDisconnected += OnDeviceDisconnected;
		_audioService.StatusChanged += OnAudioStatusChanged;

		// Initialize DialogService with root grid
		DialogService.Initialize(RootGrid);

		RequestPermissions();

		// Set initial state
		SetState(UIState.Initial);
	}

	private async void RequestPermissions()
	{
		try
		{
			var bluetoothStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
			var micStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();

			if (bluetoothStatus != PermissionStatus.Granted)
			{
				await Permissions.RequestAsync<Permissions.Bluetooth>();
			}

			if (micStatus != PermissionStatus.Granted)
			{
				await Permissions.RequestAsync<Permissions.Microphone>();
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Permission error: {ex.Message}");
		}
	}

	private async Task<bool> CheckBluetoothPermissionsAsync()
	{
		try
		{
			// On Android 12+ (API 31+), we need BLUETOOTH_SCAN and BLUETOOTH_CONNECT
			// On older Android, we need BLUETOOTH and location permissions

			var bluetoothStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();

			if (bluetoothStatus != PermissionStatus.Granted)
			{
				System.Diagnostics.Debug.WriteLine("[Permissions] Requesting Bluetooth permissions...");
				bluetoothStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
			}

			if (bluetoothStatus != PermissionStatus.Granted)
			{
				System.Diagnostics.Debug.WriteLine("[Permissions] Bluetooth permission denied");
				return false;
			}

			System.Diagnostics.Debug.WriteLine("[Permissions] Bluetooth permissions granted");
			return true;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[Permissions] Error checking Bluetooth permissions: {ex.Message}");
			return false;
		}
	}

	// Set UI State
	private void SetState(UIState newState)
	{
		_currentState = newState;

		MainThread.BeginInvokeOnMainThread(() =>
		{
			// Hide everything first
			MainCard.IsVisible = false;
			DeviceListSection.IsVisible = false;
			DeviceInfoSection.IsVisible = false;
			AudioControlsSection.IsVisible = false;
			MessageSection.IsVisible = false;
			ActionButtonsSection.IsVisible = false;
			SecondaryActionBorder.IsVisible = false;

			switch (newState)
			{
				case UIState.Initial:
					// Just scan button visible (default)
					break;

				case UIState.DeviceList:
					DeviceListSection.IsVisible = true;
					// Clear selection to allow re-selecting the same device
					DeviceCollectionView.SelectedItem = null;
					break;

				case UIState.DeviceSelected:
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					RenameButton.IsVisible = false; // Hide rename button until connected
					DeviceNameLabel.Text = GetDeviceDisplayName(_selectedDevice);
					DeviceStatusLabel.Text = "Ready to connect";
					DeviceStatusLabel.TextColor = Color.FromArgb("#8E8E93");
					ActionButtonsSection.IsVisible = true;
					PrimaryActionLabel.Text = "Connect";
					SecondaryActionBorder.IsVisible = true;
					SecondaryActionLabel.Text = "Back";
					Grid.SetColumnSpan(PrimaryActionBorder, 2);
					break;

				case UIState.Connecting:
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					RenameButton.IsVisible = false;
					DeviceNameLabel.Text = GetDeviceDisplayName(_selectedDevice);
					DeviceStatusLabel.Text = "Connecting...";
					DeviceStatusLabel.TextColor = Color.FromArgb("#4A90E2");
					ActionButtonsSection.IsVisible = false;
					break;

				case UIState.Connected:
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					RenameButton.IsVisible = true; // Show rename button when connected
					DeviceNameLabel.Text = GetDeviceDisplayName(_selectedDevice);
					DeviceStatusLabel.Text = "✓ Connected";
					DeviceStatusLabel.TextColor = Color.FromArgb("#4CAF50");
					AudioControlsSection.IsVisible = true;
					ActionButtonsSection.IsVisible = false;
					break;

				case UIState.Failed:
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					RenameButton.IsVisible = false;
					DeviceNameLabel.Text = GetDeviceDisplayName(_selectedDevice);
					DeviceStatusLabel.Text = "Connection failed";
					DeviceStatusLabel.TextColor = Color.FromArgb("#FF5252");

					MessageSection.IsVisible = true;
					MessageLabel.Text = $"Could not connect to {GetDeviceDisplayName(_selectedDevice)}.";
					ShowFailureReasons();

					ActionButtonsSection.IsVisible = true;
					PrimaryActionLabel.Text = "Try Again";
					SecondaryActionBorder.IsVisible = true;
					SecondaryActionLabel.Text = "Troubleshoot";
					Grid.SetColumnSpan(PrimaryActionBorder, 2);
					break;
			}
		});
	}

	private void ShowFailureReasons()
	{
		BulletPointsContainer.Clear();
		BulletPointsContainer.IsVisible = true;

		var reasons = new List<string>
		{
			"Device is not paired in Settings",
			"Device is turned off or out of range",
			"Device is connected to another phone",
			"Bluetooth is disabled"
		};

		foreach (var reason in reasons)
		{
			var border = new Border
			{
				BackgroundColor = Color.FromArgb("#1E1E38"),
				Stroke = Color.FromArgb("#4A90E2"),
				StrokeThickness = 1,
				Padding = 12,
				Margin = new Thickness(0, 0, 0, 8),
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }
			};

			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
				},
				ColumnSpacing = 10
			};

			var icon = new Label
			{
				Text = "⚠️",
				FontSize = 16,
				VerticalOptions = LayoutOptions.Start
			};

			var text = new Label
			{
				Text = reason,
				TextColor = Color.FromArgb("#CCCCCC"),
				FontSize = 13,
				LineHeight = 1.3
			};

			Grid.SetColumn(icon, 0);
			Grid.SetColumn(text, 1);

			grid.Add(icon);
			grid.Add(text);
			border.Content = grid;

			BulletPointsContainer.Add(border);
		}
	}

	// Event Handlers
	private async void OnScanClicked(object? sender, EventArgs e)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("[MainPage] Scan button clicked");

			// Show loading
			ScanButton.IsEnabled = false;
			ScanningIndicator.IsRunning = true;
			ScanningIndicator.IsVisible = true;

			// Check and request Bluetooth permissions
			var hasPermissions = await CheckBluetoothPermissionsAsync();
			if (!hasPermissions)
			{
				await DialogService.ShowErrorAsync(
					"Permissions Required",
					"Bluetooth permissions are required to scan for devices.",
					new List<string>
					{
						"Grant Bluetooth permissions in Settings",
						"Restart the app and try again"
					});
				return;
			}

			// Check if Bluetooth is enabled
			if (!_bluetoothService.IsBluetoothEnabled())
			{
				System.Diagnostics.Debug.WriteLine("[MainPage] Bluetooth is OFF, asking user for permission to enable");

				var enableBluetooth = await DisplayAlert(
					"Bluetooth is Off",
					"Bluetooth is currently turned off. Would you like to turn it on?",
					"Turn On",
					"Cancel");

				if (enableBluetooth)
				{
					System.Diagnostics.Debug.WriteLine("[MainPage] User approved, enabling Bluetooth...");

					bool success = await _bluetoothService.RequestEnableBluetoothAsync();

					if (!success)
					{
						await DialogService.ShowErrorAsync(
							"Bluetooth Error",
							"Failed to enable Bluetooth. Please enable it manually from Settings.",
							new List<string>
							{
								"Go to Settings → Bluetooth",
								"Turn on Bluetooth",
								"Return to the app and try again"
							});
						return;
					}

					System.Diagnostics.Debug.WriteLine("[MainPage] Bluetooth enabled successfully");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("[MainPage] User declined to enable Bluetooth");
					await DialogService.ShowInfoAsync(
						"Bluetooth Required",
						"Bluetooth must be enabled to scan for devices. Please enable it manually from Settings.",
						new List<string>
						{
							"Go to Settings → Bluetooth",
							"Turn on Bluetooth",
							"Return to the app and try again"
						});
					return;
				}
			}

			var devices = await _bluetoothService.ScanForDevicesAsync();
			_availableDevices = devices;

			System.Diagnostics.Debug.WriteLine($"[MainPage] ===== Applying Custom Names to {_availableDevices.Count} Devices =====");

			// Apply custom names to devices
			foreach (var device in _availableDevices)
			{
				System.Diagnostics.Debug.WriteLine($"[MainPage] Processing device:");
				System.Diagnostics.Debug.WriteLine($"  → Original Name: '{device.Name}'");
				System.Diagnostics.Debug.WriteLine($"  → Device Address: '{device.Address}'");

				var displayName = DeviceNameManager.GetDisplayName(device.Address, device.Name);

				System.Diagnostics.Debug.WriteLine($"  → Display Name Returned: '{displayName}'");

				device.Name = displayName;

				System.Diagnostics.Debug.WriteLine($"  → Device.Name Set To: '{device.Name}'");
			}

			System.Diagnostics.Debug.WriteLine($"[MainPage] ===== Custom Names Applied =====");

			if (_availableDevices.Any())
			{
				DeviceCollectionView.ItemsSource = _availableDevices;
				SetState(UIState.DeviceList);
			}
			else
			{
				await DialogService.ShowWarningAsync(
					"No Devices Found",
					"No Bluetooth devices found nearby.",
					new List<string>
					{
						"Make sure Bluetooth is enabled",
						"Turn on your Bluetooth device",
						"Ensure device is in pairing mode",
						"Device should be within range (10m)"
					});
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Scan error: {ex.Message}");
			await DialogService.ShowErrorAsync("Scan Error", $"Failed to scan for devices.\n\n{ex.Message}");
		}
		finally
		{
			ScanButton.IsEnabled = true;
			ScanningIndicator.IsRunning = false;
			ScanningIndicator.IsVisible = false;
		}
	}

	private void OnDeviceSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.Count > 0)
		{
			_selectedDevice = e.CurrentSelection[0] as BluetoothDevice;
			System.Diagnostics.Debug.WriteLine($"[MainPage] Device selected: {_selectedDevice?.Name}");

			// Check if we're already connected to this device
			if (_bluetoothService.IsConnected &&
			    _bluetoothService.ConnectedDevice != null &&
			    _selectedDevice != null &&
			    _bluetoothService.ConnectedDevice.Address == _selectedDevice.Address)
			{
				// Already connected to this device - go directly to engagement view
				System.Diagnostics.Debug.WriteLine($"[MainPage] Already connected to {_selectedDevice.Name}, going to engagement view");
				SetState(UIState.Connected);
			}
			else
			{
				// Not connected or different device - show connect button
				System.Diagnostics.Debug.WriteLine($"[MainPage] Not connected to this device, showing connect button");
				SetState(UIState.DeviceSelected);
			}
		}
	}

	private async void OnPrimaryActionClicked(object? sender, EventArgs e)
	{
		try
		{
			if (_currentState == UIState.DeviceSelected)
			{
				// Connect action
				await ConnectToSelectedDevice();
			}
			else if (_currentState == UIState.Failed)
			{
				// Retry action
				await ConnectToSelectedDevice();
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Primary action error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"An error occurred.\n\n{ex.Message}");
		}
	}

	private async void OnSecondaryActionClicked(object? sender, EventArgs e)
	{
		try
		{
			if (_currentState == UIState.DeviceSelected)
			{
				// Back to device list - clear selection
				_selectedDevice = null;
				SetState(UIState.DeviceList);
			}
			else if (_currentState == UIState.Failed)
			{
				// Troubleshoot action
				var report = await _diagnostics.PerformDiagnosticsAsync();
				await DialogService.ShowInfoAsync("Connectivity Diagnostics", report.ToString());
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Secondary action error: {ex.Message}");
		}
	}

	private async Task ConnectToSelectedDevice()
	{
		if (_selectedDevice == null) return;

		try
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Connecting to {_selectedDevice.Name}");

			SetState(UIState.Connecting);

			var success = await _bluetoothService.ConnectToDeviceAsync(_selectedDevice);

			if (success)
			{
				SetState(UIState.Connected);

				// Show success message after animation
				await Task.Delay(1000);
				await DialogService.ShowConnectedAsync(_selectedDevice.Name);
			}
			else
			{
				SetState(UIState.Failed);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Connection error: {ex.Message}");
			SetState(UIState.Failed);
		}
	}

	private void OnDeviceConnected(object? sender, BluetoothDevice device)
	{
		System.Diagnostics.Debug.WriteLine($"[MainPage] Device connected event: {device.Name}");
	}

	private void OnDeviceDisconnected(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[MainPage] Device disconnected event");
		SetState(UIState.Initial);
	}

	private void OnAudioStatusChanged(object? sender, string status)
	{
		System.Diagnostics.Debug.WriteLine($"[MainPage] Audio status: {status}");
	}

	private async void OnStartAudioClicked(object? sender, EventArgs e)
	{
		try
		{
			await _audioService.StartAudioRoutingAsync();
			StartAudioBtn.IsVisible = false;
			StopAudioBtn.IsVisible = true;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Start audio error: {ex.Message}");
			await DialogService.ShowErrorAsync("Audio Error", $"Failed to start audio.\\n\\n{ex.Message}");
		}
	}

	private async void OnStopAudioClicked(object? sender, EventArgs e)
	{
		try
		{
			await _audioService.StopAudioRoutingAsync();
			StartAudioBtn.IsVisible = true;
			StopAudioBtn.IsVisible = false;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Stop audio error: {ex.Message}");
		}
	}

	private void OnVolumeChanged(object? sender, ValueChangedEventArgs e)
	{
		var volume = (int)e.NewValue;
		VolumeLabel.Text = $"{volume}%";
		_audioService.SetVolume(volume / 100.0);
	}

	private async void OnVisualizerClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new VisualizerPage(_audioService));
	}

	private async void OnEffectsClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new EffectsPage(_audioService));
	}

	private async void OnDisconnectClicked(object? sender, EventArgs e)
	{
		try
		{
			await _audioService.StopAudioRoutingAsync();
			await _bluetoothService.DisconnectAsync();
			SetState(UIState.Initial);
			_selectedDevice = null;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Disconnect error: {ex.Message}");
		}
	}

	private async void OnRenameDeviceClicked(object? sender, EventArgs e)
	{
		if (_selectedDevice == null) return;

		try
		{
			var currentName = DeviceNameManager.GetDisplayName(_selectedDevice.Address, _selectedDevice.Name);
			var result = await DisplayPromptAsync(
				"Rename Device",
				"Enter a custom name for this device:",
				initialValue: currentName,
				maxLength: 30,
				keyboard: Keyboard.Text);

			if (!string.IsNullOrWhiteSpace(result))
			{
				DeviceNameManager.SetCustomName(_selectedDevice.Address, result);
				DeviceNameLabel.Text = result;
				System.Diagnostics.Debug.WriteLine($"[MainPage] Device renamed: {_selectedDevice.Name} → {result}");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Rename error: {ex.Message}");
		}
	}

	private async void OnEditDeviceNameClicked(object? sender, EventArgs e)
	{
		try
		{
			// Get the device from the tapped element
			if (sender is Border border && border.BindingContext is BluetoothDevice device)
			{
				System.Diagnostics.Debug.WriteLine($"[MainPage] ===== OnEditDeviceNameClicked =====");
				System.Diagnostics.Debug.WriteLine($"[MainPage] Device Address: '{device.Address}'");
				System.Diagnostics.Debug.WriteLine($"[MainPage] Device Current Name: '{device.Name}'");

				var currentName = DeviceNameManager.GetDisplayName(device.Address, device.Name);

				System.Diagnostics.Debug.WriteLine($"[MainPage] Current Display Name: '{currentName}'");

				var result = await DisplayPromptAsync(
					"Rename Device",
					"Enter a custom name for this device:",
					initialValue: currentName,
					maxLength: 30,
					keyboard: Keyboard.Text);

				System.Diagnostics.Debug.WriteLine($"[MainPage] User entered: '{result}'");

				if (!string.IsNullOrWhiteSpace(result))
				{
					System.Diagnostics.Debug.WriteLine($"[MainPage] Calling SetCustomName...");
					DeviceNameManager.SetCustomName(device.Address, result);

					System.Diagnostics.Debug.WriteLine($"[MainPage] SetCustomName completed");

					// Update the device name in the list
					device.Name = result;

					System.Diagnostics.Debug.WriteLine($"[MainPage] Updated device.Name to: '{device.Name}'");

					// Refresh the collection view to show the new name
					DeviceCollectionView.ItemsSource = null;
					DeviceCollectionView.ItemsSource = _availableDevices;

					System.Diagnostics.Debug.WriteLine($"[MainPage] Collection view refreshed");
					System.Diagnostics.Debug.WriteLine($"[MainPage] Device renamed: {device.Address} → {result}");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"[MainPage] User canceled or entered empty name");
				}

				System.Diagnostics.Debug.WriteLine($"[MainPage] ===== OnEditDeviceNameClicked END =====");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Edit device name error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"Failed to rename device.\n\n{ex.Message}");
		}
	}

	private async void OnDeleteDeviceClicked(object? sender, EventArgs e)
	{
		try
		{
			// Get the device from the tapped element
			if (sender is Border border && border.BindingContext is BluetoothDevice device)
			{
				var deviceName = DeviceNameManager.GetDisplayName(device.Address, device.Name);
				var confirmed = await DisplayAlert(
					"Forget Device",
					$"Are you sure you want to forget \"{deviceName}\"?\n\nThis will:\n• Remove custom name\n• Unpair the device from your phone",
					"Forget",
					"Cancel");

				if (confirmed)
				{
					System.Diagnostics.Debug.WriteLine($"[MainPage] Forgetting device: {deviceName}");

					// Remove custom name
					DeviceNameManager.RemoveCustomName(device.Address);

					// Unpair the device (Android)
					await _bluetoothService.UnpairDeviceAsync(device);

					// Refresh device list
					_availableDevices.Remove(device);
					DeviceCollectionView.ItemsSource = null;
					DeviceCollectionView.ItemsSource = _availableDevices;

					System.Diagnostics.Debug.WriteLine($"[MainPage] Device forgotten: {deviceName}");
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Delete device error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"Failed to forget device.\n\n{ex.Message}");
		}
	}

	// Helper method to get device display name (custom or original)
	private string GetDeviceDisplayName(BluetoothDevice? device)
	{
		if (device == null) return "Unknown Device";
		return DeviceNameManager.GetDisplayName(device.Address, device.Name);
	}

	private async void OnSettingsClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[MainPage] Settings button clicked");

		try
		{
			var settingsPage = new SettingsPage(_authService);
			await Navigation.PushAsync(settingsPage);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Settings navigation error: {ex.Message}");
		}
	}
}
