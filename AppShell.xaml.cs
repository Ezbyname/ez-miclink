using BluetoothMicrophoneApp.UI;

namespace BluetoothMicrophoneApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for navigation
		Routing.RegisterRoute("visualizer", typeof(VisualizerPage));
		Routing.RegisterRoute("effects", typeof(EffectsPage));
	}
}
