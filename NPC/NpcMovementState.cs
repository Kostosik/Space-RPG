namespace GameProject.NPC;

public sealed class NpcMovementState
{
    public string? TargetObjectId { get; private set; }

    public string? TargetSystemId { get; private set; }

    // Система, из которой NPC совершил последний переход.
    // Нужна, чтобы не летать постоянно туда-обратно.
    public string? LastSystemId { get; private set; }

    public double TargetAngleRadians { get; private set; }

    public float Rotation { get; private set; }

    public int PlanetStopsSinceLastJump { get; private set; }

    public bool HasTarget =>
        TargetObjectId is not null ||
        TargetSystemId is not null;

    public bool IsSystemExitTarget =>
        TargetSystemId is not null;

    public bool IsArrived { get; private set; }

    public void SetTarget(string objectId, double angleRadians)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);

        if (!double.IsFinite(angleRadians))
            throw new ArgumentOutOfRangeException(nameof(angleRadians));

        TargetObjectId = objectId;
        TargetSystemId = null;
        TargetAngleRadians = angleRadians;
        IsArrived = false;
    }

    public void SetSystemTarget(string systemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemId);

        TargetObjectId = null;
        TargetSystemId = systemId;
        IsArrived = false;
    }

    public void SetRotation(float rotation)
    {
        if (!float.IsFinite(rotation))
            throw new ArgumentOutOfRangeException(nameof(rotation));

        Rotation = rotation;
    }

    public void MarkArrived()
    {
        if (!HasTarget)
            throw new InvalidOperationException("NPC has no movement target.");

        IsArrived = true;
    }

    public void RegisterPlanetStop()
    {
        PlanetStopsSinceLastJump++;
    }

    public void CompleteSystemTransition(string previousSystemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(previousSystemId);

        LastSystemId = previousSystemId;
        PlanetStopsSinceLastJump = 0;

        ClearTarget();
    }

    public void ClearTarget()
    {
        TargetObjectId = null;
        TargetSystemId = null;
        TargetAngleRadians = 0.0;
        IsArrived = false;
    }
}