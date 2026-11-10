namespace GameProject.NPC;

public sealed class NpcLocation
{
    public NpcLocation(
        string systemId,
        double x,
        double y,
        string? objectId = null)
    {
        ValidateId(systemId, nameof(systemId));

        if (objectId is not null)
            ValidateId(objectId, nameof(objectId));

        if (!double.IsFinite(x) ||
            !double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                "NPC position must be finite.");
        }

        SystemId = systemId;
        ObjectId = objectId;
        X = x;
        Y = y;
    }

    public string SystemId { get; }

    public string? ObjectId { get; }

    public double X { get; }

    public double Y { get; }

    private static void ValidateId(
        string value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!string.Equals(
                value,
                value.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "ID cannot contain surrounding whitespace.",
                parameterName);
        }
    }
}