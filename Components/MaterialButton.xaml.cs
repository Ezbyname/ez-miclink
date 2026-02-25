using BluetoothMicrophoneApp.Themes;

namespace BluetoothMicrophoneApp.Components;

/// <summary>
/// Material Design 3 Button Component
/// Supports multiple button variants: Filled, FilledTonal, Outlined, Text
/// Ensures minimum 48dp touch target as per Material Design guidelines
/// </summary>
public partial class MaterialButton : Border
{
    public enum ButtonVariant
    {
        Filled,        // Primary action - filled background
        FilledTonal,   // Secondary action - subtle background
        Outlined,      // Medium emphasis - border only
        Text           // Low emphasis - text only
    }

    // Bindable properties
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(MaterialButton), "Button", propertyChanged: OnTextChanged);

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(nameof(Variant), typeof(ButtonVariant), typeof(MaterialButton), ButtonVariant.Filled, propertyChanged: OnVariantChanged);

    public static readonly BindableProperty IsEnabledButtonProperty =
        BindableProperty.Create(nameof(IsEnabledButton), typeof(bool), typeof(MaterialButton), true, propertyChanged: OnEnabledChanged);

    public static readonly BindableProperty AccentColorProperty =
        BindableProperty.Create(nameof(AccentColor), typeof(Color), typeof(MaterialButton), MaterialColors.Primary);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ButtonVariant Variant
    {
        get => (ButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool IsEnabledButton
    {
        get => (bool)GetValue(IsEnabledButtonProperty);
        set => SetValue(IsEnabledButtonProperty, value);
    }

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    // Events
    public event EventHandler? Clicked;

    public MaterialButton()
    {
        InitializeComponent();

        // Add tap gesture
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnButtonTapped;
        GestureRecognizers.Add(tapGesture);

        // Set accessibility
        AutomationProperties.SetIsInAccessibleTree(this, true);
        AutomationProperties.SetName(this, Text);

        // Apply initial variant
        ApplyVariant(Variant);
    }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaterialButton button)
        {
            button.ButtonLabel.Text = (string)newValue;
            AutomationProperties.SetName(button, (string)newValue);
        }
    }

    private static void OnVariantChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaterialButton button)
        {
            button.ApplyVariant((ButtonVariant)newValue);
        }
    }

    private static void OnEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaterialButton button)
        {
            button.Opacity = (bool)newValue ? 1.0 : 0.38;
        }
    }

    private void ApplyVariant(ButtonVariant variant)
    {
        switch (variant)
        {
            case ButtonVariant.Filled:
                // Primary filled button
                BackgroundColor = AccentColor;
                Stroke = Colors.Transparent;
                StrokeThickness = 0;
                ButtonLabel.TextColor = MaterialColors.OnPrimary;
                break;

            case ButtonVariant.FilledTonal:
                // Secondary filled button with subtle background
                BackgroundColor = MaterialColors.GetContainerColor(AccentColor);
                Stroke = Colors.Transparent;
                StrokeThickness = 0;
                ButtonLabel.TextColor = AccentColor;
                break;

            case ButtonVariant.Outlined:
                // Outlined button
                BackgroundColor = Colors.Transparent;
                Stroke = new SolidColorBrush(MaterialColors.GetBorderColor(AccentColor));
                StrokeThickness = 1;
                ButtonLabel.TextColor = AccentColor;
                break;

            case ButtonVariant.Text:
                // Text button (no background, no border)
                BackgroundColor = Colors.Transparent;
                Stroke = Colors.Transparent;
                StrokeThickness = 0;
                ButtonLabel.TextColor = AccentColor;
                Padding = new Thickness(12, 8);
                break;
        }
    }

    private async void OnButtonTapped(object? sender, EventArgs e)
    {
        if (!IsEnabledButton) return;

        // Material Design press animation (scale 0.98)
        await this.ScaleTo(0.98, 50, Easing.CubicOut);

        // Add ripple effect simulation (brightness change)
        if (Variant == ButtonVariant.Filled || Variant == ButtonVariant.FilledTonal)
        {
            var originalBackground = BackgroundColor;
            BackgroundColor = AccentColor.WithAlpha(0.8f);
            await Task.Delay(100);
            BackgroundColor = originalBackground;
        }
        else
        {
            var originalOpacity = Opacity;
            Opacity = 0.7;
            await Task.Delay(100);
            Opacity = originalOpacity;
        }

        // Restore scale
        await this.ScaleTo(1.0, 150, Easing.CubicOut);

        // Fire clicked event
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Set custom icon (emoji or text) before button text
    /// </summary>
    public void SetIcon(string icon)
    {
        ButtonLabel.Text = $"{icon} {Text}";
    }
}
