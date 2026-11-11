namespace GameProject.NPC;

internal static class NpcNameGenerator
{
    private static readonly string[] FirstNames =
    [
        "Алексей", "Марина", "Иван", "Елена",
        "Дмитрий", "Анна", "Максим", "София",
        "Марк", "Алиса", "Никита", "Вера",
        "Артём", "Ирина", "Роман", "Милана",
        "Кирилл", "Дарья", "Егор", "Полина",
        "Денис", "Виктория", "Михаил", "Кира"
    ];

    private static readonly string[] LastNames =
    [
        "Волков", "Орлов", "Соколов", "Морозов",
        "Лебедев", "Ковалёв", "Смирнов", "Зайцев",
        "Новиков", "Фёдоров", "Медведев", "Белов",
        "Романов", "Воронов", "Зорин", "Громов",
        "Королёв", "Серов", "Миронов", "Титов"
    ];

    public static NpcIdentity Generate(
        int worldSeed,
        string systemId,
        int npcIndex)
    {
        var random = new NpcRandom(
            NpcDeterminism.CreateSeed(
                worldSeed,
                systemId,
                npcIndex,
                "identity"));

        var id = NpcDeterminism.CreateId(
            worldSeed,
            systemId,
            npcIndex);

        var name =
            $"{FirstNames[(int)(random.NextUInt64() % (ulong)FirstNames.Length)]} " +
            $"{LastNames[(int)(random.NextUInt64() % (ulong)LastNames.Length)]}";

        var callsign =
            $"R-{id.ToString("N")[..8].ToUpperInvariant()}";

        return new NpcIdentity(
            id,
            name,
            callsign);
    }
}