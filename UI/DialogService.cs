namespace BluetoothMicrophoneApp.UI;

/// <summary>
/// Service for showing custom dialogs with consistent design
/// </summary>
public static class DialogService
{
    private static SimpleCustomDialog? _currentDialog;
    private static Grid? _rootGrid;

    /// <summary>
    /// Initialize the dialog service with the root grid
    /// </summary>
    public static void Initialize(Grid rootGrid)
    {
        _rootGrid = rootGrid;
    }

    /// <summary>
    /// Show an info dialog
    /// </summary>
    public static async Task ShowInfoAsync(string title, string message, List<string>? bulletPoints = null)
    {
        await ShowCustomDialogAsync(
            title: title,
            message: message,
            icon: DesignSystem.Icons.Info,
            primaryButtonText: "OK",
            bulletPoints: bulletPoints
        );
    }

    /// <summary>
    /// Show a success dialog
    /// </summary>
    public static async Task ShowSuccessAsync(string title, string message, List<string>? bulletPoints = null)
    {
        await ShowCustomDialogAsync(
            title: title,
            message: message,
            icon: "✓",
            primaryButtonText: "Great!",
            bulletPoints: bulletPoints
        );
    }

    /// <summary>
    /// Show an error dialog
    /// </summary>
    public static async Task ShowErrorAsync(string title, string message, List<string>? bulletPoints = null)
    {
        await ShowCustomDialogAsync(
            title: title,
            message: message,
            icon: DesignSystem.Icons.Error,
            primaryButtonText: "OK",
            bulletPoints: bulletPoints
        );
    }

    /// <summary>
    /// Show a warning dialog
    /// </summary>
    public static async Task ShowWarningAsync(string title, string message, List<string>? bulletPoints = null)
    {
        await ShowCustomDialogAsync(
            title: title,
            message: message,
            icon: DesignSystem.Icons.Warning,
            primaryButtonText: "OK",
            bulletPoints: bulletPoints
        );
    }

    /// <summary>
    /// Show a confirmation dialog (Yes/No)
    /// </summary>
    public static async Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string confirmText = "Yes",
        string cancelText = "No",
        List<string>? bulletPoints = null)
    {
        return await ShowCustomDialogAsync(
            title: title,
            message: message,
            icon: "❓",
            primaryButtonText: confirmText,
            secondaryButtonText: cancelText,
            bulletPoints: bulletPoints
        );
    }

    /// <summary>
    /// Show a connection dialog with Bluetooth icon
    /// </summary>
    public static async Task ShowConnectionDialogAsync(string title, string message, List<string>? bulletPoints = null)
    {
        await ShowCustomDialogAsync(
            title: title,
            message: message,
            icon: DesignSystem.Icons.BluetoothConnected,
            primaryButtonText: "OK",
            bulletPoints: bulletPoints
        );
    }

    /// <summary>
    /// Show a connection failed dialog with diagnostics option
    /// </summary>
    public static async Task<bool> ShowConnectionFailedAsync(string deviceName, List<string>? reasons = null)
    {
        var bulletPoints = reasons ?? new List<string>
        {
            "Device is not paired in Settings",
            "Device is turned off or out of range",
            "Device is connected to another phone",
            "Bluetooth is disabled"
        };

        return await ShowCustomDialogAsync(
            title: "Connection Failed",
            message: $"Could not connect to {deviceName}.",
            icon: DesignSystem.Icons.Error,
            primaryButtonText: "Run Diagnostics",
            secondaryButtonText: "Cancel",
            bulletPoints: bulletPoints
        );
    }

    /// <summary>
    /// Show custom dialog with full control
    /// </summary>
    public static async Task<bool> ShowCustomDialogAsync(
        string title,
        string message,
        string? icon = null,
        string primaryButtonText = "OK",
        string? secondaryButtonText = null,
        List<string>? bulletPoints = null)
    {
        if (_rootGrid == null)
        {
            throw new InvalidOperationException("DialogService not initialized. Call Initialize() first.");
        }

        System.Diagnostics.Debug.WriteLine("[DialogService] ShowCustomDialogAsync called");

        try
        {
            // Must be called on UI thread
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Close any existing dialog
                if (_currentDialog != null)
                {
                    System.Diagnostics.Debug.WriteLine("[DialogService] Removing existing dialog");
                    _rootGrid.Children.Remove(_currentDialog);
                    _currentDialog = null;
                    await Task.Delay(50); // Brief delay to let old dialog cleanup
                }

                // Create new dialog
                System.Diagnostics.Debug.WriteLine("[DialogService] Creating new SimpleCustomDialog");
                _currentDialog = new SimpleCustomDialog();

                // Add to root grid (will overlay everything)
                System.Diagnostics.Debug.WriteLine($"[DialogService] Adding dialog to RootGrid (grid has {_rootGrid.Children.Count} children)");
                _rootGrid.Children.Add(_currentDialog);
                System.Diagnostics.Debug.WriteLine($"[DialogService] Dialog added, grid now has {_rootGrid.Children.Count} children");

                // Show dialog and wait for result
                System.Diagnostics.Debug.WriteLine("[DialogService] Calling SimpleCustomDialog.ShowAsync");
                var result = await _currentDialog.ShowAsync(
                    title: title,
                    message: message,
                    icon: icon,
                    primaryButtonText: primaryButtonText,
                    secondaryButtonText: secondaryButtonText,
                    bulletPoints: bulletPoints
                );

                System.Diagnostics.Debug.WriteLine($"[DialogService] Dialog closed with result: {result}");

                // Clean up
                _rootGrid.Children.Remove(_currentDialog);
                _currentDialog = null;

                return result;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DialogService] ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DialogService] Stack: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Show devices found dialog
    /// </summary>
    public static async Task ShowDevicesFoundAsync(int deviceCount)
    {
        await ShowSuccessAsync(
            title: "Devices Found",
            message: $"Found {deviceCount} Bluetooth device(s).",
            bulletPoints: new List<string>
            {
                "Tap on a device to select it",
                "Then tap Connect to pair"
            }
        );
    }

    /// <summary>
    /// Show no devices dialog
    /// </summary>
    public static async Task ShowNoDevicesAsync()
    {
        await ShowWarningAsync(
            title: "No Devices Found",
            message: "No Bluetooth devices found nearby.",
            bulletPoints: new List<string>
            {
                "Make sure Bluetooth is enabled",
                "Device is paired in Android Settings",
                "Device is turned on and nearby",
                "Try moving closer to the device"
            }
        );
    }

    /// <summary>
    /// Show connected successfully dialog
    /// </summary>
    public static async Task ShowConnectedAsync(string deviceName)
    {
        await ShowCustomDialogAsync(
            title: "Connected",
            message: $"Successfully connected to {deviceName}.",
            icon: "✓",
            primaryButtonText: "Great!",
            bulletPoints: new List<string>
            {
                "You can now start audio amplification",
                "Adjust the gain slider as needed",
                "Speak into your microphone to test"
            }
        );
    }

    /// <summary>
    /// Show disconnected dialog
    /// </summary>
    public static async Task ShowDisconnectedAsync()
    {
        await ShowInfoAsync(
            title: "Disconnected",
            message: "Device disconnected successfully."
        );
    }
}
