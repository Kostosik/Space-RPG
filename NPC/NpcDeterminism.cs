using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace GameProject.NPC;

internal static class NpcDeterminism
{
    public static int CreateSeed(
        int worldSeed,
        string systemId,
        int npcIndex,
        string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        return HashToInt(
            $"npc|{worldSeed}|{systemId}|{npcIndex}|{purpose}");
    }

    public static int CreateDerivedSeed(
        int seed,
        string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        return HashToInt(
            $"npc-stream|{seed}|{purpose}");
    }

    public static Guid CreateId(
        int worldSeed,
        string systemId,
        int npcIndex)
    {
        var input = Encoding.UTF8.GetBytes(
            $"npc-id|{worldSeed}|{systemId}|{npcIndex}");

        var hash = SHA256.HashData(input);

        return new Guid(hash.AsSpan(0, 16));
    }

    private static int HashToInt(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return BinaryPrimitives.ReadInt32LittleEndian(hash);
    }
}