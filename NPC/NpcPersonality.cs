using System.Collections.ObjectModel;

namespace GameProject.NPC;

public sealed class NpcPersonality
{
    private readonly ReadOnlyDictionary<string, float> _traits;

    public NpcPersonality(
        IReadOnlyDictionary<string, float>? traits = null)
    {
        var copy = new Dictionary<string, float>(
            StringComparer.Ordinal);

        if (traits is not null)
        {
            foreach (var trait in traits)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(
                    trait.Key);

                if (!string.Equals(
                        trait.Key,
                        trait.Key.Trim(),
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "Trait ID cannot contain surrounding whitespace.",
                        nameof(traits));
                }

                if (!float.IsFinite(trait.Value) ||
                    trait.Value < 0f ||
                    trait.Value > 1f)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(traits),
                        $"Invalid value for trait '{trait.Key}'.");
                }

                copy.Add(trait.Key, trait.Value);
            }
        }

        _traits = new ReadOnlyDictionary<string, float>(copy);
    }

    public IReadOnlyDictionary<string, float> Traits => _traits;

    public bool TryGetTrait(
        string traitId,
        out float value)
    {
        return _traits.TryGetValue(traitId, out value);
    }
}