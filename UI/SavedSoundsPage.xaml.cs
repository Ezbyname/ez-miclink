using BluetoothMicrophoneApp.Services;
using Microsoft.Maui.Controls.Shapes;

namespace BluetoothMicrophoneApp.UI;

public partial class SavedSoundsPage : ContentPage
{
	private readonly IAudioService _audioService;

	public SavedSoundsPage(IAudioService audioService)
	{
		InitializeComponent();
		_audioService = audioService;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		LoadSounds();
	}

	private void LoadSounds()
	{
		SoundsContainer.Children.Clear();
		var sounds = CustomSoundService.LoadAll();

		if (sounds.Count == 0)
		{
			EmptyState.IsVisible = true;
			return;
		}

		EmptyState.IsVisible = false;

		foreach (var sound in sounds.OrderByDescending(s => s.CreatedAt))
		{
			var card = CreateSoundCard(sound);
			SoundsContainer.Children.Add(card);
		}
	}

	private Border CreateSoundCard(CustomSound sound)
	{
		var card = new Border
		{
			BackgroundColor = Color.FromArgb("#1A1A2E"),
			Stroke = new SolidColorBrush(Color.FromArgb("#3A3A50")),
			StrokeThickness = 1,
			Padding = new Thickness(18),
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) }
		};

		var grid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Auto)
			},
			ColumnSpacing = 12
		};

		// Sound info
		var infoStack = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
		infoStack.Children.Add(new Label
		{
			Text = sound.Name,
			FontSize = 17,
			FontAttributes = FontAttributes.Bold,
			TextColor = Colors.White
		});

		var detailText = $"Base: {sound.BasePreset}  |  B:{sound.Bass:+0;-0;0} M:{sound.Mid:+0;-0;0} T:{sound.Treble:+0;-0;0} G:{sound.Distortion:0}";
		infoStack.Children.Add(new Label
		{
			Text = detailText,
			FontSize = 11,
			TextColor = Color.FromArgb("#8E8E93")
		});

		grid.Children.Add(infoStack);
		Grid.SetColumn(infoStack, 0);

		// Play button
		var playBtn = new Border
		{
			StrokeThickness = 0,
			WidthRequest = 44,
			HeightRequest = 44,
			VerticalOptions = LayoutOptions.Center,
			Background = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 1),
				GradientStops =
				{
					new GradientStop(Color.FromArgb("#4CAF50"), 0f),
					new GradientStop(Color.FromArgb("#45A049"), 1f)
				}
			},
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(22) }
		};
		playBtn.Content = new Label
		{
			Text = "\u25B6",
			FontSize = 18,
			FontAttributes = FontAttributes.Bold,
			TextColor = Colors.White,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center
		};

		var playTap = new TapGestureRecognizer();
		playTap.Tapped += async (s, e) => await ApplySound(sound);
		playBtn.GestureRecognizers.Add(playTap);

		grid.Children.Add(playBtn);
		Grid.SetColumn(playBtn, 1);

		// Delete button
		var deleteBtn = new Border
		{
			StrokeThickness = 1,
			Stroke = new SolidColorBrush(Color.FromArgb("#FF525266")),
			BackgroundColor = Colors.Transparent,
			WidthRequest = 44,
			HeightRequest = 44,
			VerticalOptions = LayoutOptions.Center,
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(22) }
		};
		deleteBtn.Content = new Label
		{
			Text = "\U0001F5D1",
			FontSize = 18,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			Opacity = 0.7
		};

		var deleteTap = new TapGestureRecognizer();
		deleteTap.Tapped += async (s, e) => await DeleteSound(sound);
		deleteBtn.GestureRecognizers.Add(deleteTap);

		grid.Children.Add(deleteBtn);
		Grid.SetColumn(deleteBtn, 2);

		card.Content = grid;
		return card;
	}

	private async Task ApplySound(CustomSound sound)
	{
		try
		{
			// Apply the base preset first
			_audioService.SetEffect(sound.BasePreset);

			// Apply the custom EQ + distortion
			_audioService.SetMasterEQ(sound.Bass, sound.Mid, sound.Treble);
			_audioService.SetMasterDistortion(sound.Distortion / 10f);

			// Navigate to editor with the values loaded
			var editor = new SoundEditorPage(_audioService, sound.BasePreset, sound.Name);
			editor.LoadCustomValues(sound.Bass, sound.Mid, sound.Treble, sound.Distortion);
			await Navigation.PushAsync(editor);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Failed to apply sound: {ex.Message}", "OK");
		}
	}

	private async Task DeleteSound(CustomSound sound)
	{
		bool confirm = await DisplayAlert("Delete", $"Delete \"{sound.Name}\"?", "Delete", "Cancel");
		if (confirm)
		{
			CustomSoundService.Delete(sound.Name);
			LoadSounds();
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
