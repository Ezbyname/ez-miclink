using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.UI;

public partial class VisualizerPage : ContentPage
{
	private readonly IAudioService _audioService;
	private bool _isRunning = false;
	private System.Timers.Timer? _animationTimer;
	private System.Timers.Timer? _volumeTimer;

	public VisualizerPage(IAudioService audioService)
	{
		InitializeComponent();
		_audioService = audioService;
		_audioService.StatusChanged += OnAudioStatusChanged;
	}

	private void OnAudioStatusChanged(object? sender, string status)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			StatusLabel.Text = status;
		});
	}

	private async void OnToggleClicked(object? sender, EventArgs e)
	{
		try
		{
			if (!_isRunning)
			{
				// Start
				await _audioService.StartAudioRoutingAsync();
				_isRunning = true;

				ToggleLabel.Text = "⏹ Stop";
				ToggleButton.Background = new LinearGradientBrush
				{
					StartPoint = new Point(0, 0),
					EndPoint = new Point(1, 1),
					GradientStops = new GradientStopCollection
					{
						new GradientStop { Color = Color.FromArgb("#FF5252"), Offset = 0.0f },
						new GradientStop { Color = Color.FromArgb("#E53935"), Offset = 1.0f }
					}
				};

				StatusLabel.Text = "🎤 Recording...";
				OnAirIndicator.IsVisible = true;

				StartAnimations();
			}
			else
			{
				// Stop
				await _audioService.StopAudioRoutingAsync();
				_isRunning = false;

				ToggleLabel.Text = "▶ Start";
				ToggleButton.Background = new LinearGradientBrush
				{
					StartPoint = new Point(0, 0),
					EndPoint = new Point(1, 1),
					GradientStops = new GradientStopCollection
					{
						new GradientStop { Color = Color.FromArgb("#4CAF50"), Offset = 0.0f },
						new GradientStop { Color = Color.FromArgb("#45A049"), Offset = 1.0f }
					}
				};

				StatusLabel.Text = "Ready";
				OnAirIndicator.IsVisible = false;

				StopAnimations();
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[VisualizerPage] Toggle error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"Failed to toggle audio.\\n\\n{ex.Message}");
		}
	}

	private void StartAnimations()
	{
		// Pulse animation for the glow rings
		_animationTimer = new System.Timers.Timer(50);
		_animationTimer.Elapsed += (s, e) =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var scale = 1.0 + (Math.Sin(DateTime.Now.Millisecond / 100.0) * 0.1);
					OuterGlow.Scale = scale;
					MiddleGlow.Scale = 1.2 - (scale - 1.0);
				}
				catch { }
			});
		};
		_animationTimer.Start();

		// Volume meter animation
		_volumeTimer = new System.Timers.Timer(100);
		_volumeTimer.Elapsed += (s, e) =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var random = new Random();
					VolumeMeter.Progress = random.NextDouble() * 0.8 + 0.2;
				}
				catch { }
			});
		};
		_volumeTimer.Start();
	}

	private void StopAnimations()
	{
		_animationTimer?.Stop();
		_animationTimer?.Dispose();
		_animationTimer = null;

		_volumeTimer?.Stop();
		_volumeTimer?.Dispose();
		_volumeTimer = null;

		MainThread.BeginInvokeOnMainThread(() =>
		{
			OuterGlow.Scale = 1.0;
			MiddleGlow.Scale = 1.0;
			VolumeMeter.Progress = 0;
		});
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		StopAnimations();
		await Navigation.PopAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		StopAnimations();
		_audioService.StatusChanged -= OnAudioStatusChanged;
	}
}
