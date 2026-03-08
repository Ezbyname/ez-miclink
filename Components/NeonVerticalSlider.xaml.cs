using System;
using Microsoft.Maui.Controls;

namespace BluetoothMicrophoneApp.Components;

public partial class NeonVerticalSlider : ContentView
{
	#region Bindable Properties

	public static readonly BindableProperty ValueProperty =
		BindableProperty.Create(nameof(Value), typeof(double), typeof(NeonVerticalSlider),
			0.0, BindingMode.TwoWay, propertyChanged: OnValueChanged);

	public static readonly BindableProperty MinimumProperty =
		BindableProperty.Create(nameof(Minimum), typeof(double), typeof(NeonVerticalSlider),
			-10.0, propertyChanged: OnRangeChanged);

	public static readonly BindableProperty MaximumProperty =
		BindableProperty.Create(nameof(Maximum), typeof(double), typeof(NeonVerticalSlider),
			10.0, propertyChanged: OnRangeChanged);

	public static readonly BindableProperty AccentColorProperty =
		BindableProperty.Create(nameof(AccentColor), typeof(Color), typeof(NeonVerticalSlider),
			Colors.White, propertyChanged: OnAccentColorChanged);

	public static readonly BindableProperty ParameterNameProperty =
		BindableProperty.Create(nameof(ParameterName), typeof(string), typeof(NeonVerticalSlider),
			string.Empty, propertyChanged: OnParameterNameChanged);

	public static readonly BindableProperty ShowValueLabelProperty =
		BindableProperty.Create(nameof(ShowValueLabel), typeof(bool), typeof(NeonVerticalSlider),
			true, propertyChanged: OnShowValueLabelChanged);

	#endregion

	#region Properties

	public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
	public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
	public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
	public Color AccentColor { get => (Color)GetValue(AccentColorProperty); set => SetValue(AccentColorProperty, value); }
	public string ParameterName { get => (string)GetValue(ParameterNameProperty); set => SetValue(ParameterNameProperty, value); }
	public bool ShowValueLabel { get => (bool)GetValue(ShowValueLabelProperty); set => SetValue(ShowValueLabelProperty, value); }

	#endregion

	#region Events

	public event EventHandler<ValueChangedEventArgs>? ValueChanged;
	public event EventHandler? DragStarted;
	public event EventHandler? DragCompleted;

	#endregion

	private const double TrackHeight = 180.0;
	private const double HandleHeight = 24.0;

	private bool _isDragging = false;
	private bool _panActive = false;
	private double _panStartValue;

	public NeonVerticalSlider()
	{
		InitializeComponent();

		var panGesture = new PanGestureRecognizer();
		panGesture.PanUpdated += OnPanUpdated;
		TouchOverlay.GestureRecognizers.Add(panGesture);

		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += OnTapped;
		TouchOverlay.GestureRecognizers.Add(tapGesture);

		UpdateVisuals();
	}

	#region Callbacks

	private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is NeonVerticalSlider s) s.UpdateVisuals();
	}

	private static void OnRangeChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is NeonVerticalSlider s)
		{
			s.UpdateScaleLabels();
			s.UpdateVisuals();
		}
	}

	private static void OnAccentColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is NeonVerticalSlider s && newValue is Color c)
		{
			s.ValueLabel.TextColor = c;
			s.FillBorder.Background = new SolidColorBrush(c);
			s.InnerHighlight.BackgroundColor = c;
		}
	}

	private static void OnParameterNameChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is NeonVerticalSlider s && newValue is string n)
			s.ParameterLabel.Text = n;
	}

	private static void OnShowValueLabelChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is NeonVerticalSlider s && newValue is bool v)
			s.ValueLabel.IsVisible = v;
	}

	#endregion

	#region Gestures

	private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		switch (e.StatusType)
		{
			case GestureStatus.Started:
				_isDragging = true;
				_panActive = false;
				_panStartValue = Value;
				DragStarted?.Invoke(this, EventArgs.Empty);
				break;

			case GestureStatus.Running:
				if (!_panActive && Math.Abs(e.TotalY) < 6) break;
				_panActive = true;

				double range = Maximum - Minimum;
				double delta = -(e.TotalY / TrackHeight) * range;
				double nv = Math.Clamp(Math.Round(_panStartValue + delta, 1), Minimum, Maximum);
				if (nv != Value)
				{
					double ov = Value;
					Value = nv;
					ValueChanged?.Invoke(this, new ValueChangedEventArgs(ov, nv));
				}
				break;

			case GestureStatus.Completed:
			case GestureStatus.Canceled:
				_isDragging = false;
				_panActive = false;
				DragCompleted?.Invoke(this, EventArgs.Empty);
				break;
		}
	}

	private void OnTapped(object? sender, TappedEventArgs e)
	{
		if (_panActive) return;
		var pt = e.GetPosition(TouchOverlay);
		if (pt == null) return;

		double mid = TrackHeight / 2.0;
		double step = pt.Value.Y < mid ? 1.0 : -1.0;
		double nv = Math.Clamp(Math.Round(Value + step, 1), Minimum, Maximum);
		if (nv != Value)
		{
			double ov = Value;
			Value = nv;
			ValueChanged?.Invoke(this, new ValueChangedEventArgs(ov, nv));
		}
	}

	#endregion

	#region Visuals

	private void UpdateScaleLabels()
	{
		double mid = (Maximum + Minimum) / 2.0;
		TopLabel.Text = Maximum.ToString("0");
		MidLabel.Text = mid.ToString("0");
		BottomLabel.Text = Minimum.ToString("0");
	}

	private void UpdateVisuals()
	{
		double range = Maximum - Minimum;
		if (range <= 0) return;

		double norm = (Value - Minimum) / range;

		// Fill height: grows from bottom via HeightRequest (this works reliably)
		double fillH = Math.Clamp(norm * TrackHeight, 0, TrackHeight);
		FillBorder.HeightRequest = fillH;

		// Handle button: VerticalOptions=End, pushed UP from bottom via Margin.Bottom
		// Same positioning mechanism as the fill bar (which we know works)
		double handleBottom = Math.Max(0, fillH - (HandleHeight / 2.0));
		// Cap so handle doesn't overflow the top
		handleBottom = Math.Min(handleBottom, TrackHeight - HandleHeight);
		HandleButton.Margin = new Thickness(0, 0, 0, handleBottom);

		// Value label
		ValueLabel.Text = Math.Round(Value, 1).ToString();

		System.Diagnostics.Debug.WriteLine(
			$"[Slider:{ParameterName}] Val={Value:F1} norm={norm:F2} fillH={fillH:F0} handleBot={handleBottom:F0}");
	}

	#endregion
}
