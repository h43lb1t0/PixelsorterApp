namespace PixelsorterApp.Extensions;

public static class BorderAnimationExtensions
{
    public static Task<bool> StrokeColorTo(this Border border, Color targetColor, uint rate = 16, uint length = 250, Easing? easing = null)
    {
        ArgumentNullException.ThrowIfNull(border);
        ArgumentNullException.ThrowIfNull(targetColor);

        var tcs = new TaskCompletionSource<bool>();

        // Get the starting color (defaults to Transparent if null or not a SolidColorBrush)
        Color startColor = (border.Stroke as SolidColorBrush)?.Color ?? Colors.Transparent;

        var animation = new Animation(v =>
        {
            float r = (float)(startColor.Red + v * (targetColor.Red - startColor.Red));
            float g = (float)(startColor.Green + v * (targetColor.Green - startColor.Green));
            float b = (float)(startColor.Blue + v * (targetColor.Blue - startColor.Blue));
            float a = (float)(startColor.Alpha + v * (targetColor.Alpha - startColor.Alpha));

            border.Stroke = new SolidColorBrush(Color.FromRgba(r, g, b, a));
        }, 0, 1, easing);

        // "StrokeColorAnimation" acts as a handle to prevent overlapping animations on the same property
        animation.Commit(border, "StrokeColorAnimation", rate, length, finished: (v, c) => tcs.SetResult(c));

        return tcs.Task;
    }

    /// <summary>
    /// Plays a subtle completion animation: a crimson border flash and a micro-scale pulse.
    /// Best used on image cards or content containers to acknowledge a successful operation.
    /// </summary>
    public static async Task PlaySuccessPulseAsync(this Border border)
    {
        // Cancel any lingering loading border animation
        border.AbortAnimation("StrokeColorAnimation");

        var app = Application.Current;
        if (app == null) return;

        var primaryColor = app.RequestedTheme == AppTheme.Dark
            ? (Color)app.Resources["PrimaryDark"]
            : (Color)app.Resources["Primary"];

        var restingColor = app.RequestedTheme == AppTheme.Dark
            ? (Color)app.Resources["SurfaceDark"]
            : (Color)app.Resources["SurfaceLight"];

        // Subtle scale pulse — the element "lands" into place
        _ = border.ScaleToAsync(1.015, 200, Easing.CubicOut);

        // Crimson border flash — the product's color acknowledging "done"
        await border.StrokeColorTo(primaryColor, rate: 16, length: 280, easing: Easing.CubicOut);

        // Scale back down with spring ease
        _ = border.ScaleToAsync(1.0, 300, Easing.SpringOut);

        // Fade border back to resting color
        await border.StrokeColorTo(restingColor, rate: 16, length: 450, easing: Easing.CubicIn);

        // Restore the theme-aware binding so theme switches still work
        border.SetAppTheme<Brush>(
            Border.StrokeProperty,
            new SolidColorBrush((Color)app.Resources["DividerLight"]),
            new SolidColorBrush((Color)app.Resources["DividerDark"]));
    }
}