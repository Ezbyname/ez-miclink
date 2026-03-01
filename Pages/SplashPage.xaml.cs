namespace BluetoothMicrophoneApp.Pages;

public partial class SplashPage : ContentPage
{
	public SplashPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// Wait for 4 seconds
		await Task.Delay(4000);

		// Navigate to main page using relative navigation (not absolute)
		await Shell.Current.GoToAsync("mainpage");
	}
}
