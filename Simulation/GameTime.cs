using Engine.Core.Math;
using Engine.Core.Time;

namespace GameProject.Simulation;

public sealed class GameTime
{
    private Duration _elapsed = Duration.Zero;
    private decimal _fractionalTicks;

    public Duration Elapsed => _elapsed;

    public Fixed32 SpeedMultiplier { get; private set; } =
        Fixed32.One;

    public bool IsPaused { get; private set; }

    public long DayNumber =>
        _elapsed.Value.Ticks / TimeSpan.TicksPerDay + 1;

    public Duration TimeOfDay =>
        new(
            TimeSpan.FromTicks(
                _elapsed.Value.Ticks % TimeSpan.TicksPerDay));

    public event Action<GameTimeAdvanced>? Advanced;

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
        {
            return;
        }

        if (simulationTime.Delta < Fixed32.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(simulationTime));
        }

        var scaledDelta =
            simulationTime.Delta * SpeedMultiplier;

        var elapsedTicks =
            (decimal)scaledDelta.ToFloat()
            * TimeSpan.TicksPerSecond;

        AdvanceFractional(elapsedTicks);
    }

    public void AdvanceBy(Duration interval)
    {
        if (interval.IsNegative)
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval));
        }

        if (interval.IsZero)
        {
            return;
        }

        var previous = _elapsed;

        _elapsed += interval;

        Advanced?.Invoke(
            new GameTimeAdvanced(
                previous,
                _elapsed,
                interval));
    }


    public void CompleteDay()
    {
        if (IsPaused)
            return;

        // Сначала останавливаем игровое время.
        Pause();

        // Определяем остаток времени до конца суток.
        var elapsedTicks = _elapsed.Value.Ticks;

        var ticksIntoDay =
            elapsedTicks % TimeSpan.TicksPerDay;

        var ticksToNextDay =
            TimeSpan.TicksPerDay - ticksIntoDay;

        // Убираем накопленную дробную часть.
        _fractionalTicks = 0m;

        // Доводим игровые часы до конца текущего дня.
        // AdvanceBy также вызовет событие Advanced.
        AdvanceBy(
            new Duration(
                TimeSpan.FromTicks(ticksToNextDay)));
    }


    private void AdvanceFractional(decimal ticks)
    {
        _fractionalTicks += ticks;

        var wholeTicks =
            decimal.ToInt64(
                decimal.Truncate(_fractionalTicks));

        if (wholeTicks <= 0)
        {
            return;
        }

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