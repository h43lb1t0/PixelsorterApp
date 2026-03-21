using Microsoft.Maui.Controls;

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
}