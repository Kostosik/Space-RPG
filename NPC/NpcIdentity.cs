namespace GameProject.NPC;

public sealed class NpcIdentity
{
    public NpcIdentity(
        Guid id,
        string name,
        string? callsign = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("NPC ID cannot be empty.", nameof(id));

        ValidateText(name, nameof(name));

        if (callsign is not null)
            ValidateText(callsign, nameof(callsign));

        Id = id;
        Name = name;
        Callsign = callsign;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string? Callsign { get; }

    public static NpcIdentity Create(
        string name,
        string? callsign = null)
    {
        return new NpcIdentity(
            Guid.NewGuid(),
            name,
            callsign);
    }

    private static void ValidateText(
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
                "Value cannot contain surrounding whitespace.",
                parameterName);
        }
    }
}