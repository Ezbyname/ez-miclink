using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.Components;

public partial class FloatingStopBar : ContentView
{
	private IAudioService? _audioService;
	private System.Timers.Timer? _pollTimer;

	public FloatingStopBar()
	{
		InitializeComponent();
	}

	public void Attach(IAudioService audioService)
	{
		_audioService = audioService;

		// Poll IsRouting every second to show/hide
		_pollTimer = new System.Timers.Timer(1000);
		_pollTimer.Elapsed += (s, e) =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				IsVisible = _audioService?.IsRouting == true;
			});
		};
		_pollTimer.Start();

		// Initial state
		IsVisible = _audioService.IsRouting;
	}

	public void Detach()
	{
		_pollTimer?.Stop();
		_pollTimer?.Dispose();
		_pollTimer = null;
	}

	private async void OnStopTapped(object? sender, EventArgs e)
	{
		if (_audioService == null) return;

		try
		{
			await _audioService.StopAudioRoutingAsync();
			IsVisible = false;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[FloatingStopBar] Stop error: {ex.Message}");
		}
	}
}
