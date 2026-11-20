using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class ProgressionTests
{
    private static readonly DungeonMap Corridor = DungeonMap.Parse(
    [
        "######",
        "#....#",
        "######"
    ]);

    private static readonly EnemyKind Rat = new("test-rat", "Rat", new Stats(maxHealth: 6, attack: 1, defense: 0));

    private static readonly EnemyKind Boss =
        new("test-boss", "Boss", new Stats(maxHealth: 40, attack: 1, defense: 0), isBoss: true);

    private static Player NewPlayer() => new(new Position(1, 1), new Stats(maxHealth: 30, attack: 100, defense: 0));

    [Theory]
    [InlineData(1, 20)]
    [InlineData(2, 80)]
    [InlineData(5, 500)]
    public void ExperienceToNextLevel_GrowsWithTheSquareOfTheLevel(int level, int expected)
    {
        Assert.Equal(expected, Progression.ExperienceToNextLevel(level));
    }

    [Theory]
    [InlineData(Rarity.Common, 1, 3)]
    [InlineData(Rarity.Rare, 1, 6)]
    [InlineData(Rarity.Epic, 1, 9)]
    [InlineData(Rarity.Legendary, 1, 15)]
    [InlineData(Rarity.Common, 3, 9)]
    public void ExperienceFor_ScalesWithRarityAndLevel(Rarity rarity, int level, int expected)
    {
        Assert.Equal(expected, Progression.ExperienceFor(new Enemy(1, new Position(0, 0), Rat, rarity, level)));
    }

    [Fact]
    public void ExperienceFor_BossesGiveDouble()
    {
        Assert.Equal(40, Progression.ExperienceFor(new Enemy(1, new Position(0, 0), Boss)));
    }

    [Fact]
    public void GainExperience_BelowTheThreshold_KeepsTheLevel()
    {
        var player = NewPlayer();

        Assert.Equal(0, player.GainExperience(19));
        Assert.Equal(1, player.Level);
        Assert.Equal(19, player.Experience);
        Assert.Equal(0, player.UnspentPoints);
    }

    [Fact]
    public void GainExperience_ReachingTheThreshold_LevelsUpAndHeals()
    {
        var player = NewPlayer();
        player.TakeDamage(20);

        var levels = player.GainExperience(25);

        Assert.Equal(1, levels);
        Assert.Equal(2, player.Level);
        Assert.Equal(5, player.Experience);
        Assert.Equal(Progression.PointsPerLevel, player.UnspentPoints);
        Assert.Equal(player.Stats.MaxHealth, player.Health);
    }

    [Fact]
    public void GainExperience_ALotAtOnce_CanLevelUpMoreThanOnce()
    {
        var player = NewPlayer();

        var levels = player.GainExperience(100);

        Assert.Equal(2, levels);
        Assert.Equal(3, player.Level);
        Assert.Equal(0, player.Experience);
        Assert.Equal(2 * Progression.PointsPerLevel, player.UnspentPoints);
    }

    [Theory]
    [InlineData(StatPoint.Health, 35, 100, 0, 0, 10)]
    [InlineData(StatPoint.Attack, 30, 101, 0, 0, 10)]
    [InlineData(StatPoint.Defense, 30, 100, 1, 0, 10)]
    [InlineData(StatPoint.Evasion, 30, 100, 0, 2, 10)]
    [InlineData(StatPoint.Critical, 30, 100, 0, 0, 12)]
    public void SpendPoint_RaisesTheChosenStat(StatPoint stat, int health, int attack, int defense, int evasion, int critical)
    {
        var player = NewPlayer();
        player.GainExperience(20);

        var result = player.SpendPoint(stat);

        Assert.Equal(SpendResult.Spent, result);
        Assert.Equal(new Stats(health, attack, defense, evasion, critical), player.Stats);
        Assert.Equal(Progression.PointsPerLevel - 1, player.UnspentPoints);
    }

    [Fact]
    public void SpendPoint_OnHealth_AlsoHealsTheNewHitPoints()
    {
        var player = NewPlayer();
        player.GainExperience(20);
        player.TakeDamage(10);

        player.SpendPoint(StatPoint.Health);

        Assert.Equal(25, player.Health);
        Assert.Equal(35, player.Stats.MaxHealth);
    }

    [Fact]
    public void SpendPoint_WithoutPoints_IsRejected()
    {
        var player = NewPlayer();

        Assert.Equal(SpendResult.NoPointsLeft, player.SpendPoint(StatPoint.Attack));
        Assert.Equal(100, player.Stats.Attack);
    }

    [Fact]
    public void SpendPoint_PastTheEvasionCap_IsRejected()
    {
        var player = new Player(new Position(1, 1), new Stats(maxHealth: 30, attack: 1, defense: 0, evasion: 49));
        player.GainExperience(20);

        Assert.Equal(SpendResult.StatAtCap, player.SpendPoint(StatPoint.Evasion));
        Assert.Equal(Progression.PointsPerLevel, player.UnspentPoints);
    }

    [Fact]
    public void SpentPoints_SurviveEquippingGear()
    {
        var player = NewPlayer();
        player.GainExperience(20);
        player.SpendPoint(StatPoint.Attack);
        player.Inventory.TryAdd(new WeaponDefinition("test-sword", "Sword", Rarity.Common, WeaponType.Sword, [new StatModifier(StatType.Attack, 3)]));

        player.Equip(player.Inventory.Entries[0].Id);

        Assert.Equal(104, player.CombatStats.Attack);
    }

    [Fact]
    public void KillingAnEnemy_GrantsExperience()
    {
        var game = new Game(Corridor, NewPlayer(), [new Enemy(1, new Position(2, 1), Rat)], new FixedRandom(0.99, 0.99));
        game.Player.Facing = Direction.Right;

        var result = game.Attack();

        Assert.Contains(new ExperienceGained(3), result.Events);
        Assert.Equal(3, game.Player.Experience);
    }

    [Fact]
    public void KillingABoss_CanLevelUpTheHero()
    {
        var game = new Game(Corridor, NewPlayer(), [new Enemy(1, new Position(2, 1), Boss)], new FixedRandom(0.99, 0.99));
        game.Player.Facing = Direction.Right;

        var result = game.Attack();

        Assert.Contains(new ExperienceGained(40), result.Events);
        Assert.Contains(new LeveledUp(2), result.Events);
        Assert.Equal(2, game.Player.Level);
        Assert.Equal(20, game.Player.Experience);
    }

    [Fact]
    public void GameSpendPoint_DoesNotPassTheTurn()
    {
        var player = NewPlayer();
        player.GainExperience(20);
        var game = new Game(Corridor, player, [], new FixedRandom());

        var result = game.SpendPoint(StatPoint.Defense);

        Assert.False(result.TurnPassed);
        Assert.Contains(new StatPointSpent(StatPoint.Defense), result.Events);
    }

    [Fact]
    public void GameSpendPoint_WithoutPoints_IsRejected()
    {
        var game = new Game(Corridor, NewPlayer(), [], new FixedRandom());

        var result = game.SpendPoint(StatPoint.Defense);

        Assert.Contains(new SpendRejected(SpendResult.NoPointsLeft), result.Events);
    }

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(-3, 1.0)]
    [InlineData(1, 0.75)]
    [InlineData(2, 0.5)]
    [InlineData(10, 0.1)]
    public void Experience_ShrinksWhenTheHeroOutlevelsTheEnemy(int levelsAbove, double factor) =>
        Assert.Equal(factor, Progression.LevelGapFactor(levelsAbove), precision: 6);

    [Fact]
    public void AnOutleveledEnemy_GivesLessExperience()
    {
        var enemy = new Enemy(1, new Position(0, 0), Rat, Rarity.Common, level: 4);

        Assert.True(Progression.ExperienceFor(enemy, playerLevel: 6) < Progression.ExperienceFor(enemy, playerLevel: 4));
        Assert.Equal(Progression.ExperienceFor(enemy), Progression.ExperienceFor(enemy, playerLevel: 2));
    }
}
