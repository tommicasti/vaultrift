namespace Vaultrift.Infrastructure.Accounts;

public sealed record TrophyDefinition(string Id, string Name, string Description, string? Counter = null, long Goal = 0);

public static class TrophyCatalog
{
    public const string FirstBlood = "first-blood";
    public const string KingSlayer = "king-slayer";
    public const string Collector = "collector";
    public const string Untouchable = "untouchable";
    public const string Unbroken = "unbroken";
    public const string CustodeNormal = "custode-normal";
    public const string CustodeHard = "custode-hard";
    public const string CustodeHardcore = "custode-hardcore";
    public const string WeaponMaster = "weapon-master";
    public const string Rich = "rich";

    public const string CoinsCounter = "coins";
    public const string ConqueredPrefix = "conquered:";
    public const long RichGoal = 100_000;
    public const int UnbrokenFloor = 10;

    public static IReadOnlyList<TrophyDefinition> All { get; } =
    [
        new(FirstBlood, "First Blood", "Defeat your first monster."),
        new(KingSlayer, "King Slayer", "Defeat a boss."),
        new(Collector, "Collector", "Find an exotic item."),
        new(Untouchable, "Untouchable", "Clear a floor without taking any damage."),
        new(Unbroken, "Unbroken", $"Reach floor {UnbrokenFloor} without dying."),
        new(CustodeNormal, "Custode Slain", "Defeat the Custode. Reward: a dragon weapon for your class in every new game."),
        new(CustodeHard, "Custode Slain: Hard", "Defeat the Custode on Hard or Hardcore. Reward: your dragon weapon grows to its second form."),
        new(CustodeHardcore, "Custode Slain: Hardcore", "Defeat the Custode on Hardcore. Reward: your dragon weapon reaches its third form."),
        new(WeaponMaster, "Weapon Master", "Defeat the Custode with every class."),
        new(Rich, "Filthy Rich", "Collect 100,000 coins across all your heroes.", CoinsCounter, RichGoal),
    ];

    public static TrophyDefinition Find(string id) => All.First(trophy => trophy.Id == id);
}
