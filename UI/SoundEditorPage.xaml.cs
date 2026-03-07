using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.UI;

public partial class SoundEditorPage : ContentPage
{
	private readonly IAudioService _audioService;
	private readonly string _presetName;
	private readonly string _displayName;
	private bool _isModified;
	private bool _isUpdating; // Prevent feedback loops

	public SoundEditorPage(IAudioService audioService, string presetName, string displayName)
	{
		InitializeComponent();
		_audioService = audioService;
		_presetName = presetName;
		_displayName = displayName;

		PresetNameLabel.Text = displayName;
		PresetSubtitle.Text = $"Based on: {displayName}";

		// Reset master EQ to flat, then load current values
		_audioService.ResetMasterEQ();
		_isModified = false;
	}

	/// <summary>
	/// Load a saved custom sound's EQ values into the sliders.
	/// </summary>
	public void LoadCustomValues(float bass, float mid, float treble, float distortion)
	{
		_isUpdating = true;
		BassSlider.Value = bass;
		MidSlider.Value = mid;
		TrebleSlider.Value = treble;
		DistortionSlider.Value = distortion;
		_isUpdating = false;

		// Apply to audio engine
		_audioService.SetMasterEQ(bass, mid, treble);
		_audioService.SetMasterDistortion(distortion / 10f);

		_isModified = true;
		SaveAsButton.IsVisible = true;
		ResetButton.IsVisible = true;
	}

	private void OnEQChanged(object? sender, ValueChangedEventArgs e)
	{
		if (_isUpdating)
			return;

		float bass = (float)BassSlider.Value;
		float mid = (float)MidSlider.Value;
		float treble = (float)TrebleSlider.Value;
		float distortion = (float)DistortionSlider.Value;

		// Apply EQ in real-time
		_audioService.SetMasterEQ(bass, mid, treble);
		_audioService.SetMasterDistortion(distortion / 10f); // 0-10 slider -> 0-1 range

		// Show Save As button when user modifies
		if (!_isModified)
		{
			_isModified = true;
			SaveAsButton.IsVisible = true;
			ResetButton.IsVisible = true;
		}
	}

	private async void OnSaveAsClicked(object? sender, EventArgs e)
	{
		string? name = await DisplayPromptAsync(
			"Save Custom Sound",
			"Enter a name for your sound:",
			"Save",
			"Cancel",
			placeholder: "My Cool Sound",
			maxLength: 30);

		if (string.IsNullOrWhiteSpace(name))
			return;

		var customSound = new CustomSound
		{
			Name = name.Trim(),
			BasePreset = _presetName,
			Bass = (float)BassSlider.Value,
			Mid = (float)MidSlider.Value,
			Treble = (float)TrebleSlider.Value,
			Distortion = (float)DistortionSlider.Value,
			CreatedAt = DateTime.Now
		};

		CustomSoundService.Save(customSound);

		await DisplayAlert("Saved", $"\"{name}\" has been saved to My Saved Sounds.", "OK");
	}

	private void OnResetClicked(object? sender, EventArgs e)
	{
		_isUpdating = true;
		BassSlider.Value = 0;
		MidSlider.Value = 0;
		TrebleSlider.Value = 0;
		DistortionSlider.Value = 0;
		_isUpdating = false;

		_audioService.ResetMasterEQ();

		_isModified = false;
		SaveAsButton.IsVisible = false;
		ResetButton.IsVisible = false;
	}

	private async void OnSavedSoundsClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new SavedSoundsPage(_audioService));
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
