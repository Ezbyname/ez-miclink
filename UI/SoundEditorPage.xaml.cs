using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.UI;

public partial class SoundEditorPage : ContentPage
{
	private readonly IAudioService _audioService;
	private readonly string _presetName;
	private readonly string _displayName;
	private readonly string _basePresetName;

	private bool _isModified;
	private bool _isUpdating;

	// Preset defaults (UI names -> audio engine mapping):
	// Tone      -> Bass
	// Space     -> Mid
	// Bright    -> Treble
	// Character -> Distortion
	private float _defaultTone;
	private float _defaultSpace;
	private float _defaultBright;
	private float _defaultCharacter;

	public SoundEditorPage(IAudioService audioService, string presetName, string displayName, string? basePresetName = null)
	{
		InitializeComponent();

		_audioService = audioService;
		_presetName = presetName;
		_displayName = displayName;
		_basePresetName = basePresetName ?? presetName;

		PresetNameLabel.Text = displayName;
		PresetSubtitle.Text = $"Based on: {_basePresetName}";

		LoadPresetDefaults();
		ApplyDefaultsToUi();
		ApplyCurrentValuesToAudio();

		_isModified = false;
		SaveAsButton.IsVisible = false;
		ResetButton.IsVisible = false;

		StopBar.Attach(_audioService);
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		StopBar.Detach();
	}

	private void LoadPresetDefaults()
	{
		// Default sliders are always 0 (neutral) for all presets.
		// Each preset's Configure() already defines its own sound via the effect chain.
		// The sliders represent user tweaks on top of the preset, so 0 = pure preset sound.
		_defaultTone = 0f;
		_defaultSpace = 0f;
		_defaultBright = 0f;
		_defaultCharacter = 0f;
	}

	private void ApplyDefaultsToUi()
	{
		_isUpdating = true;
		ToneSlider.Value = _defaultTone;
		SpaceSlider.Value = _defaultSpace;
		BrightSlider.Value = _defaultBright;
		CharacterSlider.Value = _defaultCharacter;
		_isUpdating = false;
	}

	/// <summary>
	/// Load saved custom sound values into the editor.
	/// Storage model uses Bass/Mid/Treble/Distortion -> mapped to UI names.
	/// </summary>
	public void LoadCustomValues(float bass, float mid, float treble, float distortion)
	{
		// The saved values become the defaults for this editing session
		_defaultTone = bass;
		_defaultSpace = mid;
		_defaultBright = treble;
		_defaultCharacter = distortion;

		_isUpdating = true;
		ToneSlider.Value = bass;
		SpaceSlider.Value = mid;
		BrightSlider.Value = treble;
		CharacterSlider.Value = distortion;
		_isUpdating = false;

		ApplyCurrentValuesToAudio();

		// Starting from saved values - not modified yet
		_isModified = false;
		SaveAsButton.IsVisible = false;
		ResetButton.IsVisible = false;
	}

	private void ApplyCurrentValuesToAudio()
	{
		float tone = (float)ToneSlider.Value;
		float space = (float)SpaceSlider.Value;
		float bright = (float)BrightSlider.Value;
		float character = (float)CharacterSlider.Value;

		// Scale slider values (±10) to dB range (±12) for audible impact without distortion
		_audioService.SetMasterEQ(tone * 1.2f, space * 1.2f, bright * 1.2f);
		_audioService.SetMasterDistortion(character / 10f);
	}

	private void OnControlChanged(object? sender, ValueChangedEventArgs e)
	{
		if (_isUpdating)
			return;

		ApplyCurrentValuesToAudio();

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
			placeholder: "My Custom Voice",
			maxLength: 30);

		if (string.IsNullOrWhiteSpace(name))
			return;

		var customSound = new CustomSound
		{
			Name = name.Trim(),
			BasePreset = _basePresetName,
			Bass = (float)ToneSlider.Value,
			Mid = (float)SpaceSlider.Value,
			Treble = (float)BrightSlider.Value,
			Distortion = (float)CharacterSlider.Value,
			CreatedAt = DateTime.Now
		};

		CustomSoundService.Save(customSound);

		_isModified = false;
		SaveAsButton.IsVisible = false;
		ResetButton.IsVisible = false;

		await DisplayAlert("Saved", $"\"{name}\" has been saved to My Saved Sounds.", "OK");
	}

	private void OnResetClicked(object? sender, EventArgs e)
	{
		// Re-apply the base preset to reset the audio engine to its pure sound
		_audioService.SetEffect(_basePresetName);

		ApplyDefaultsToUi();
		ApplyCurrentValuesToAudio();

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
		if (_isModified)
		{
			bool discard = await DisplayAlert(
				"Discard changes?",
				"You have unsaved changes. Leave without saving?",
				"Discard",
				"Stay");

			if (!discard)
				return;
		}

		await Navigation.PopAsync();
	}
}
