namespace PixelsorterApp.Views;

public partial class PulsingDotsView : ContentView
{
    // ── Bindable property ───────────────────────────────────────────────────

    /// <summary>
    /// Controls whether the dots are actively animating.
    /// Set to <c>true</c> when a sort operation starts, <c>false</c> when it ends.
    /// </summary>
    public static readonly BindableProperty IsAnimatingProperty =
        BindableProperty.Create(
            nameof(IsAnimating),
            typeof(bool),
            typeof(PulsingDotsView),
            defaultValue: false,
            propertyChanged: OnIsAnimatingChanged);

    public bool IsAnimating
    {
        get => (bool)GetValue(IsAnimatingProperty);
        set => SetValue(IsAnimatingProperty, value);
    }

    private static void OnIsAnimatingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (PulsingDotsView)bindable;
        if ((bool)newValue)
            view.StartPulsing();
        else
            view.StopPulsing();
    }

    // ── State ───────────────────────────────────────────────────────────────

    private CancellationTokenSource? _cts;

    /// <summary>
    /// True when the system accessibility "reduce motion" preference is on.
    /// MAUI does not expose this cross-platform, so we use a conservative heuristic:
    /// the non-animated path is also shorter and cleaner for low-end devices.
    /// </summary>
    private static bool ReduceMotion =>
#if ANDROID
        Android.Provider.Settings.Global.GetFloat(
            Android.App.Application.Context.ContentResolver,
            Android.Provider.Settings.Global.TransitionAnimationScale, 1f) == 0f;
#else
        false;
#endif

    // ── Lifecycle ───────────────────────────────────────────────────────────

    public PulsingDotsView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        // If the view is detached, always stop.
        if (Handler is null)
            StopPulsing();
        // Do NOT auto-start on attach — wait for IsAnimating=true.
    }

    // ── Control ─────────────────────────────────────────────────────────────

    /// <summary>Starts the wave pulse animation. Cancels any running loop first.</summary>
    public void StartPulsing()
    {
        StopPulsing();
        _cts = new CancellationTokenSource();
        _ = ReduceMotion
            ? RunReducedMotionLoop(_cts.Token)
            : RunBounceWaveLoop(_cts.Token);
    }

    /// <summary>Stops the animation and resets all dots to their resting state.</summary>
    public void StopPulsing()
    {
        _cts?.Cancel();
        _cts = null;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Dot1.Opacity = 0.3; Dot1.TranslationY = 0; Dot1.Scale = 1;
            Dot2.Opacity = 0.3; Dot2.TranslationY = 0; Dot2.Scale = 1;
            Dot3.Opacity = 0.3; Dot3.TranslationY = 0; Dot3.Scale = 1;
        });
    }

    // ── Animation loops ─────────────────────────────────────────────────────

    /// <summary>
    /// Full motion: three dots bounce upward with staggered starts and simultaneously
    /// fade from dim → bright → dim, creating a pixel-sorting wave.
    /// Each dot is 120 ms behind the previous; total cycle ≈ 900 ms.
    /// </summary>
    private async Task RunBounceWaveLoop(CancellationToken token)
    {
        const int stagger = 120;       // ms between each dot's phase start
        const uint halfCycle = 280;    // ms per up or down stroke
        const double lift = -7;        // dp, upward = negative Y in MAUI

        try
        {
            // Initial stagger offsets so the loop starts mid-wave immediately.
            await Task.WhenAll(
                AnimateDotAsync(Dot1, halfCycle, lift, 0, token),
                AnimateDotAsync(Dot2, halfCycle, lift, stagger, token),
                AnimateDotAsync(Dot3, halfCycle, lift, stagger * 2, token)
            );
        }
        catch (TaskCanceledException) { return; }

        // Continuous in-phase loop after the stagger warmup.
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.WhenAll(
                    AnimateDotAsync(Dot1, halfCycle, lift, 0, token),
                    AnimateDotAsync(Dot2, halfCycle, lift, stagger, token),
                    AnimateDotAsync(Dot3, halfCycle, lift, stagger * 2, token)
                );

                // Rest beat — all three dims before the next wave.
                await Task.Delay(stagger * 2, token);
            }
            catch (TaskCanceledException) { break; }
        }
    }

    /// <summary>
    /// One full up-down bounce + fade cycle for a single dot, with an optional
    /// <paramref name="delayMs"/> before starting.
    /// </summary>
    private static async Task AnimateDotAsync(
        View dot, uint halfCycle, double lift, int delayMs, CancellationToken token)
    {
        if (delayMs > 0)
            await Task.Delay(delayMs, token);

        if (token.IsCancellationRequested) return;

        // Up stroke: brighten + lift + grow.
        await Task.WhenAll(
            dot.FadeToAsync(1.0, halfCycle, Easing.SinOut),
            dot.TranslateToAsync(0, lift, halfCycle, Easing.SinOut),
            dot.ScaleToAsync(1.25, halfCycle, Easing.SinOut)
        );

        if (token.IsCancellationRequested) return;

        // Down stroke: dim + return + shrink.
        await Task.WhenAll(
            dot.FadeToAsync(0.3, halfCycle, Easing.SinIn),
            dot.TranslateToAsync(0, 0, halfCycle, Easing.SinIn),
            dot.ScaleToAsync(1.0, halfCycle, Easing.SinIn)
        );
    }

    /// <summary>
    /// Reduce-motion fallback: a slow, gentle opacity pulse on all three dots together.
    /// No translation, no scale.
    /// </summary>
    private async Task RunReducedMotionLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.WhenAll(
                    Dot1.FadeToAsync(1.0, 600, Easing.SinInOut),
                    Dot2.FadeToAsync(1.0, 600, Easing.SinInOut),
                    Dot3.FadeToAsync(1.0, 600, Easing.SinInOut)
                );
                await Task.WhenAll(
                    Dot1.FadeToAsync(0.3, 600, Easing.SinInOut),
                    Dot2.FadeToAsync(0.3, 600, Easing.SinInOut),
                    Dot3.FadeToAsync(0.3, 600, Easing.SinInOut)
                );
            }
            catch (TaskCanceledException) { break; }
        }
    }
}