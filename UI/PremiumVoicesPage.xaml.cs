using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.UI;

public partial class PremiumVoicesPage : ContentPage
{
	private readonly IAudioService _audioService;
	private string _currentVoice = "None";

	// Map of preset names to display names
	private static readonly Dictionary<string, string> VoiceDisplayNames = new()
	{
		["nerdy"] = "Nerdy",
		["squeaky_cartoon"] = "Squeaky Cartoon",
		["dopey_giant"] = "Dopey Giant",
		["squawky_bird"] = "Squawky Bird",
		["dopey_dad"] = "Dopey Dad",
		["mouse"] = "Mouse",
		["villain"] = "Villain",
		["grumpy_cat"] = "Grumpy Cat"
	};

	public PremiumVoicesPage(IAudioService audioService)
	{
		InitializeComponent();
		_audioService = audioService;
	}

	private async void OnVoiceSelected(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is not string voicePreset)
			return;

		try
		{
			// Clear all checkmarks
			NerdyCheck.IsVisible = false;
			SqueakyCheck.IsVisible = false;
			DopeyGiantCheck.IsVisible = false;
			SquawkyCheck.IsVisible = false;
			DopeyDadCheck.IsVisible = false;
			MouseCheck.IsVisible = false;
			VillainCheck.IsVisible = false;
			GrumpyCatCheck.IsVisible = false;

			// Set the selected checkmark
			switch (voicePreset)
			{
				case "nerdy": NerdyCheck.IsVisible = true; break;
				case "squeaky_cartoon": SqueakyCheck.IsVisible = true; break;
				case "dopey_giant": DopeyGiantCheck.IsVisible = true; break;
				case "squawky_bird": SquawkyCheck.IsVisible = true; break;
				case "dopey_dad": DopeyDadCheck.IsVisible = true; break;
				case "mouse": MouseCheck.IsVisible = true; break;
				case "villain": VillainCheck.IsVisible = true; break;
				case "grumpy_cat": GrumpyCatCheck.IsVisible = true; break;
			}

			var displayName = VoiceDisplayNames.GetValueOrDefault(voicePreset, voicePreset);
			_currentVoice = displayName;
			CurrentVoiceLabel.Text = displayName;

			_audioService.SetEffect(voicePreset);

			// Navigate to sound editor with the selected voice
			await Navigation.PushAsync(new SoundEditorPage(_audioService, voicePreset, displayName));
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[PremiumVoicesPage] Error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"Failed to apply voice.\n\n{ex.Message}");
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
