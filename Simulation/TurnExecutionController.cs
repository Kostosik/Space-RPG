
namespace GameProject.Simulation;

public sealed class TurnExecutionController : IDisposable
{
    private readonly GameTime _time;
    private readonly Func<bool> _hasPendingOrders;
    private bool _holdToAdvance;
    private bool _stopAtEndOfDay;
    private bool _disposed;

    public TurnExecutionController(
        GameTime time,
        Func<bool> hasPendingOrders)
    {
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(hasPendingOrders);

        _time = time;
        _hasPendingOrders = hasPendingOrders;

        _time.DayCompleted += OnDayCompleted;
    }

    public void BeginHoldToAdvance()
    {
        if (_disposed)
            return;

        _holdToAdvance = true;
        _stopAtEndOfDay = false;

        if (_time.IsPaused)
            _time.Resume();
    }

    public void EndHoldToAdvance()
    {
        if (_disposed)
            return;

        _holdToAdvance = false;

        if (!_time.IsPaused)
            _stopAtEndOfDay = true;
    }

    public void CancelHoldToAdvance()
    {
        if (_disposed)
            return;

        _holdToAdvance = false;
    }

    public void PressEndTurn(bool shiftHeld)
    {
        if (_disposed)
            return;

        if (_time.IsPaused)
        {
            // Shift + Space: выполнить ровно один день.
            _stopAtEndOfDay = shiftHeld;
            _holdToAdvance = false;
            _time.Resume();
            return;
        }

        // Space во время перелёта:
        // остановиться после окончания текущего дня.
        _stopAtEndOfDay = true;
    }

    public void RequestStopAtEndOfDay()
    {
        if (!_time.IsPaused)
            _stopAtEndOfDay = true;
    }

    private void OnDayCompleted(GameDayCompleted completed)
    {
        if (_stopAtEndOfDay ||
            (!_holdToAdvance && !_hasPendingOrders()))
        {
            _time.Pause();
            _stopAtEndOfDay = false;
            _holdToAdvance = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _time.DayCompleted -= OnDayCompleted;
    }
}
