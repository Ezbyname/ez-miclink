using System;
using Microsoft.Maui.Controls;

namespace BluetoothMicrophoneApp.Components;

/// <summary>
/// Reusable Neon Vertical Slider component with customizable gradient and accent color.
///
/// FEATURES:
/// - 5-layer visual structure (background, fill, ticks, labels, handle)
/// - GPU-accelerated handle positioning (TranslationY)
/// - Bindable properties for customization
/// - Drag feedback animations
/// - Neon glow effects
///
/// DESIGN PATTERN: Reusable Component
/// - Eliminates code duplication (600 lines → 150 lines per usage)
/// - Single source of truth for slider appearance
/// - Consistent behavior across app
///
/// USAGE:
/// <controls:NeonVerticalSlider
///     Value="{Binding ToneValue}"
///     Gradient="{StaticResource GradientOrange}"
///     AccentColor="#FFB347"
///     ParameterName="Tone"
///     ValueChanged="OnToneChanged" />
/// </summary>
public partial class NeonVerticalSlider : ContentView
{
    #region Bindable Properties

    /// <summary>
    /// Current value of the slider (-10 to +10 by default)
    /// </summary>
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(
            nameof(Value),
            typeof(double),
            typeof(NeonVerticalSlider),
            0.0,
            BindingMode.TwoWay,
            propertyChanged: OnValueChanged);

    /// <summary>
    /// Minimum value (default: -10)
    /// </summary>
    public static readonly BindableProperty MinimumProperty =
        BindableProperty.Create(
            nameof(Minimum),
            typeof(double),
            typeof(NeonVerticalSlider),
            -10.0,
            propertyChanged: OnRangeChanged);

    /// <summary>
    /// Maximum value (default: +10)
    /// </summary>
    public static readonly BindableProperty MaximumProperty =
        BindableProperty.Create(
            nameof(Maximum),
            typeof(double),
            typeof(NeonVerticalSlider),
            10.0,
            propertyChanged: OnRangeChanged);

    /// <summary>
    /// Gradient brush for the active fill
    /// </summary>
    public static readonly BindableProperty GradientProperty =
        BindableProperty.Create(
            nameof(Gradient),
            typeof(Brush),
            typeof(NeonVerticalSlider),
            null,
            propertyChanged: OnGradientChanged);

    /// <summary>
    /// Accent color for highlights and glows
    /// </summary>
    public static readonly BindableProperty AccentColorProperty =
        BindableProperty.Create(
            nameof(AccentColor),
            typeof(Color),
            typeof(NeonVerticalSlider),
            Colors.White,
            propertyChanged: OnAccentColorChanged);

    /// <summary>
    /// Parameter name label (shown below slider)
    /// </summary>
    public static readonly BindableProperty ParameterNameProperty =
        BindableProperty.Create(
            nameof(ParameterName),
            typeof(string),
            typeof(NeonVerticalSlider),
            string.Empty,
            propertyChanged: OnParameterNameChanged);

    /// <summary>
    /// Show or hide the value label at the top
    /// </summary>
    public static readonly BindableProperty ShowValueLabelProperty =
        BindableProperty.Create(
            nameof(ShowValueLabel),
            typeof(bool),
            typeof(NeonVerticalSlider),
            true,
            propertyChanged: OnShowValueLabelChanged);

    #endregion

    #region Properties

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public Brush Gradient
    {
        get => (Brush)GetValue(GradientProperty);
        set => SetValue(GradientProperty, value);
    }

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public string ParameterName
    {
        get => (string)GetValue(ParameterNameProperty);
        set => SetValue(ParameterNameProperty, value);
    }

    public bool ShowValueLabel
    {
        get => (bool)GetValue(ShowValueLabelProperty);
        set => SetValue(ShowValueLabelProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when the slider value changes
    /// </summary>
    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    /// <summary>
    /// Fired when user starts dragging
    /// </summary>
    public event EventHandler? DragStarted;

    /// <summary>
    /// Fired when user completes dragging
    /// </summary>
    public event EventHandler? DragCompleted;

    #endregion

    private bool _isDragging = false;

    public NeonVerticalSlider()
    {
        InitializeComponent();

        // Initialize internal slider range
        InvisibleSlider.Minimum = Minimum;
        InvisibleSlider.Maximum = Maximum;
        InvisibleSlider.Value = Value;

        // Initial update
        UpdateVisuals(false);
    }

    #region Property Changed Callbacks

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is NeonVerticalSlider slider)
        {
            slider.InvisibleSlider.Value = (double)newValue;
            slider.UpdateVisuals(slider._isDragging);
        }
    }

    private static void OnRangeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is NeonVerticalSlider slider)
        {
            slider.InvisibleSlider.Minimum = slider.Minimum;
            slider.InvisibleSlider.Maximum = slider.Maximum;
            slider.UpdateVisuals(slider._isDragging);
        }
    }

    private static void OnGradientChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is NeonVerticalSlider slider && newValue is Brush gradient)
        {
            slider.FillBorder.Background = gradient;
        }
    }

    private static void OnAccentColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is NeonVerticalSlider slider && newValue is Color color)
        {
            slider.ValueLabel.TextColor = color;
            slider.InnerHighlight.BackgroundColor = color;
            slider.FillShadow.Brush = color;
            slider.HighlightShadow.Brush = color;
        }
    }

    private static void OnParameterNameChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is NeonVerticalSlider slider && newValue is string name)
        {
            slider.ParameterLabel.Text = name;
        }
    }

    private static void OnShowValueLabelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is NeonVerticalSlider slider && newValue is bool show)
        {
            slider.ValueLabel.IsVisible = show;
        }
    }

    #endregion

    #region Event Handlers

    private void OnSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        Value = e.NewValue;
        UpdateVisuals(_isDragging);

        // Raise external event
        ValueChanged?.Invoke(this, e);
    }

    private void OnSliderDragStarted(object? sender, EventArgs e)
    {
        _isDragging = true;
        UpdateVisuals(true);

        // Raise external event
        DragStarted?.Invoke(this, e);
    }

    private void OnSliderDragCompleted(object? sender, EventArgs e)
    {
        _isDragging = false;
        UpdateVisuals(false);

        // Raise external event
        DragCompleted?.Invoke(this, e);
    }

    #endregion

    #region Visual Update Logic

    /// <summary>
    /// Update the slider visual appearance based on current value.
    ///
    /// GPU-ACCELERATED: Uses TranslationY for handle positioning (no layout recalculation).
    /// DRAG FEEDBACK: Scales handle and increases glow during drag.
    /// </summary>
    private void UpdateVisuals(bool isDragging)
    {
        double value = Value;
        double min = Minimum;
        double max = Maximum;
        double range = max - min;

        // Map value to fill height (0-180px)
        double normalizedValue = (value - min) / range; // 0.0 to 1.0
        double fillHeight = normalizedValue * 180.0;
        fillHeight = Math.Clamp(fillHeight, 0, 180);

        FillBorder.HeightRequest = fillHeight;

        // Map value to handle position (GPU-accelerated TranslationY)
        // Handle travels 138px (180px slider - 42px handle height)
        double normalizedInverted = (max - value) / range; // Invert for top-down positioning
        double handleTranslation = normalizedInverted * 138.0;
        handleTranslation = Math.Clamp(handleTranslation, 0, 138);

        HandleBorder.TranslationY = handleTranslation;

        // Update value label
        ValueLabel.Text = Math.Round(value, 1).ToString();

        // Apply drag feedback animations
        if (isDragging)
        {
            HandleBorder.Scale = 1.05;
            FillShadow.Opacity = 0.72f;
            HighlightShadow.Opacity = 0.96f;
        }
        else
        {
            HandleBorder.Scale = 1.0;
            FillShadow.Opacity = 0.6f;
            HighlightShadow.Opacity = 0.8f;
        }
    }

    #endregion
}
