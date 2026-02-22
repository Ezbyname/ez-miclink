using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.UI;

public partial class EffectsPage : ContentPage
{
	private readonly IAudioService _audioService;
	private string _currentEffect = "None";

	public EffectsPage(IAudioService audioService)
	{
		InitializeComponent();
		_audioService = audioService;
	}

	private async void OnEffectSelected(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is not string effect)
			return;

		try
		{
			// Clear all checkmarks
			PodcastCheck.IsVisible = false;
			StageMCCheck.IsVisible = false;
			KaraokeCheck.IsVisible = false;
			AnnouncerCheck.IsVisible = false;
			RobotCheck.IsVisible = false;
			MegaphoneCheck.IsVisible = false;
			StadiumCheck.IsVisible = false;
			DeepVoiceCheck.IsVisible = false;
			ChipmunkCheck.IsVisible = false;
			AnimeVoiceCheck.IsVisible = false;

			// Set the selected checkmark
			switch (effect)
			{
				case "Podcast":
					PodcastCheck.IsVisible = true;
					break;
				case "Stage MC":
					StageMCCheck.IsVisible = true;
					break;
				case "Karaoke":
					KaraokeCheck.IsVisible = true;
					break;
				case "Announcer":
					AnnouncerCheck.IsVisible = true;
					break;
				case "Robot":
					RobotCheck.IsVisible = true;
					break;
				case "Megaphone":
					MegaphoneCheck.IsVisible = true;
					break;
				case "Stadium":
					StadiumCheck.IsVisible = true;
					break;
				case "Deep Voice":
					DeepVoiceCheck.IsVisible = true;
					break;
				case "Chipmunk":
					ChipmunkCheck.IsVisible = true;
					break;
				case "Anime Voice":
					AnimeVoiceCheck.IsVisible = true;
					break;
			}

			_currentEffect = effect;
			CurrentEffectLabel.Text = effect;

			// Apply the effect to the audio service
			string effectPresetName = effect.Replace(" ", "_").ToLower();
			_audioService.SetEffect(effectPresetName);

			// Show confirmation
			await DialogService.ShowSuccessAsync(
				"Effect Applied",
				$"{effect} effect has been activated!",
				new List<string>
				{
					"This effect is now processing your voice in real-time",
					"You can change effects at any time"
				}
			);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[EffectsPage] Error: {ex.Message}");
			await DialogService.ShowErrorAsync("Error", $"Failed to apply effect.\\n\\n{ex.Message}");
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
