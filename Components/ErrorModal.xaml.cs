namespace BluetoothMicrophoneApp.Components;

public partial class ErrorModal : ContentView
{
	private TaskCompletionSource<bool>? _taskCompletionSource;

	public ErrorModal()
	{
		InitializeComponent();
	}

	/// <summary>
	/// Show the error modal with custom title and message
	/// </summary>
	/// <param name="title">Error title</param>
	/// <param name="message">User-friendly error message</param>
	/// <returns>True if "Try Again" was clicked, False if "Cancel" was clicked</returns>
	public async Task<bool> ShowAsync(string title, string message)
	{
		ErrorTitle.Text = title;
		ErrorMessage.Text = message;

		_taskCompletionSource = new TaskCompletionSource<bool>();

		// Show modal with fade + scale animation
		IsVisible = true;

		// Animate opacity and scale simultaneously
		var opacityAnimation = ModalContainer.FadeTo(1, 300, Easing.CubicOut);
		var scaleAnimation = ModalContainer.ScaleTo(1, 300, Easing.SpringOut);

		await Task.WhenAll(opacityAnimation, scaleAnimation);

		return await _taskCompletionSource.Task;
	}

	private async void OnTryAgainClicked(object? sender, EventArgs e)
	{
		await HideModalAsync();
		_taskCompletionSource?.SetResult(true);
	}

	private async void OnCancelClicked(object? sender, EventArgs e)
	{
		await HideModalAsync();
		_taskCompletionSource?.SetResult(false);
	}

	private async void OnOverlayTapped(object? sender, EventArgs e)
	{
		await HideModalAsync();
		_taskCompletionSource?.SetResult(false);
	}

	private async Task HideModalAsync()
	{
		// Hide modal with fade + scale animation
		var opacityAnimation = ModalContainer.FadeTo(0, 200, Easing.CubicIn);
		var scaleAnimation = ModalContainer.ScaleTo(0.8, 200, Easing.CubicIn);

		await Task.WhenAll(opacityAnimation, scaleAnimation);
		IsVisible = false;
	}
}
