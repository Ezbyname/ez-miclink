namespace BluetoothMicrophoneApp.UI;

public partial class TextInputDialog : ContentView
{
    private TaskCompletionSource<string?>? _taskCompletionSource;

    public TextInputDialog()
    {
        InitializeComponent();
    }

    public async Task<string?> ShowAsync(
        string title,
        string message,
        string? icon = null,
        string? placeholder = null,
        string? initialValue = null,
        int? maxLength = null,
        Keyboard? keyboard = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[TextInputDialog] ShowAsync called - Title: {title}");

            _taskCompletionSource = new TaskCompletionSource<string?>();

            // Set content on UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                System.Diagnostics.Debug.WriteLine("[TextInputDialog] Setting UI content");

                TitleLabel.Text = title;
                MessageLabel.Text = message;

                if (!string.IsNullOrEmpty(icon))
                {
                    IconLabel.Text = icon;
                }

                InputEntry.Placeholder = placeholder ?? "";
                InputEntry.Text = initialValue ?? "";
                InputEntry.MaxLength = maxLength ?? int.MaxValue;
                InputEntry.Keyboard = keyboard ?? Keyboard.Default;

                System.Diagnostics.Debug.WriteLine("[TextInputDialog] UI content set, showing overlay");
            });

            // Show with animation
            await ShowWithAnimationAsync();

            // Auto-focus the input field after animation completes
            await Task.Delay(350); // Wait for animation to complete
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                InputEntry.Focus();
                System.Diagnostics.Debug.WriteLine("[TextInputDialog] Input field focused, keyboard should appear");
            });

            System.Diagnostics.Debug.WriteLine("[TextInputDialog] Waiting for user input");
            var result = await _taskCompletionSource.Task;
            System.Diagnostics.Debug.WriteLine($"[TextInputDialog] User input complete: {result}");

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TextInputDialog] ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[TextInputDialog] Stack: {ex.StackTrace}");
            throw;
        }
    }

    private async Task ShowWithAnimationAsync()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                System.Diagnostics.Debug.WriteLine("[TextInputDialog] Starting show animation");

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

                System.Diagnostics.Debug.WriteLine("[TextInputDialog] Show animation complete");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TextInputDialog] Animation error: {ex.Message}");
        }
    }

    private async Task HideWithAnimationAsync()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                System.Diagnostics.Debug.WriteLine("[TextInputDialog] Starting hide animation");

                // Animate out
                var overlayTask = OverlayGrid.FadeTo(0, 150, Easing.CubicIn);
                var dialogFadeTask = DialogBorder.FadeTo(0, 200, Easing.CubicIn);
                var dialogScaleTask = DialogBorder.ScaleTo(0.95, 200, Easing.CubicIn);

                await Task.WhenAll(overlayTask, dialogFadeTask, dialogScaleTask);

                OverlayGrid.IsVisible = false;

                System.Diagnostics.Debug.WriteLine("[TextInputDialog] Hide animation complete");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TextInputDialog] Hide animation error: {ex.Message}");
        }
    }

    private async void OnOkButtonClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[TextInputDialog] OK button clicked");
        await CloseAsync(InputEntry.Text);
    }

    private async void OnCancelButtonClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[TextInputDialog] Cancel button clicked");
        await CloseAsync(null);
    }

    private async void OnInputCompleted(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[TextInputDialog] Input completed (Enter pressed)");
        await CloseAsync(InputEntry.Text);
    }

    private async void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[TextInputDialog] Overlay tapped - ignoring");
        // Don't close on overlay tap - force user to use buttons
    }

    private async Task CloseAsync(string? result)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[TextInputDialog] CloseAsync called with result: {result}");

            // Unfocus entry to dismiss keyboard
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                InputEntry.Unfocus();
            });

            // Small delay to let keyboard dismiss
            await Task.Delay(100);

            // Animate out
            await HideWithAnimationAsync();

            // Set result
            _taskCompletionSource?.TrySetResult(result);

            System.Diagnostics.Debug.WriteLine("[TextInputDialog] Dialog closed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TextInputDialog] Close error: {ex.Message}");
            _taskCompletionSource?.TrySetResult(null);
        }
    }
}
