public class BusyScope : IDisposable
{
    private readonly Action _onComplete;

    public BusyScope(Action onStart, Action onComplete)
    {
        // Run the setup code immediately
        onStart?.Invoke();


        // Save the teardown code for later
        _onComplete = onComplete;
    }

    public void Dispose()
    {
        // Run the teardown code when the using block ends
        _onComplete?.Invoke();
    }
}