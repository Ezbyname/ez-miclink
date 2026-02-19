using Microsoft.Maui.Controls.Shapes;

namespace BluetoothMicrophoneApp.UI;

public partial class BluetoothConnectionGraphic : ContentView
{
    private bool _isAnimating;

    public BluetoothConnectionGraphic()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        StartAnimations();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        StopAnimations();
    }

    public void StartAnimations()
    {
        _isAnimating = true;

        // Animate outer ring - slow pulse
        AnimateRing(OuterGlow, 1.0, 1.15, 2000);

        // Animate middle ring - medium pulse (offset timing)
        Task.Delay(500).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AnimateRing(MiddleGlow, 1.0, 1.12, 1800);
            });
        });

        // Animate inner ring - fast pulse (offset timing)
        Task.Delay(1000).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AnimateRing(InnerGlow, 1.0, 1.10, 1600);
            });
        });

        // Animate center icon - gentle pulse
        AnimateCenterIcon();

        // Animate connection waves
        AnimateWaves();
    }

    public void StopAnimations()
    {
        _isAnimating = false;
    }

    private async void AnimateRing(Ellipse ring, double fromScale, double toScale, uint duration)
    {
        while (_isAnimating)
        {
            await ring.ScaleTo(toScale, duration / 2, Easing.SinInOut);
            await ring.ScaleTo(fromScale, duration / 2, Easing.SinInOut);
        }
    }

    private async void AnimateCenterIcon()
    {
        while (_isAnimating)
        {
            await CenterIcon.ScaleTo(1.08, 1000, Easing.SinInOut);
            await CenterIcon.ScaleTo(1.0, 1000, Easing.SinInOut);
        }
    }

    private async void AnimateWaves()
    {
        while (_isAnimating)
        {
            // Left waves - fade in/out with slight movement
            var leftTasks = new[]
            {
                WaveLeft1.FadeTo(0.8, 800, Easing.SinInOut),
                WaveLeft2.FadeTo(0.7, 900, Easing.SinInOut)
            };
            await Task.WhenAll(leftTasks);

            var leftFadeOut = new[]
            {
                WaveLeft1.FadeTo(0.3, 800, Easing.SinInOut),
                WaveLeft2.FadeTo(0.2, 900, Easing.SinInOut)
            };
            await Task.WhenAll(leftFadeOut);

            // Right waves - fade in/out with slight movement (offset)
            await Task.Delay(200);

            var rightTasks = new[]
            {
                WaveRight1.FadeTo(0.8, 800, Easing.SinInOut),
                WaveRight2.FadeTo(0.7, 900, Easing.SinInOut)
            };
            await Task.WhenAll(rightTasks);

            var rightFadeOut = new[]
            {
                WaveRight1.FadeTo(0.3, 800, Easing.SinInOut),
                WaveRight2.FadeTo(0.2, 900, Easing.SinInOut)
            };
            await Task.WhenAll(rightFadeOut);
        }
    }
}
