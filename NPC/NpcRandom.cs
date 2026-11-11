namespace GameProject.NPC;

public sealed class NpcRandom
{
    private readonly int _rootSeed;

    private ulong _state;

    public NpcRandom(int seed)
    {
        _rootSeed = seed;
        _state = unchecked((ulong)(uint)seed);
    }

    public NpcRandom Fork(string purpose)
    {
        return new NpcRandom(
            NpcDeterminism.CreateDerivedSeed(
                _rootSeed,
                purpose));
    }

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;

        var value = _state;

        value = (value ^ (value >> 30)) *
                0xBF58476D1CE4E5B9UL;

        value = (value ^ (value >> 27)) *
                0x94D049BB133111EBUL;

        return value ^ (value >> 31);
    }

    public double NextDouble()
    {
        return (NextUInt64() >> 11) *
               (1.0 / 9007199254740992.0);
    }

    public float NextSingle()
    {
        return (float)NextDouble();
    }
}