namespace PixelsorterApp.Views;

public partial class PulsingDotsView : ContentView
{
    private CancellationTokenSource? _cts;

    public PulsingDotsView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null)
            StartPulsing();
        else
            StopPulsing();
    }

    private void StartPulsing()
    {
        StopPulsing();
        _cts = new CancellationTokenSource();
        _ = RunPulseLoop(_cts.Token);
    }

    private void StopPulsing()
    {
        _cts?.Cancel();
        _cts = null;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Dot1.Opacity = 0.3;
            Dot2.Opacity = 0.3;
            Dot3.Opacity = 0.3;
        });
    }

    /// <summary>
    /// Sequentially pulses each dot with a staggered delay to create a flowing animation.
    /// </summary>
    private async Task RunPulseLoop(CancellationToken token)
    {
        const uint pulseDuration = 350;
        const int cycleDelay = 200;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await PulseDot(Dot1, pulseDuration, token);
                await Task.Delay(cycleDelay, token);

                await PulseDot(Dot2, pulseDuration, token);
                await Task.Delay(cycleDelay, token);

                await PulseDot(Dot3, pulseDuration, token);
                await Task.Delay(cycleDelay * 3, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private static async Task PulseDot(View dot, uint duration, CancellationToken token)
    {
        if (token.IsCancellationRequested) return;
        await dot.FadeToAsync(1.0, duration / 2, Easing.SinInOut);
        if (token.IsCancellationRequested) return;
        await dot.FadeToAsync(0.3, duration / 2, Easing.SinInOut);
    }
}
