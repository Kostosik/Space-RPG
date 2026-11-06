
using Engine.Core.Math;
using Engine.Core.Time;

namespace GameProject.Simulation;

public sealed class GameTime
{
    private const long TicksPerDay = TimeSpan.TicksPerDay;

    private Duration _elapsed = Duration.Zero;
    private decimal _fractionalTicks;

    // По умолчанию один игровой день длится 6 секунд реального времени.
    private TimeSpan _dayDuration = TimeSpan.FromSeconds(6);

    public Duration Elapsed => _elapsed;

    public TimeSpan DayDuration => _dayDuration;

    public Fixed32 SpeedMultiplier { get; private set; } =
        Fixed32.One;

    public bool IsPaused { get; private set; }

    public long DayNumber =>
        _elapsed.Value.Ticks / TicksPerDay + 1;

    // Прогресс текущих суток от 0 до 1.
    public double DayProgress =>
        (double)(_elapsed.Value.Ticks % TicksPerDay) /
        TicksPerDay;

    public Duration TimeOfDay =>
        new(TimeSpan.FromTicks(
            _elapsed.Value.Ticks % TicksPerDay));

    public event Action<GameTimeAdvanced>? Advanced;

    public event Action<GameDayCompleted>? DayCompleted;

    public void SetDayDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration));
        }

        _dayDuration = duration;
    }

    public void SetSpeedMultiplier(Fixed32 multiplier)
    {
        if (multiplier <= Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(multiplier));
        }

        SpeedMultiplier = multiplier;
    }

    public void Pause()
    {
        IsPaused = true;
    }

    public void Resume()
    {
        IsPaused = false;
    }

    public void Advance(SimulationTime simulationTime)
    {
        if (IsPaused)
            return;

        if (simulationTime.Delta < Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(simulationTime));
        }

        var scaledDelta =
            simulationTime.Delta * SpeedMultiplier;

        // Переводим реальное время в долю игрового дня.
        var elapsedTicks =
            (decimal)scaledDelta.ToFloat() *
            TicksPerDay /
            (decimal)_dayDuration.TotalSeconds;

        AdvanceFractional(elapsedTicks);
    }

    public void AdvanceBy(Duration interval)
    {
        if (interval.IsNegative)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval));
        }

        var remainingTicks = interval.Value.Ticks;

        while (remainingTicks > 0)
        {
            var ticksIntoDay =
                _elapsed.Value.Ticks % TicksPerDay;

            var ticksToBoundary =
                TicksPerDay - ticksIntoDay;

            var stepTicks = Math.Min(
                remainingTicks,
                ticksToBoundary);

            var step = new Duration(
                TimeSpan.FromTicks(stepTicks));

            var previous = _elapsed;

            _elapsed += step;
            remainingTicks -= stepTicks;

            // Сначала обновляются все подписчики времени.
            Advanced?.Invoke(
                new GameTimeAdvanced(
                    previous,
                    _elapsed,
                    step));

            // Затем обрабатывается граница игровых суток.
            if (stepTicks == ticksToBoundary)
            {
                DayCompleted?.Invoke(
                    new GameDayCompleted(
                        DayNumber - 1,
                        _elapsed));

                // Если выполнение хода потребовало паузы,
                // не продолжаем обрабатывать остаток интервала.
                if (IsPaused)
                    break;
            }
        }
    }

    private void AdvanceFractional(decimal ticks)
    {
        _fractionalTicks += ticks;

        var wholeTicks =
            decimal.ToInt64(
                decimal.Truncate(_fractionalTicks));

        if (wholeTicks <= 0)
            return;

        _fractionalTicks -= wholeTicks;

        AdvanceBy(
            new Duration(
                TimeSpan.FromTicks(wholeTicks)));
    }
}

public readonly record struct GameTimeAdvanced(
    Duration PreviousElapsed,
    Duration CurrentElapsed,
    Duration Delta);

public readonly record struct GameDayCompleted(
    long DayNumber,
    Duration Elapsed);
