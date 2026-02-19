using Microsoft.Maui.Controls.Shapes;

namespace BluetoothMicrophoneApp.UI;

public partial class SimpleCustomDialog : ContentView
{
    private TaskCompletionSource<bool>? _taskCompletionSource;

    public SimpleCustomDialog()
    {
        InitializeComponent();
    }

    public async Task<bool> ShowAsync(
        string title,
        string message,
        string? icon = null,
        string primaryButtonText = "OK",
        string? secondaryButtonText = null,
        List<string>? bulletPoints = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleCustomDialog] ShowAsync called - Title: {title}");

            _taskCompletionSource = new TaskCompletionSource<bool>();

            // Set content on UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Setting UI content");

                TitleLabel.Text = title;
                MessageLabel.Text = message;

                if (!string.IsNullOrEmpty(icon))
                {
                    IconLabel.Text = icon;
                }

                // Setup buttons
                PrimaryButton.Text = primaryButtonText;

                if (!string.IsNullOrEmpty(secondaryButtonText))
                {
                    SecondaryButton.Text = secondaryButtonText;
                    SecondaryButtonBorder.IsVisible = true;

                    ButtonContainer.ColumnDefinitions.Clear();
                    ButtonContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    ButtonContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    Grid.SetColumn(PrimaryButton.Parent as Border, 0);
                    Grid.SetColumn(SecondaryButtonBorder, 1);
                }
                else
                {
                    SecondaryButtonBorder.IsVisible = false;
                    ButtonContainer.ColumnDefinitions.Clear();
                    ButtonContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(PrimaryButton.Parent as Border, 0);
                    Grid.SetColumnSpan(PrimaryButton.Parent as Border, 2);
                }

                // Add bullet points if provided
                if (bulletPoints != null && bulletPoints.Any())
                {
                    BulletPointsContainer.Clear();
                    BulletPointsContainer.IsVisible = true;

                    foreach (var point in bulletPoints)
                    {
                        var border = new Border
                        {
                            BackgroundColor = Color.FromArgb("#1E1E38"),
                            Stroke = Color.FromArgb("#4A90E2"),
                            StrokeThickness = 1,
                            Padding = 12,
                            Margin = new Thickness(0, 0, 0, 8),
                            StrokeShape = new RoundRectangle { CornerRadius = 10 }
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
                        border.Content = grid;

                        BulletPointsContainer.Add(border);
                    }
                }
                else
                {
                    BulletPointsContainer.IsVisible = false;
                }

                System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] UI content set, showing overlay");
            });

            // Show with animation
            await ShowWithAnimationAsync();

            System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Waiting for user interaction");
            var result = await _taskCompletionSource.Task;
            System.Diagnostics.Debug.WriteLine($"[SimpleCustomDialog] User interaction complete: {result}");

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleCustomDialog] ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SimpleCustomDialog] Stack: {ex.StackTrace}");
            throw;
        }
    }

    private async Task ShowWithAnimationAsync()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Starting show animation");

                // Make visible immediately
                OverlayGrid.IsVisible = true;
                OverlayGrid.Opacity = 0;
                DialogBorder.Opacity = 0;
                DialogBorder.Scale = 0.9;

                // Animate in
                var overlayTask = OverlayGrid.FadeTo(1, 200, Easing.CubicOut);
                var dialogFadeTask = DialogBorder.FadeTo(1, 250, Easing.CubicOut);
                var dialogScaleTask = DialogBorder.ScaleTo(1, 250, Easing.CubicOut);

                await Task.WhenAll(overlayTask, dialogFadeTask, dialogScaleTask);

                System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Show animation complete");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleCustomDialog] Animation error: {ex.Message}");
        }
    }

    private async Task HideWithAnimationAsync()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Starting hide animation");

                // Animate out
                var overlayTask = OverlayGrid.FadeTo(0, 150, Easing.CubicIn);
                var dialogFadeTask = DialogBorder.FadeTo(0, 200, Easing.CubicIn);
                var dialogScaleTask = DialogBorder.ScaleTo(0.95, 200, Easing.CubicIn);

                await Task.WhenAll(overlayTask, dialogFadeTask, dialogScaleTask);

                OverlayGrid.IsVisible = false;

                System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Hide animation complete");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleCustomDialog] Hide animation error: {ex.Message}");
        }
    }

    private async void OnPrimaryButtonClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Primary button clicked");
        await CloseAsync(true);
    }

    private async void OnSecondaryButtonClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Secondary button clicked");
        await CloseAsync(false);
    }

    private async void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Overlay tapped - ignoring");
        // Don't close on overlay tap - force user to use buttons
    }

    private async Task CloseAsync(bool result)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleCustomDialog] CloseAsync called with result: {result}");

            // Animate out
            await HideWithAnimationAsync();

            // Set result
            _taskCompletionSource?.TrySetResult(result);

            System.Diagnostics.Debug.WriteLine("[SimpleCustomDialog] Dialog closed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleCustomDialog] Close error: {ex.Message}");
            _taskCompletionSource?.TrySetResult(false);
        }
    }
}
