using BluetoothMicrophoneApp.Services;

namespace BluetoothMicrophoneApp.UI;

public partial class SoundEditorPage : ContentPage
{
	private readonly IAudioService _audioService;
	private readonly string _presetName;
	private readonly string _displayName;

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

	public SoundEditorPage(IAudioService audioService, string presetName, string displayName)
	{
		InitializeComponent();

		_audioService = audioService;
		_presetName = presetName;
		_displayName = displayName;

		PresetNameLabel.Text = displayName;
		PresetSubtitle.Text = $"Based on: {displayName}";

		LoadPresetDefaults();
		ApplyDefaultsToUi();
		ApplyCurrentValuesToAudio();

		_isModified = false;
		SaveAsButton.IsVisible = false;
		ResetButton.IsVisible = false;
	}

	private void LoadPresetDefaults()
	{
		switch (_presetName.ToLowerInvariant())
		{
			case "robot":
				_defaultTone = -2f; _defaultSpace = -1f;
				_defaultBright = 3f; _defaultCharacter = 7f;
				break;
			case "megaphone":
				_defaultTone = -3f; _defaultSpace = 2f;
				_defaultBright = 5f; _defaultCharacter = 6f;
				break;
			case "stadium":
				_defaultTone = 1f; _defaultSpace = 8f;
				_defaultBright = 2f; _defaultCharacter = 1f;
				break;
			case "deepvoice":
			case "deep voice":
				_defaultTone = 6f; _defaultSpace = 1f;
				_defaultBright = -3f; _defaultCharacter = 2f;
				break;
			case "helium":
			case "chipmunk":
				_defaultTone = -4f; _defaultSpace = 0f;
				_defaultBright = 7f; _defaultCharacter = 1f;
				break;
			case "anime":
			case "animevoice":
			case "anime voice":
				_defaultTone = -1f; _defaultSpace = 1f;
				_defaultBright = 6f; _defaultCharacter = 2f;
				break;
			case "podcast":
				_defaultTone = 2f; _defaultSpace = 1f;
				_defaultBright = 3f; _defaultCharacter = 0f;
				break;
			case "villain":
				_defaultTone = 5f; _defaultSpace = 3f;
				_defaultBright = -2f; _defaultCharacter = 5f;
				break;
			case "grumpycat":
			case "grumpy cat":
				_defaultTone = 3f; _defaultSpace = -2f;
				_defaultBright = -1f; _defaultCharacter = 4f;
				break;
			default:
				_defaultTone = 0f; _defaultSpace = 0f;
				_defaultBright = 0f; _defaultCharacter = 0f;
				break;
		}
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
		_isUpdating = true;
		ToneSlider.Value = bass;
		SpaceSlider.Value = mid;
		BrightSlider.Value = treble;
		CharacterSlider.Value = distortion;
		_isUpdating = false;

		ApplyCurrentValuesToAudio();

		_isModified = true;
		SaveAsButton.IsVisible = true;
		ResetButton.IsVisible = true;
	}

	private void ApplyCurrentValuesToAudio()
	{
		float tone = (float)ToneSlider.Value;
		float space = (float)SpaceSlider.Value;
		float bright = (float)BrightSlider.Value;
		float character = (float)CharacterSlider.Value;

		_audioService.SetMasterEQ(tone, space, bright);
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
			BasePreset = _presetName,
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
