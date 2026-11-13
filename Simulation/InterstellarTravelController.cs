namespace GameProject.Simulation;

internal sealed class InterstellarTravelController : IDisposable
{
    private readonly GameTime _time;
    private TimeSpan? _arrivalTime;
    private bool _disposed;

    public InterstellarTravelController(GameTime time)
    {
        ArgumentNullException.ThrowIfNull(time);

        _time = time;
        _time.Advanced += OnTimeAdvanced;
    }

    public bool IsActive => _arrivalTime.HasValue;

    public bool IsArrivalReady { get; private set; }

    public void Begin(TimeSpan arrivalTime)
    {
        if (_disposed)
            throw new ObjectDisposedException(
                nameof(InterstellarTravelController));

        if (IsActive)
            throw new InvalidOperationException(
                "Interstellar travel is already active.");

        if (arrivalTime <= _time.Elapsed.Value)
            throw new ArgumentOutOfRangeException(
                nameof(arrivalTime));

        _arrivalTime = arrivalTime;
        IsArrivalReady = false;

        _time.SchedulePauseAt(arrivalTime);
        _time.Resume();
    }

    private void OnTimeAdvanced(GameTimeAdvanced advanced)
    {
        if (_arrivalTime.HasValue &&
            advanced.CurrentElapsed.Value >= _arrivalTime.Value)
        {
            IsArrivalReady = true;
        }
    }

    public void Complete()
    {
        _time.ClearScheduledPause();

        _arrivalTime = null;
        IsArrivalReady = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _time.Advanced -= OnTimeAdvanced;

        _arrivalTime = null;
        IsArrivalReady = false;
    }
}