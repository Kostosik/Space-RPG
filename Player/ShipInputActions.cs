using Engine.Input;

namespace GameProject.Player;

public static class ShipInputActions
{
    public static readonly InputAction Thrust =
        new("Thrust");

    public static readonly InputAction Reverse =
        new("Reverse");

    public static readonly InputAction TurnLeft =
        new("TurnLeft");

    public static readonly InputAction TurnRight =
        new("TurnRight");

    public static readonly InputAction EndTurn =
        new("EndTurn");

    public static readonly InputAction Shift =
        new("Shift");
}