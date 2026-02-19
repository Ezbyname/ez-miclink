using System.Collections.ObjectModel;

namespace BluetoothMicrophoneApp.UI;

public partial class CustomDialog : ContentView
{
    private TaskCompletionSource<bool>? _taskCompletionSource;

    public CustomDialog()
    {
        InitializeComponent();
        IsVisible = false;
    }

    public async Task<bool> ShowAsync(
        string title,
        string message,
        string? icon = null,
        string primaryButtonText = "OK",
        string? secondaryButtonText = null,
        List<string>? bulletPoints = null,
        bool showBluetoothGraphic = false)
    {
        System.Diagnostics.Debug.WriteLine("CustomDialog.ShowAsync START");
        _taskCompletionSource = new TaskCompletionSource<bool>();

        // Set content
        System.Diagnostics.Debug.WriteLine($"Setting title: {title}");
        TitleLabel.Text = title;
        MessageLabel.Text = message;

        if (!string.IsNullOrEmpty(icon))
        {
            IconLabel.Text = icon;
        }

        // Show/hide Bluetooth graphic
        BluetoothGraphic.IsVisible = showBluetoothGraphic;
        if (showBluetoothGraphic)
        {
            BluetoothGraphic.StartAnimations();
        }

        // Setup buttons
        PrimaryButton.Text = primaryButtonText;

        if (!string.IsNullOrEmpty(secondaryButtonText))
        {
            SecondaryButton.Text = secondaryButtonText;
            SecondaryButton.IsVisible = true;
            ButtonContainer.ColumnDefinitions.Clear();
            ButtonContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ButtonContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(PrimaryButton, 0);
            Grid.SetColumn(SecondaryButton, 1);
        }
        else
        {
            SecondaryButton.IsVisible = false;
            ButtonContainer.ColumnDefinitions.Clear();
            ButtonContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(PrimaryButton, 0);
            Grid.SetColumnSpan(PrimaryButton, 2);
        }

        // Add bullet points if provided
        if (bulletPoints != null && bulletPoints.Any())
        {
            BulletPointsContainer.Clear();
            BulletPointsContainer.IsVisible = true;

            foreach (var point in bulletPoints)
            {
                var bulletFrame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#1E1E38"),
                    BorderColor = Color.FromArgb("#4A90E2"),
                    CornerRadius = 10,
                    Padding = 12,
                    HasShadow = false
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

                var checkmark = new Label
                {
                    Text = "✓",
                    TextColor = Color.FromArgb("#4CAF50"),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Start,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                var text = new Label
                {
                    Text = point,
                    TextColor = Color.FromArgb("#CCCCCC"),
                    FontSize = 14,
                    LineHeight = 1.3
                };

                Grid.SetColumn(checkmark, 0);
                Grid.SetColumn(text, 1);

                grid.Add(checkmark);
                grid.Add(text);
                bulletFrame.Content = grid;

                BulletPointsContainer.Add(bulletFrame);
            }
        }
        else
        {
            BulletPointsContainer.IsVisible = false;
        }

        // Show with animation
        System.Diagnostics.Debug.WriteLine("Making OverlayGrid visible");
        OverlayGrid.IsVisible = true;

        System.Diagnostics.Debug.WriteLine("Starting fade/scale animation");
        await Task.WhenAll(
            DialogFrame.FadeTo(1, 250, Easing.CubicOut),
            DialogFrame.ScaleTo(1, 250, Easing.CubicOut)
        );

        System.Diagnostics.Debug.WriteLine("Animation complete, waiting for user interaction");
        return await _taskCompletionSource.Task;
    }

    private async void OnPrimaryButtonClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Primary button clicked");
        await CloseAsync(true);
    }

    private async void OnSecondaryButtonClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Secondary button clicked");
        await CloseAsync(false);
    }

    private async void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        // Don't close on overlay tap - force user to use buttons
        // await CloseAsync(false);
    }

    private async Task CloseAsync(bool result)
    {
        System.Diagnostics.Debug.WriteLine($"CloseAsync called with result: {result}");

        // Animate out
        await Task.WhenAll(
            DialogFrame.FadeTo(0, 200, Easing.CubicIn),
            DialogFrame.ScaleTo(0.9, 200, Easing.CubicIn)
        );

        System.Diagnostics.Debug.WriteLine("Close animation complete");
        OverlayGrid.IsVisible = false;
        _taskCompletionSource?.SetResult(result);
        System.Diagnostics.Debug.WriteLine("TaskCompletionSource result set");
    }
}
