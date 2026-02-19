using BluetoothMicrophoneApp.Services;
using BluetoothMicrophoneApp.UI;

namespace BluetoothMicrophoneApp;

public partial class MainPage : ContentPage
{
	private readonly IBluetoothService _bluetoothService;
	private readonly IAudioService _audioService;
	private readonly IConnectivityDiagnostics _diagnostics;

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

	public MainPage(IBluetoothService bluetoothService, IAudioService audioService, IConnectivityDiagnostics diagnostics)
	{
		InitializeComponent();

		_bluetoothService = bluetoothService;
		_audioService = audioService;
		_diagnostics = diagnostics;

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
					break;

				case UIState.DeviceSelected:
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					DeviceNameLabel.Text = _selectedDevice?.Name ?? "Unknown Device";
					DeviceStatusLabel.Text = "Ready to connect";
					DeviceStatusLabel.TextColor = Color.FromArgb("#8E8E93");
					ActionButtonsSection.IsVisible = true;
					PrimaryActionLabel.Text = "Connect";
					Grid.SetColumnSpan(PrimaryActionBorder, 2);
					break;

				case UIState.Connecting:
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					DeviceNameLabel.Text = _selectedDevice?.Name ?? "Unknown Device";
					DeviceStatusLabel.Text = "Connecting...";
					DeviceStatusLabel.TextColor = Color.FromArgb("#4A90E2");
					ActionButtonsSection.IsVisible = false;
					break;

				case UIState.Connected:
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					DeviceNameLabel.Text = _selectedDevice?.Name ?? "Unknown Device";
					DeviceStatusLabel.Text = "✓ Connected";
					DeviceStatusLabel.TextColor = Color.FromArgb("#4CAF50");
					AudioControlsSection.IsVisible = true;
					ActionButtonsSection.IsVisible = false;
					break;

				case UIState.Failed:
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					DeviceNameLabel.Text = _selectedDevice?.Name ?? "Unknown Device";
					DeviceStatusLabel.Text = "Connection failed";
					DeviceStatusLabel.TextColor = Color.FromArgb("#FF5252");

					MessageSection.IsVisible = true;
					MessageLabel.Text = $"Could not connect to {_selectedDevice?.Name}.";
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

			var devices = await _bluetoothService.ScanForDevicesAsync();
			_availableDevices = devices;

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
						"Device is paired in Settings",
						"Device is turned on and nearby"
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

			SetState(UIState.DeviceSelected);
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
			if (_currentState == UIState.Failed)
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
}
