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

	// Animation cancellation tokens
	private CancellationTokenSource? _magnifyingGlassAnimationCts;
	private CancellationTokenSource? _dotsAnimationCts;

	// Scanning cancellation token
	private CancellationTokenSource? _scanningCts;

	// Audio operation flag to prevent race conditions
	private bool _isAudioOperationInProgress = false;

	// Bluetooth monitoring
	private System.Timers.Timer? _bluetoothMonitor;
	private bool _wasBluetoothEnabled = true;
	private bool _isShowingBluetoothDialog = false; // Prevent multiple dialogs

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

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// Auto-scan for devices when landing on home page
		System.Diagnostics.Debug.WriteLine("[MainPage] OnAppearing - Starting auto-scan");

		// Small delay to ensure page is fully loaded
		await Task.Delay(500);

		// Start monitoring Bluetooth status
		StartBluetoothMonitoring();

		// Only auto-scan if we're in Initial state (not already connected)
		if (_currentState == UIState.Initial && !_bluetoothService.IsConnected)
		{
			await StartScanning();
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

		// Stop monitoring when page is not visible
		StopBluetoothMonitoring();
	}

	private void StartBluetoothMonitoring()
	{
		StopBluetoothMonitoring(); // Stop any existing monitor

		_bluetoothMonitor = new System.Timers.Timer(2000); // Check every 2 seconds
		_bluetoothMonitor.Elapsed += async (s, e) =>
		{
			try
			{
				var isEnabled = _bluetoothService.IsBluetoothEnabled();

				// Detect when Bluetooth state changes
				if (_wasBluetoothEnabled && !isEnabled)
				{
					// Bluetooth was just disabled
					System.Diagnostics.Debug.WriteLine("[MainPage] 🔴 Bluetooth was disabled");
					await HandleBluetoothDisabled();
				}
				else if (!_wasBluetoothEnabled && isEnabled)
				{
					// Bluetooth was just enabled
					System.Diagnostics.Debug.WriteLine("[MainPage] 🟢 Bluetooth was enabled");
					await HandleBluetoothEnabled();
				}

				_wasBluetoothEnabled = isEnabled;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[MainPage] Bluetooth monitor error: {ex.Message}");
			}
		};
		_bluetoothMonitor.Start();

		System.Diagnostics.Debug.WriteLine("[MainPage] Bluetooth monitoring started");
	}

	private void StopBluetoothMonitoring()
	{
		if (_bluetoothMonitor != null)
		{
			_bluetoothMonitor.Stop();
			_bluetoothMonitor.Dispose();
			_bluetoothMonitor = null;
			System.Diagnostics.Debug.WriteLine("[MainPage] Bluetooth monitoring stopped");
		}
	}

	private async Task HandleBluetoothDisabled()
	{
		// Prevent showing multiple dialogs
		if (_isShowingBluetoothDialog)
		{
			System.Diagnostics.Debug.WriteLine("[MainPage] Bluetooth disabled dialog already showing, skipping...");
			return;
		}

		_isShowingBluetoothDialog = true;

		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			try
			{
				// Clear device lists
				_availableDevices.Clear();
				AvailableDevicesView.ItemsSource = null;
				// TODO: RecentlyConnectedView was removed
				// RecentlyConnectedView.ItemsSource = null;

				// Hide device list sections
				AvailableDevicesSection.IsVisible = false;
				// TODO: RecentlyConnectedSection was removed
				// RecentlyConnectedSection.IsVisible = false;

				// If we were connected, disconnect first
				if (_bluetoothService.IsConnected)
				{
					await _audioService.StopAudioRoutingAsync();
					await _bluetoothService.DisconnectAsync();
				}

				// Return to initial state
				SetState(UIState.Initial);
				_selectedDevice = null;

				// Show message to enable Bluetooth (only once)
				await DialogService.ShowWarningAsync(
					"Bluetooth Disabled",
					"Bluetooth has been turned off. Please enable it to scan for devices.",
					new List<string>
					{
						"Go to Settings → Bluetooth",
						"Turn on Bluetooth",
						"Return to the app to scan"
					});
			}
			finally
			{
				_isShowingBluetoothDialog = false;
			}
		});
	}

	private async Task HandleBluetoothEnabled()
	{
		// Reset dialog flag when Bluetooth is re-enabled
		_isShowingBluetoothDialog = false;

		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			// Only rescan if we're in initial state (not already connected or in the middle of something)
			if (_currentState == UIState.Initial)
			{
				System.Diagnostics.Debug.WriteLine("[MainPage] Bluetooth enabled - starting automatic scan");
				await Task.Delay(500); // Brief delay to let Bluetooth settle
				await StartScanning();
			}
		});
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
			System.Diagnostics.Debug.WriteLine("[Permissions] ========================================");
			System.Diagnostics.Debug.WriteLine("[Permissions] Checking Bluetooth permissions...");

#if ANDROID
			// CRITICAL: Use direct Android permission check (MAUI abstraction is unreliable)
			System.Diagnostics.Debug.WriteLine("[Permissions] Using DIRECT Android runtime permission check...");
			bool hasAndroidPermissions = Platforms.Android.Services.AndroidBluetoothPermissions.HasBluetoothPermissions();
			System.Diagnostics.Debug.WriteLine($"[Permissions] Direct Android check result: {hasAndroidPermissions}");

			if (hasAndroidPermissions)
			{
				System.Diagnostics.Debug.WriteLine("[Permissions] ✅ Android runtime permissions GRANTED");
				System.Diagnostics.Debug.WriteLine("[Permissions] ========================================");
				return true;
			}

			// Permissions not granted - try requesting
			System.Diagnostics.Debug.WriteLine("[Permissions] ⚠️ Android runtime permissions NOT GRANTED");
			System.Diagnostics.Debug.WriteLine("[Permissions] Attempting to request permissions...");

			Platforms.Android.Services.AndroidBluetoothPermissions.RequestBluetoothPermissions();

			// Wait a bit for user to respond to permission dialog
			await Task.Delay(2000);

			// Check again
			hasAndroidPermissions = Platforms.Android.Services.AndroidBluetoothPermissions.HasBluetoothPermissions();
			System.Diagnostics.Debug.WriteLine($"[Permissions] After request, Android check result: {hasAndroidPermissions}");

			if (hasAndroidPermissions)
			{
				System.Diagnostics.Debug.WriteLine("[Permissions] ✅ Permissions granted by user");
				System.Diagnostics.Debug.WriteLine("[Permissions] ========================================");
				return true;
			}

			// Still not granted - user must enable manually in Settings
			System.Diagnostics.Debug.WriteLine("[Permissions] ❌ BLUETOOTH PERMISSIONS DENIED");
			System.Diagnostics.Debug.WriteLine("[Permissions] User must enable manually in Android Settings");

			await DialogService.ShowErrorAsync(
				"Bluetooth Permission Required",
				"E-z MicLink needs Bluetooth permission to find your devices.",
				new List<string>
				{
					"1. Go to Settings → Apps → E-z MicLink",
					"2. Tap 'Permissions'",
					"3. Find 'Nearby devices' (or 'Bluetooth')",
					"4. Set to 'Allow' (NOT 'Ask every time')",
					"5. Return to the app and scan again"
				});

			System.Diagnostics.Debug.WriteLine("[Permissions] ========================================");
			return false;
#else
			// Non-Android platforms: use MAUI abstraction
			var bluetoothStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
			System.Diagnostics.Debug.WriteLine($"[Permissions] MAUI status: {bluetoothStatus}");

			if (bluetoothStatus == PermissionStatus.Granted)
			{
				System.Diagnostics.Debug.WriteLine("[Permissions] ✅ Bluetooth permissions ALREADY GRANTED");
				System.Diagnostics.Debug.WriteLine("[Permissions] ========================================");
				return true;
			}

			// Permission not granted, need to request
			if (bluetoothStatus == PermissionStatus.Denied)
			{
				System.Diagnostics.Debug.WriteLine("[Permissions] ⚠️ Permission was DENIED previously");
				System.Diagnostics.Debug.WriteLine("[Permissions] User must enable in Settings manually");

				await DialogService.ShowErrorAsync(
					"Bluetooth Permission Required",
					"E-z MicLink needs Bluetooth permission to scan for and connect to devices.",
					new List<string>
					{
						"Go to Settings → Apps → E-z MicLink",
						"Tap 'Permissions'",
						"Enable 'Nearby devices' or 'Bluetooth'",
						"Return to the app and try again"
					});

				System.Diagnostics.Debug.WriteLine("[Permissions] ========================================");
				return false;
			}

			// Request permission
			System.Diagnostics.Debug.WriteLine("[Permissions] Requesting Bluetooth permissions via MAUI...");
			bluetoothStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
			System.Diagnostics.Debug.WriteLine($"[Permissions] After MAUI request: {bluetoothStatus}");

			if (bluetoothStatus != PermissionStatus.Granted)
			{
				System.Diagnostics.Debug.WriteLine("[Permissions] ❌ Bluetooth permission DENIED by user");

				await DialogService.ShowErrorAsync(
					"Permission Denied",
					"Bluetooth permission is required to use this app.",
					new List<string>
					{
						"Without this permission, the app cannot:",
						"• Scan for Bluetooth devices",
						"• Connect to your headphones/speakers",
						"• Use Bluetooth microphones"
					});

				System.Diagnostics.Debug.WriteLine("[Permissions] ========================================");
				return false;
			}

			System.Diagnostics.Debug.WriteLine("[Permissions] ✅ Bluetooth permissions GRANTED via MAUI");
			System.Diagnostics.Debug.WriteLine("[Permissions] ========================================");
			return true;
#endif
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[Permissions] ❌ ERROR checking Bluetooth permissions: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[Permissions] Stack trace: {ex.StackTrace}");
			System.Diagnostics.Debug.WriteLine("[Permissions] ========================================");

			await DialogService.ShowErrorAsync(
				"Permission Error",
				$"Error checking Bluetooth permissions: {ex.Message}",
				new List<string>
				{
					"Try restarting the app",
					"Check Android version (requires Android 5.0+)",
					"Contact support if issue persists"
				});

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
			// TODO: HeaderSection was removed
			// HeaderSection.IsVisible = false;
			ScanButton.IsVisible = false;
			MainCard.IsVisible = false;
			AvailableDevicesSection.IsVisible = false;
			// TODO: RecentlyConnectedSection was removed
			// RecentlyConnectedSection.IsVisible = false;
			DeviceInfoSection.IsVisible = false;
			AudioControlsSection.IsVisible = false;
			MessageSection.IsVisible = false;
			ActionButtonsSection.IsVisible = false;
			SecondaryActionBorder.IsVisible = false;
			BackButtonSection.IsVisible = false;

			switch (newState)
			{
				case UIState.Initial:
					// Just header and scan button visible (default)
					// TODO: HeaderSection was removed
					// HeaderSection.IsVisible = true;
					ScanButton.IsVisible = true;
					break;

				case UIState.DeviceList:
					// Show header, scan button and device list sections
					// TODO: HeaderSection was removed
					// HeaderSection.IsVisible = true;
					ScanButton.IsVisible = true;
					// Show sections based on what's populated (handled in StartScanning)
					// Clear selections to allow re-selecting the same device
					AvailableDevicesView.SelectedItem = null;
					AvailableDevicesView.SelectedItem = null;
					break;

				case UIState.DeviceSelected:
					// Hide header and scan button, show card
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					RenameButton.IsVisible = false; // Hide rename button until connected
					DeviceNameLabel.Text = GetDeviceDisplayName(_selectedDevice);
					DeviceStatusLabel.Text = "Ready to connect";
					DeviceStatusLabel.TextColor = Colors.White;
					DeviceStatusLabel.Opacity = 0.6;
					ActionButtonsSection.IsVisible = true;
					PrimaryActionLabel.Text = "Connect";
					SecondaryActionBorder.IsVisible = true;
					SecondaryActionLabel.Text = "Back";
					Grid.SetColumnSpan(PrimaryActionBorder, 2);
					break;

				case UIState.Connecting:
					// Hide header and scan button, show card
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					RenameButton.IsVisible = false;
					DeviceNameLabel.Text = GetDeviceDisplayName(_selectedDevice);
					DeviceStatusLabel.Text = "Connecting...";
					DeviceStatusLabel.TextColor = Color.FromArgb("#00D2FF");
					DeviceStatusLabel.Opacity = 1.0;
					ActionButtonsSection.IsVisible = false;
					break;

				case UIState.Connected:
					// Hide header and scan button completely, show ONLY the connected card
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					RenameButton.IsVisible = true; // Show rename button when connected
					DeviceNameLabel.Text = GetDeviceDisplayName(_selectedDevice);
					DeviceStatusLabel.Text = "✓ Connected";
					DeviceStatusLabel.TextColor = Color.FromArgb("#4CAF50");
					DeviceStatusLabel.Opacity = 1.0;
					AudioControlsSection.IsVisible = true;
					ActionButtonsSection.IsVisible = false;
					
					BackButtonSection.IsVisible = true; // Show back button to return to device list
					break;

				case UIState.Failed:
					// Hide header and scan button, show card
					MainCard.IsVisible = true;
					DeviceInfoSection.IsVisible = true;
					RenameButton.IsVisible = false;
					DeviceNameLabel.Text = GetDeviceDisplayName(_selectedDevice);
					DeviceStatusLabel.Text = "Connection failed";
					DeviceStatusLabel.TextColor = Color.FromArgb("#FB7185");
					DeviceStatusLabel.Opacity = 1.0;

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
				Background = new SolidColorBrush(Color.FromRgba(255, 255, 255, 0.05)),
				Stroke = Color.FromArgb("#00D2FF"),
				StrokeThickness = 1,
				Padding = 16,
				Margin = new Thickness(0, 0, 0, 8),
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 }
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
				TextColor = Colors.White,
				Opacity = 0.6,
				FontFamily = "Inter",
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
	private async void OnDebugPermissionsClicked(object? sender, EventArgs e)
	{
		try
		{
			var messages = new List<string>();

#if ANDROID
			System.Diagnostics.Debug.WriteLine("[Debug] Checking Android permissions...");

			var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
			if (context == null)
			{
				messages.Add("❌ Cannot get Android context");
			}
			else
			{
				messages.Add($"✅ Android API: {Android.OS.Build.VERSION.SdkInt}");

				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
				{
					// Check Android 12+ permissions
					var connectPerm = context.CheckSelfPermission(Android.Manifest.Permission.BluetoothConnect);
					var scanPerm = context.CheckSelfPermission(Android.Manifest.Permission.BluetoothScan);

					messages.Add($"BLUETOOTH_CONNECT: {connectPerm}");
					messages.Add($"BLUETOOTH_SCAN: {scanPerm}");

					if (connectPerm != Android.Content.PM.Permission.Granted)
					{
						messages.Add("❌ BLUETOOTH_CONNECT is DENIED!");
					}
					if (scanPerm != Android.Content.PM.Permission.Granted)
					{
						messages.Add("❌ BLUETOOTH_SCAN is DENIED!");
					}
				}
				else
				{
					messages.Add("Android 11 or lower - checking legacy permissions");
					var btPerm = context.CheckSelfPermission(Android.Manifest.Permission.Bluetooth);
					var btAdminPerm = context.CheckSelfPermission(Android.Manifest.Permission.BluetoothAdmin);
					var locPerm = context.CheckSelfPermission(Android.Manifest.Permission.AccessFineLocation);

					messages.Add($"BLUETOOTH: {btPerm}");
					messages.Add($"BLUETOOTH_ADMIN: {btAdminPerm}");
					messages.Add($"LOCATION: {locPerm}");
				}

				// Check Bluetooth adapter
				var adapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
				if (adapter == null)
				{
					messages.Add("❌ BluetoothAdapter is NULL!");
				}
				else
				{
					messages.Add($"✅ Bluetooth enabled: {adapter.IsEnabled}");

					try
					{
						var bonded = adapter.BondedDevices;
						if (bonded == null)
						{
							messages.Add("❌ BondedDevices returned NULL!");
							messages.Add("This means permission is denied");
						}
						else
						{
							messages.Add($"✅ BondedDevices count: {bonded.Count}");
							foreach (var device in bonded)
							{
								messages.Add($"  → {device.Name} ({device.Address})");
							}
						}
					}
					catch (Java.Lang.SecurityException secEx)
					{
						messages.Add($"❌ SecurityException: {secEx.Message}");
						messages.Add("BLUETOOTH_CONNECT permission is missing!");
					}
				}
			}
#else
			messages.Add("Not running on Android");
#endif

			await DialogService.ShowInfoAsync(
				"Permission Debug Info",
				"Current Bluetooth permission status:",
				messages);
		}
		catch (Exception ex)
		{
			await DialogService.ShowErrorAsync("Debug Error", $"Error: {ex.Message}");
		}
	}

	private async void OnScanClicked(object? sender, EventArgs e)
	{
		await StartScanning();
	}

	private void OnStopScanClicked(object? sender, EventArgs e)
	{
		System.Diagnostics.Debug.WriteLine("[MainPage] Stop Scanning button clicked");

		// Cancel the scanning operation
		_scanningCts?.Cancel();

		// Hide the stop button and re-enable scan button immediately
		MainThread.BeginInvokeOnMainThread(() =>
		{
			// TODO: StopScanButton was removed
			// StopScanButton.IsVisible = false;
			ScanButton.IsEnabled = true;
		});

		// Stop animations immediately
		_magnifyingGlassAnimationCts?.Cancel();
		_dotsAnimationCts?.Cancel();

		System.Diagnostics.Debug.WriteLine("[MainPage] Scan cancellation requested");
	}

	private async Task StartScanning()
	{
		// Cancel any existing scan
		_scanningCts?.Cancel();
		_scanningCts?.Dispose();
		_scanningCts = new CancellationTokenSource();

		try
		{
			System.Diagnostics.Debug.WriteLine("[MainPage] ==========================================");
			System.Diagnostics.Debug.WriteLine("[MainPage] ========== SCAN START ==========");
			System.Diagnostics.Debug.WriteLine("[MainPage] ==========================================");

			// Show loading and Stop Scanning button
			ScanButton.IsEnabled = false;
			// TODO: StopScanButton was removed
			// StopScanButton.IsVisible = true;

			// Start scanning animations
			StartScanningAnimations();

			// Check and request Bluetooth permissions
			System.Diagnostics.Debug.WriteLine("[MainPage] ========== PERMISSION CHECK START ==========");
			var hasPermissions = await CheckBluetoothPermissionsAsync();
			System.Diagnostics.Debug.WriteLine($"[MainPage] Permission check result: {hasPermissions}");
			System.Diagnostics.Debug.WriteLine("[MainPage] ========== PERMISSION CHECK END ==========");

			if (!hasPermissions)
			{
				System.Diagnostics.Debug.WriteLine("[MainPage] ❌ SCAN ABORTED: No Bluetooth permissions");
				System.Diagnostics.Debug.WriteLine("[MainPage] ==========================================");
				return;
			}

			// Check if scanning was cancelled
			if (_scanningCts?.Token.IsCancellationRequested == true)
			{
				System.Diagnostics.Debug.WriteLine("[MainPage] ❌ SCAN CANCELLED by user");
				return;
			}

			System.Diagnostics.Debug.WriteLine("[MainPage] ✅ Permissions granted, proceeding with scan");

			// Check if Bluetooth is enabled
			if (!_bluetoothService.IsBluetoothEnabled())
			{
				System.Diagnostics.Debug.WriteLine("[MainPage] Bluetooth is OFF, asking user for permission to enable");

				var enableBluetooth = await DialogService.ShowConfirmationAsync(
					"Bluetooth is Off",
					"Bluetooth is currently turned off. Would you like to turn it on?",
					confirmText: "Turn On",
					cancelText: "Cancel");

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

			// Check if scanning was cancelled
			if (_scanningCts?.Token.IsCancellationRequested == true)
			{
				System.Diagnostics.Debug.WriteLine("[MainPage] ❌ SCAN CANCELLED by user");
				return;
			}

			// First, check for devices already connected at system level
			System.Diagnostics.Debug.WriteLine("[MainPage] ========== STEP 1: Check Already-Connected Devices ==========");
			List<BluetoothDevice> alreadyConnectedDevices;
			// TODO: GetConnectedDevicesAsync method not implemented in IBluetoothService
			// try
			// {
			// 	alreadyConnectedDevices = await _bluetoothService.GetConnectedDevicesAsync();
			// 	System.Diagnostics.Debug.WriteLine($"[MainPage] ✅ Found {alreadyConnectedDevices.Count} already-connected devices");
			// }
			// catch (Exception ex)
			// {
			// 	System.Diagnostics.Debug.WriteLine($"[MainPage] ⚠️ Error checking connected devices: {ex.Message}");
			// 	alreadyConnectedDevices = new List<BluetoothDevice>();
			// }
			alreadyConnectedDevices = new List<BluetoothDevice>();
			System.Diagnostics.Debug.WriteLine("[MainPage] Skipping already-connected devices check (not implemented)");

			// Check if scanning was cancelled
			if (_scanningCts?.Token.IsCancellationRequested == true)
			{
				System.Diagnostics.Debug.WriteLine("[MainPage] ❌ SCAN CANCELLED by user");
				return;
			}

			// Now scan for all available devices
			System.Diagnostics.Debug.WriteLine("[MainPage] ========== STEP 2: Scan for All Devices ==========");
			System.Diagnostics.Debug.WriteLine("[MainPage] Calling _bluetoothService.ScanForDevicesAsync()...");
			List<BluetoothDevice> devices;

			try
			{
				devices = await _bluetoothService.ScanForDevicesAsync();

				// Check if scanning was cancelled immediately after scan
				if (_scanningCts?.Token.IsCancellationRequested == true)
				{
					System.Diagnostics.Debug.WriteLine("[MainPage] ❌ SCAN CANCELLED by user after scan completed");
					return;
				}

				System.Diagnostics.Debug.WriteLine($"[MainPage] ✅ Scan completed: {devices.Count} devices found");

				if (devices.Count == 0)
				{
					System.Diagnostics.Debug.WriteLine("[MainPage] ⚠️⚠️⚠️ WARNING: Scan returned ZERO devices! ⚠️⚠️⚠️");
					System.Diagnostics.Debug.WriteLine("[MainPage] This usually means:");
					System.Diagnostics.Debug.WriteLine("[MainPage]   1. BLUETOOTH_CONNECT permission is missing/denied");
					System.Diagnostics.Debug.WriteLine("[MainPage]   2. No devices are paired in Android Bluetooth settings");
					System.Diagnostics.Debug.WriteLine("[MainPage]   3. Bluetooth adapter returned null for BondedDevices");
				}
			}
			catch (UnauthorizedAccessException uaEx)
			{
				System.Diagnostics.Debug.WriteLine($"[MainPage] ❌ UnauthorizedAccessException during scan!");
				System.Diagnostics.Debug.WriteLine($"[MainPage] Message: {uaEx.Message}");

				await DialogService.ShowErrorAsync(
					"Permission Denied",
					"Cannot access Bluetooth devices.",
					new List<string>
					{
						"CLOSE the app completely (swipe away from recent apps)",
						"Go to Settings → Apps → E-z MicLink",
						"Tap 'Permissions' → 'Nearby devices'",
						"Change from 'Ask every time' to 'Allow'",
						"Open E-z MicLink and try scanning again"
					});
				return;
			}

			// Merge the lists, marking already-connected devices
			// TODO: BluetoothDevice.IsConnected property not implemented
			// foreach (var alreadyConnected in alreadyConnectedDevices)
			// {
			// 	var existingDevice = devices.FirstOrDefault(d => d.Address == alreadyConnected.Address);
			// 	if (existingDevice != null)
			// 	{
			// 		// Mark existing device as connected
			// 		existingDevice.IsConnected = true;
			// 		System.Diagnostics.Debug.WriteLine($"[MainPage] ✅ Device {existingDevice.Name} is already connected at system level");
			// 	}
			// 	else
			// 	{
			// 		// Add the connected device if not in scan results
			// 		devices.Add(alreadyConnected);
			// 		System.Diagnostics.Debug.WriteLine($"[MainPage] ➕ Added already-connected device {alreadyConnected.Name}");
			// 	}
			// }

			_availableDevices = devices;

			System.Diagnostics.Debug.WriteLine($"[MainPage] ===== Scan returned {_availableDevices.Count} devices =====");
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
			// NEW LOGIC: Show recently connected devices separately, but ALL devices in Available
			// Recently Paired = Devices that have connection history (were successfully connected before)
			var recentlyPairedDevices = _availableDevices
				.Where(d => Services.DeviceConnectionHistory.HasConnectedBefore(d.Address))
				.Take(3)  // Show max 3 recently paired devices
				.ToList();

			// Available Devices = ALL devices (including paired, connected, everything)
			var availableDevices = _availableDevices.ToList();

			System.Diagnostics.Debug.WriteLine($"[MainPage] ========== STEP 3: Display Devices ==========");
			System.Diagnostics.Debug.WriteLine($"[MainPage] ✅ Recently Paired Devices: {recentlyPairedDevices.Count} devices");
			foreach (var device in recentlyPairedDevices)
			{
				// TODO: BluetoothDevice.IsConnected property not implemented
				System.Diagnostics.Debug.WriteLine($"[MainPage]   → {device.Name} ({device.Address}) - IsPaired: {device.IsPaired}"); // , IsConnected: {device.IsConnected}
			}

			System.Diagnostics.Debug.WriteLine($"[MainPage] ✅ Available Devices: {availableDevices.Count} devices (ALL devices)");
			foreach (var device in availableDevices)
			{
				// TODO: BluetoothDevice.IsConnected property not implemented
				System.Diagnostics.Debug.WriteLine($"[MainPage]   → {device.Name} ({device.Address}) - IsPaired: {device.IsPaired}"); // , IsConnected: {device.IsConnected}
			}

			// CRITICAL FIX: Update UI directly without SetState to avoid race condition
			// SetState uses MainThread.BeginInvokeOnMainThread which creates a race with our UI updates
			System.Diagnostics.Debug.WriteLine("[MainPage] Updating UI directly on main thread...");

			MainThread.BeginInvokeOnMainThread(() =>
			{
				System.Diagnostics.Debug.WriteLine("[MainPage] [UI Thread] Starting UI update...");

				// Update state tracker
				_currentState = UIState.DeviceList;

				// Hide everything except what we need
				// TODO: HeaderSection was removed
				// HeaderSection.IsVisible = true;
				ScanButton.IsVisible = true;
				MainCard.IsVisible = false;
				DeviceInfoSection.IsVisible = false;
				AudioControlsSection.IsVisible = false;
				MessageSection.IsVisible = false;
				ActionButtonsSection.IsVisible = false;
				SecondaryActionBorder.IsVisible = false;
				
				// BackButtonSection.IsVisible = false;

				// Clear selections
				AvailableDevicesView.SelectedItem = null;
				// TODO: RecentlyConnectedView was removed
				// RecentlyConnectedView.SelectedItem = null;

				// Show Recently Paired Devices section (devices with connection history)
				// TODO: RecentlyConnectedView and RecentlyConnectedSection were removed
				// if (recentlyPairedDevices.Any())
				// {
				// 	System.Diagnostics.Debug.WriteLine($"[MainPage] [UI Thread] Setting RecentlyConnectedView with {recentlyPairedDevices.Count} devices");
				// 	RecentlyConnectedView.ItemsSource = recentlyPairedDevices;
				// 	RecentlyConnectedSection.IsVisible = true;
				// 	System.Diagnostics.Debug.WriteLine("[MainPage] [UI Thread] ✅ RecentlyConnectedSection is now VISIBLE");
				// }
				// else
				// {
				// 	RecentlyConnectedSection.IsVisible = false;
				// 	System.Diagnostics.Debug.WriteLine("[MainPage] [UI Thread] RecentlyConnectedSection hidden (no recently paired devices)");
				// }

				// Show Available Devices section (ALL devices - paired, connected, unpaired, everything)
				if (availableDevices.Any())
				{
					System.Diagnostics.Debug.WriteLine($"[MainPage] [UI Thread] Setting AvailableDevicesView with {availableDevices.Count} devices");
					AvailableDevicesView.ItemsSource = availableDevices;
					AvailableDevicesSection.IsVisible = true;
					System.Diagnostics.Debug.WriteLine("[MainPage] [UI Thread] ✅ AvailableDevicesSection is now VISIBLE");
				}
				else
				{
					AvailableDevicesSection.IsVisible = false;
					System.Diagnostics.Debug.WriteLine("[MainPage] [UI Thread] AvailableDevicesSection hidden (no devices)");
				}

				System.Diagnostics.Debug.WriteLine("[MainPage] [UI Thread] ========== UI UPDATE COMPLETE ==========");
			});
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
		catch (OperationCanceledException)
		{
			System.Diagnostics.Debug.WriteLine("[MainPage] Scan was cancelled by user");
			// Don't show error dialog for user-initiated cancellation
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Scan error: {ex.Message}");
			await DialogService.ShowErrorAsync("Scan Error", $"Failed to scan for devices.\n\n{ex.Message}");
		}
		finally
		{
			// Stop scanning animations
			await StopScanningAnimationsAsync();

			MainThread.BeginInvokeOnMainThread(() =>
			{
				ScanButton.IsEnabled = true;
				// TODO: StopScanButton was removed
				// StopScanButton.IsVisible = false;
			});
		}
	}

	private void OnDeviceTapped(object? sender, TappedEventArgs e)
	{
		// Get the device from the binding context
		if (sender is Border border && border.BindingContext is BluetoothDevice device)
		{
			_selectedDevice = device;
			System.Diagnostics.Debug.WriteLine($"[MainPage] Device tapped: {_selectedDevice?.Name}");
			// TODO: BluetoothDevice.IsConnected property not implemented
			// System.Diagnostics.Debug.WriteLine($"[MainPage] Device IsConnected: {_selectedDevice?.IsConnected}");

			// Check if this device is already connected at system level
			// TODO: BluetoothDevice.IsConnected and UseAlreadyConnectedDevice not implemented
			// if (_selectedDevice != null && _selectedDevice.IsConnected)
			// {
			// 	// Device is already connected at system level - use it immediately
			// 	System.Diagnostics.Debug.WriteLine($"[MainPage] Device {_selectedDevice.Name} is already connected at system level, using it immediately");
			//
			// 	// Mark as our connected device (no need to establish connection)
			// 	_bluetoothService.UseAlreadyConnectedDevice(_selectedDevice);
			//
			// 	// Go directly to connected state
			// 	SetState(UIState.Connected);
			// }
			// Check if we're already connected to this device in our app
			if (_bluetoothService.IsConnected &&
			    _bluetoothService.ConnectedDevice != null &&
			    _selectedDevice != null &&
			    _bluetoothService.ConnectedDevice.Address == _selectedDevice.Address)
			{
				// Already connected to this device via our app - go directly to engagement view
				System.Diagnostics.Debug.WriteLine($"[MainPage] Already connected to {_selectedDevice.Name} via app, going to engagement view");
				SetState(UIState.Connected);
			}
			else
			{
				// Not connected - show connect button
				System.Diagnostics.Debug.WriteLine($"[MainPage] Not connected to this device, showing connect button");
				SetState(UIState.DeviceSelected);
			}
		}
	}

	private void OnDeviceSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.Count > 0)
		{
			_selectedDevice = e.CurrentSelection[0] as BluetoothDevice;
			System.Diagnostics.Debug.WriteLine($"[MainPage] Device selected: {_selectedDevice?.Name}");
			// TODO: BluetoothDevice.IsConnected property not implemented
			// System.Diagnostics.Debug.WriteLine($"[MainPage] Device IsConnected: {_selectedDevice?.IsConnected}");

			// Check if this device is already connected at system level
			// TODO: BluetoothDevice.IsConnected and UseAlreadyConnectedDevice not implemented
			// if (_selectedDevice != null && _selectedDevice.IsConnected)
			// {
			// 	// Device is already connected at system level - use it immediately
			// 	System.Diagnostics.Debug.WriteLine($"[MainPage] Device {_selectedDevice.Name} is already connected at system level, using it immediately");
			//
			// 	// Mark as our connected device (no need to establish connection)
			// 	_bluetoothService.UseAlreadyConnectedDevice(_selectedDevice);
			//
			// 	// Go directly to connected state
			// 	SetState(UIState.Connected);
			// }
			// Check if we're already connected to this device in our app
			if (_bluetoothService.IsConnected &&
			    _bluetoothService.ConnectedDevice != null &&
			    _selectedDevice != null &&
			    _bluetoothService.ConnectedDevice.Address == _selectedDevice.Address)
			{
				// Already connected to this device via our app - go directly to engagement view
				System.Diagnostics.Debug.WriteLine($"[MainPage] Already connected to {_selectedDevice.Name} via app, going to engagement view");
				SetState(UIState.Connected);
			}
			else
			{
				// Not connected - show connect button
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
				// Verify connection is actually established by waiting briefly and checking status
				await Task.Delay(500);

				// Double-check that we're still connected
				if (_bluetoothService.IsConnected)
				{
					// Mark this device as successfully connected in history
					Services.DeviceConnectionHistory.MarkDeviceAsConnected(_selectedDevice.Address);
					System.Diagnostics.Debug.WriteLine($"[MainPage] Device marked as compatible: {_selectedDevice.Address}");

					SetState(UIState.Connected);
					System.Diagnostics.Debug.WriteLine($"[MainPage] ✅ Successfully connected to {_selectedDevice.Name}");
				}
				else
				{
					// Connection dropped immediately after "success"
					System.Diagnostics.Debug.WriteLine($"[MainPage] ⚠️ Connection dropped immediately after success");
					await HandleConnectionFailure(_selectedDevice.Name);
				}
			}
			else
			{
				// Connection failed - show alert and rescan
				System.Diagnostics.Debug.WriteLine($"[MainPage] ❌ Connection failed for {_selectedDevice.Name}");
				await HandleConnectionFailure(_selectedDevice.Name);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] ❌ Connection error: {ex.Message}");
			// Connection failed with exception - show alert and rescan
			await HandleConnectionFailure(_selectedDevice?.Name ?? "device");
		}
	}

	private async Task HandleConnectionFailure(string deviceName)
	{
		System.Diagnostics.Debug.WriteLine($"[MainPage] Handling connection failure for {deviceName}");

		// Set to failed state briefly to show the animation
		SetState(UIState.Failed);

		// Wait a moment for the failed animation to be visible
		await Task.Delay(500);

		// Show alert dialog
		await DialogService.ShowErrorAsync(
			"Connection Failed",
			$"Failed to connect to {deviceName}.",
			new List<string>
			{
				"Device might be out of range",
				"Device might be connected to another phone",
				"Try moving closer to the device",
				"Try forgetting and re-pairing the device"
			});

		// Go back to initial state (home screen)
		SetState(UIState.Initial);

		// Wait a bit before starting scan
		await Task.Delay(300);

		// Automatically start a new scan to refresh the device list
		System.Diagnostics.Debug.WriteLine("[MainPage] Starting automatic rescan after connection failure");
		await StartScanning();
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
		// Prevent multiple concurrent operations
		if (_isAudioOperationInProgress)
		{
			System.Diagnostics.Debug.WriteLine("[MainPage] Audio operation already in progress, ignoring Start click");
			return;
		}

		_isAudioOperationInProgress = true;

		try
		{
			System.Diagnostics.Debug.WriteLine("[MainPage] Starting audio routing...");

			// Disable both buttons during operation
			StartAudioBtn.IsEnabled = false;
			StopAudioBtn.IsEnabled = false;

			await _audioService.StartAudioRoutingAsync();

			System.Diagnostics.Debug.WriteLine("[MainPage] Audio routing started successfully");

			// Update UI
			MainThread.BeginInvokeOnMainThread(() =>
			{
				StartAudioBtn.IsVisible = false;
				StartAudioBtn.IsEnabled = true;
				StopAudioBtn.IsVisible = true;
				StopAudioBtn.IsEnabled = true;
			});
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Start audio error: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[MainPage] Stack trace: {ex.StackTrace}");

			// Re-enable buttons on error
			MainThread.BeginInvokeOnMainThread(() =>
			{
				StartAudioBtn.IsEnabled = true;
				StopAudioBtn.IsEnabled = true;
			});

			await DialogService.ShowErrorAsync("Audio Error", $"Failed to start audio.\n\n{ex.Message}");
		}
		finally
		{
			_isAudioOperationInProgress = false;
		}
	}

	private async void OnStopAudioClicked(object? sender, EventArgs e)
	{
		// Prevent multiple concurrent operations
		if (_isAudioOperationInProgress)
		{
			System.Diagnostics.Debug.WriteLine("[MainPage] Audio operation already in progress, ignoring Stop click");
			return;
		}

		_isAudioOperationInProgress = true;

		try
		{
			System.Diagnostics.Debug.WriteLine("[MainPage] Stopping audio routing...");

			// Disable both buttons during operation
			StartAudioBtn.IsEnabled = false;
			StopAudioBtn.IsEnabled = false;

			await _audioService.StopAudioRoutingAsync();

			System.Diagnostics.Debug.WriteLine("[MainPage] Audio routing stopped successfully");

			// Update UI
			MainThread.BeginInvokeOnMainThread(() =>
			{
				StartAudioBtn.IsVisible = true;
				StartAudioBtn.IsEnabled = true;
				StopAudioBtn.IsVisible = false;
				StopAudioBtn.IsEnabled = true;
			});
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Stop audio error: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[MainPage] Stack trace: {ex.StackTrace}");

			// Re-enable buttons on error
			MainThread.BeginInvokeOnMainThread(() =>
			{
				StartAudioBtn.IsEnabled = true;
				StopAudioBtn.IsEnabled = true;
			});

			// Don't show dialog for stop errors (less disruptive)
		}
		finally
		{
			_isAudioOperationInProgress = false;
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

			// Rescan for devices after disconnect
			await Task.Delay(300);
			await StartScanning();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Disconnect error: {ex.Message}");
		}
	}

	private async void OnBackButtonClicked(object? sender, EventArgs e)
	{
		try
		{
			// Go back to device list while staying connected
			System.Diagnostics.Debug.WriteLine("[MainPage] Back button clicked - returning to device list");
			_selectedDevice = null;
			SetState(UIState.Initial);

			// Rescan to show device list
			await Task.Delay(300);
			await StartScanning();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Back button error: {ex.Message}");
		}
	}

	private async void OnRenameDeviceClicked(object? sender, EventArgs e)
	{
		if (_selectedDevice == null) return;

		try
		{
			var currentName = DeviceNameManager.GetDisplayName(_selectedDevice.Address, _selectedDevice.Name);
			var result = await DialogService.ShowTextInputAsync(
				title: "Rename Device",
				message: "Enter a custom name for this device:",
				icon: "✏️",
				placeholder: "My Device",
				initialValue: currentName,
				maxLength: 30,
				keyboard: Keyboard.Text);

			if (!string.IsNullOrWhiteSpace(result))
			{
				bool success = DeviceNameManager.SetCustomName(_selectedDevice.Address, result);

				if (success)
				{
					DeviceNameLabel.Text = result;
					System.Diagnostics.Debug.WriteLine($"[MainPage] Device renamed: {_selectedDevice.Name} → {result}");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"[MainPage] ERROR: Failed to save device name!");
					await DialogService.ShowErrorAsync(
						"Save Failed",
						"Failed to save the device name. Please try again.");
				}
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

				var result = await DialogService.ShowTextInputAsync(
					title: "Rename Device",
					message: "Enter a custom name for this device:",
					icon: "✏️",
					placeholder: "My Device",
					initialValue: currentName,
					maxLength: 30,
					keyboard: Keyboard.Text);

				System.Diagnostics.Debug.WriteLine($"[MainPage] User entered: '{result}'");

				if (!string.IsNullOrWhiteSpace(result))
				{
					System.Diagnostics.Debug.WriteLine($"[MainPage] Calling SetCustomName...");
					bool success = DeviceNameManager.SetCustomName(device.Address, result);

					System.Diagnostics.Debug.WriteLine($"[MainPage] SetCustomName completed with success={success}");

					if (success)
					{
						// Update the device name in the list
						device.Name = result;

						System.Diagnostics.Debug.WriteLine($"[MainPage] Updated device.Name to: '{device.Name}'");

						// Refresh both collection views to show the new name
						var currentItems = AvailableDevicesView.ItemsSource;
					AvailableDevicesView.ItemsSource = null;
					AvailableDevicesView.ItemsSource = currentItems;

						System.Diagnostics.Debug.WriteLine($"[MainPage] Collection views refreshed");
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"[MainPage] ERROR: Failed to save device name!");
						await DialogService.ShowErrorAsync(
							"Save Failed",
							"Failed to save the device name. This may be a storage issue.",
							new List<string>
							{
								"Check app permissions in Settings",
								"Try restarting the app",
								"Contact support if issue persists"
							});
					}
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
				var confirmed = await DialogService.ShowConfirmationAsync(
					"Forget Device",
					$"Are you sure you want to forget \"{deviceName}\"?",
					confirmText: "Forget",
					cancelText: "Cancel",
					bulletPoints: new List<string>
					{
						"Remove custom name",
						"Unpair the device from your phone"
					});

				if (confirmed)
				{
					System.Diagnostics.Debug.WriteLine($"[MainPage] Forgetting device: {deviceName}");

					// Remove custom name
					DeviceNameManager.RemoveCustomName(device.Address);

					// Unpair the device (Android)
					await _bluetoothService.UnpairDeviceAsync(device);

					// Refresh device list
					_availableDevices.Remove(device);
					var currentItems = AvailableDevicesView.ItemsSource;
					AvailableDevicesView.ItemsSource = null;
					AvailableDevicesView.ItemsSource = currentItems;

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

	// ==================== Scanning Animations ====================

	private async void StartScanningAnimations()
	{
		// Cancel any existing animations
		StopScanningAnimations();

		// Set scanning text (stays constant during animation)
		MainThread.BeginInvokeOnMainThread(() =>
		{
			ScanButtonText.Text = "Scanning for Devices";
		});

		// Start magnifying glass animation (figure-8 pattern)
		_magnifyingGlassAnimationCts = new CancellationTokenSource();
		_ = AnimateMagnifyingGlass(_magnifyingGlassAnimationCts.Token);

		// Start dots animation (. → .. → ...)
		_dotsAnimationCts = new CancellationTokenSource();
		_ = AnimateDots(_dotsAnimationCts.Token);
	}

	private async Task StopScanningAnimationsAsync()
	{
		System.Diagnostics.Debug.WriteLine("[MainPage] StopScanningAnimationsAsync called");

		// Cancel magnifying glass animation
		_magnifyingGlassAnimationCts?.Cancel();
		_magnifyingGlassAnimationCts?.Dispose();
		_magnifyingGlassAnimationCts = null;

		// Cancel dots animation
		_dotsAnimationCts?.Cancel();
		_dotsAnimationCts?.Dispose();
		_dotsAnimationCts = null;

		// Reset to original state - ensure it runs on UI thread and completes
		try
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				ScanButtonText.Text = "Scan for Devices";
				DotsLabel.Text = "";
				MagnifyingGlass.TranslationX = 0;
				MagnifyingGlass.TranslationY = 0;
				System.Diagnostics.Debug.WriteLine("[MainPage] Scan button text reset to: " + ScanButtonText.Text);
			});
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[MainPage] Error stopping animations: {ex.Message}");
		}
	}

	private void StopScanningAnimations()
	{
		// Sync wrapper for compatibility
		_ = StopScanningAnimationsAsync();
	}

	private async Task AnimateMagnifyingGlass(CancellationToken cancellationToken)
	{
		try
		{
			// Figure-8 (lemniscate) animation parameters
			const double amplitude = 15.0;  // Size of the figure-8
			const int steps = 60;            // Number of steps per cycle
			const int delayMs = 30;          // Delay between steps

			while (!cancellationToken.IsCancellationRequested)
			{
				// Animate one complete figure-8 cycle
				for (int i = 0; i < steps; i++)
				{
					if (cancellationToken.IsCancellationRequested) break;

					// Parametric equations for figure-8 (lemniscate)
					// Starting from left side: t goes from 0 to 2π
					double t = (i / (double)steps) * 2 * Math.PI;

					// Horizontal figure-8 pattern
					double x = amplitude * Math.Cos(t);
					double y = (amplitude / 2) * Math.Sin(2 * t);

					await MagnifyingGlass.TranslateTo(x, y, delayMs, Easing.Linear);
				}

				// Small pause before repeating
				await Task.Delay(50, cancellationToken);
			}
		}
		catch (TaskCanceledException)
		{
			// Animation cancelled, reset position
			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await MagnifyingGlass.TranslateTo(0, 0, 200, Easing.CubicOut);
			});
		}
	}

	private async Task AnimateDots(CancellationToken cancellationToken)
	{
		try
		{
			int dotCount = 1;
			while (!cancellationToken.IsCancellationRequested)
			{
				string dots = new string('.', dotCount);
				MainThread.BeginInvokeOnMainThread(() =>
				{
					DotsLabel.Text = dots;
				});

				await Task.Delay(500, cancellationToken);

				dotCount = (dotCount % 3) + 1; // Cycle: 1 → 2 → 3 → 1
			}
		}
		catch (TaskCanceledException)
		{
			// Animation cancelled, clear dots
			MainThread.BeginInvokeOnMainThread(() =>
			{
				DotsLabel.Text = "";
			});
		}
	}
}
