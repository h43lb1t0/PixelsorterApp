using Microsoft.Maui.Controls.Shapes;

namespace PixelsorterApp.Views;

public partial class BeforeAfterView : ContentView
{
	private const double HandleSize = 56;
	private const double DividerWidth = 2;

	private double dragStartSplit;

	public static readonly BindableProperty BeforeImageSourceProperty = BindableProperty.Create(
		nameof(BeforeImageSource),
		typeof(ImageSource),
		typeof(BeforeAfterView),
		default(ImageSource),
		propertyChanged: OnLayoutPropertyChanged);

	public static readonly BindableProperty AfterImageSourceProperty = BindableProperty.Create(
		nameof(AfterImageSource),
		typeof(ImageSource),
		typeof(BeforeAfterView),
		default(ImageSource),
		propertyChanged: OnLayoutPropertyChanged);

	public static readonly BindableProperty BeforeLabelProperty = BindableProperty.Create(
		nameof(BeforeLabel),
		typeof(string),
		typeof(BeforeAfterView),
		"BEFORE");

	public static readonly BindableProperty AfterLabelProperty = BindableProperty.Create(
		nameof(AfterLabel),
		typeof(string),
		typeof(BeforeAfterView),
		"AFTER");

	public static readonly BindableProperty SplitPositionProperty = BindableProperty.Create(
		nameof(SplitPosition),
		typeof(double),
		typeof(BeforeAfterView),
		0.5d,
		BindingMode.TwoWay,
		coerceValue: CoerceSplitPosition,
		propertyChanged: OnSplitPositionChanged);

	public BeforeAfterView()
	{
		InitializeComponent();
		ComparisonStage.SizeChanged += (_, _) => UpdateComparisonLayout();
		SizeChanged += (_, _) => UpdateComparisonLayout();
		Loaded += (_, _) => UpdateComparisonLayout();
	}

	public ImageSource? BeforeImageSource
	{
		get => (ImageSource?)GetValue(BeforeImageSourceProperty);
		set => SetValue(BeforeImageSourceProperty, value);
	}

	public ImageSource? AfterImageSource
	{
		get => (ImageSource?)GetValue(AfterImageSourceProperty);
		set => SetValue(AfterImageSourceProperty, value);
	}

	public string BeforeLabel
	{
		get => (string)GetValue(BeforeLabelProperty);
		set => SetValue(BeforeLabelProperty, value);
	}

	public string AfterLabel
	{
		get => (string)GetValue(AfterLabelProperty);
		set => SetValue(AfterLabelProperty, value);
	}

	public double SplitPosition
	{
		get => (double)GetValue(SplitPositionProperty);
		set => SetValue(SplitPositionProperty, value);
	}

	private static void OnLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((BeforeAfterView)bindable).UpdateComparisonLayout();
	}

	private static void OnSplitPositionChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((BeforeAfterView)bindable).UpdateComparisonLayout();
	}

	private static object CoerceSplitPosition(BindableObject bindable, object value)
	{
		var position = (double)value;
		return Math.Clamp(position, 0d, 1d);
	}

	private void OverlayCanvas_Tapped(object? sender, TappedEventArgs e)
	{
		var position = e.GetPosition(OverlayCanvas);
		if (position is null || ComparisonStage.Width <= 0)
		{
			return;
		}

		SplitPosition = position.Value.X / ComparisonStage.Width;
	}

	private void OverlayCanvas_PanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		if (ComparisonStage.Width <= 0)
		{
			return;
		}

		switch (e.StatusType)
		{
			case GestureStatus.Started:
				dragStartSplit = SplitPosition;
				break;
			case GestureStatus.Running:
				SplitPosition = (dragStartSplit * ComparisonStage.Width + e.TotalX) / ComparisonStage.Width;
				break;
		}
	}

	private void UpdateComparisonLayout()
	{
		if (ComparisonStage.Width <= 0 || ComparisonStage.Height <= 0)
		{
			return;
		}

		var splitX = ComparisonStage.Width * SplitPosition;
		var handleX = Math.Clamp(splitX - (HandleSize / 2), 0, Math.Max(0, ComparisonStage.Width - HandleSize));
		var handleY = Math.Clamp((ComparisonStage.Height - HandleSize) / 2, 0, Math.Max(0, ComparisonStage.Height - HandleSize));

		BeforeImage.Clip = new RectangleGeometry
		{
			Rect = new Rect(0, 0, splitX, ComparisonStage.Height)
		};

		AbsoluteLayout.SetLayoutBounds(
			DividerLine,
			new Rect(Math.Clamp(splitX - (DividerWidth / 2), 0, Math.Max(0, ComparisonStage.Width - DividerWidth)), 0, DividerWidth, ComparisonStage.Height));
		AbsoluteLayout.SetLayoutFlags(DividerLine, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);

		AbsoluteLayout.SetLayoutBounds(Handle, new Rect(handleX, handleY, HandleSize, HandleSize));
		AbsoluteLayout.SetLayoutFlags(Handle, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
	}
}
