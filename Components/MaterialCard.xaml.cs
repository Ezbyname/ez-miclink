using BluetoothMicrophoneApp.Themes;

namespace BluetoothMicrophoneApp.Components;

/// <summary>
/// Material Design 3 Card Component
/// Follows Material Design elevation and surface guidelines
/// Supports different elevation levels and press states
/// </summary>
public partial class MaterialCard : Border
{
    // Bindable properties for customization
    public static readonly BindableProperty ElevationLevelProperty =
        BindableProperty.Create(nameof(ElevationLevel), typeof(int), typeof(MaterialCard), 1, propertyChanged: OnElevationChanged);

    public static readonly BindableProperty IsClickableProperty =
        BindableProperty.Create(nameof(IsClickable), typeof(bool), typeof(MaterialCard), false, propertyChanged: OnClickableChanged);

    public static readonly BindableProperty CornerRadiusValueProperty =
        BindableProperty.Create(nameof(CornerRadiusValue), typeof(double), typeof(MaterialCard), MaterialRadius.ExtraLarge);

    public int ElevationLevel
    {
        get => (int)GetValue(ElevationLevelProperty);
        set => SetValue(ElevationLevelProperty, value);
    }

    public bool IsClickable
    {
        get => (bool)GetValue(IsClickableProperty);
        set => SetValue(IsClickableProperty, value);
    }

    public double CornerRadiusValue
    {
        get => (double)GetValue(CornerRadiusValueProperty);
        set => SetValue(CornerRadiusValueProperty, value);
    }

    // Event for card taps
    public event EventHandler? Tapped;

    public MaterialCard()
    {
        InitializeComponent();
        ApplyElevation(ElevationLevel);
    }

    private static void OnElevationChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaterialCard card)
        {
            card.ApplyElevation((int)newValue);
        }
    }

    private static void OnClickableChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaterialCard card && (bool)newValue)
        {
            // Add tap gesture recognizer
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => card.OnCardTapped();
            card.GestureRecognizers.Add(tapGesture);
        }
    }

    private void ApplyElevation(int level)
    {
        var shadowOpacity = MaterialElevation.GetShadowOpacity(level);
        var shadowRadius = MaterialElevation.GetShadowRadius(level);

        if (Shadow != null)
        {
            Shadow.Opacity = (float)shadowOpacity;
            Shadow.Radius = (float)shadowRadius;
        }
    }

    private async void OnCardTapped()
    {
        if (!IsClickable) return;

        // Material Design press animation
        await this.ScaleTo(0.98, 50, Easing.CubicOut);

        // Intensify glow
        var originalStroke = Stroke;
        Stroke = new SolidColorBrush(MaterialColors.Primary.WithAlpha(0.5f));

        await Task.Delay(100);

        // Restore
        await this.ScaleTo(1.0, 150, Easing.CubicOut);
        Stroke = originalStroke;

        // Fire tapped event
        Tapped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Set card accent color (for themed cards)
    /// </summary>
    public void SetAccentColor(Color accentColor)
    {
        Stroke = new SolidColorBrush(MaterialColors.GetBorderColor(accentColor));
        if (Shadow != null)
        {
            Shadow.Brush = new SolidColorBrush(MaterialColors.GetGlowColor(accentColor));
        }
    }
}
