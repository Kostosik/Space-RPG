using Engine.Core.Math;
using Engine.Core.Systems;
using Engine.ECS;
using Engine.ECS.Entities;
using Engine.Input;

namespace GameProject.Player;

public struct PlayerShipComponent
{
    public Vector2 Position;

    public float Rotation;

    public float VelocityX;

    public float VelocityY;

    public float MaxSpeed;
}

public sealed class PlayerShipMovementSystem : IUpdateSystem
{
    private readonly Engine.ECS.World _world;
    private readonly EntityId _shipEntity;
    private readonly IInput _input;

    public PlayerShipMovementSystem(
        Engine.ECS.World world,
        EntityId shipEntity,
        IInput input)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(input);

        _world = world;
        _shipEntity = shipEntity;
        _input = input;
    }

    public void Update(SystemContext context)
    {
        if (!_world.Exists(_shipEntity))
        {
            return;
        }

        var delta =
            (float)context.Time.Delta.TotalSeconds;

        if (!float.IsFinite(delta) || delta <= 0.0f)
        {
            return;
        }

        delta = MathF.Min(delta, 0.05f);

        ref var ship =
            ref _world.Get<PlayerShipComponent>(_shipEntity);

        // Поворот корабля.
        var turn = 0.0f;

        if (_input.IsDown(ShipInputActions.TurnLeft))
            turn += 1.0f;

        if (_input.IsDown(ShipInputActions.TurnRight))
            turn -= 1.0f;

        ship.Rotation += turn * 2.8f * delta;

        // Ускорение и торможение.
        var thrust = 0.0f;

        if (_input.IsDown(ShipInputActions.Thrust))
            thrust += 1.0f;

        if (_input.IsDown(ShipInputActions.Reverse))
            thrust -= 0.55f;

        var directionX = MathF.Cos(ship.Rotation);
        var directionY = MathF.Sin(ship.Rotation);

        const float acceleration = 220.0f;

        ship.VelocityX +=
            directionX * thrust * acceleration * delta;

        ship.VelocityY +=
            directionY * thrust * acceleration * delta;

        // Сопротивление движения.
        var drag = MathF.Pow(0.12f, delta);

        ship.VelocityX *= drag;
        ship.VelocityY *= drag;

        // Ограничение максимальной скорости.
        var speed = MathF.Sqrt(
            ship.VelocityX * ship.VelocityX +
            ship.VelocityY * ship.VelocityY);

        if (speed > ship.MaxSpeed)
        {
            var factor = ship.MaxSpeed / speed;

            ship.VelocityX *= factor;
            ship.VelocityY *= factor;
        }

        // Перемещение.
        ship.Position = new Vector2(
            ship.Position.X + ship.VelocityX * delta,
            ship.Position.Y + ship.VelocityY * delta);
    }
}