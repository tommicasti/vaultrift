using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveTerrainTests
{
    private static readonly Stats Sturdy = new(maxHealth: 100, attack: 10, defense: 0, critical: 0);

    private static readonly EnemyKind Rat = new("rat", "Rat", new Stats(maxHealth: 40, attack: 1, defense: 0, critical: 0));

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private static LiveFloor Floor(string[] rows, int startX, int startY) =>
        new(DungeonMap.Parse(rows), new Position(startX, startY), new NeverLucky());

    private static List<LiveEvent> Run(LiveFloor floor, double seconds, int heroId = 1, double moveX = 0, double moveY = 0, bool attack = false, double aimX = 0)
    {
        floor.SetInput(heroId, HeroInput.Of(moveX, moveY, attack, aimX, 0));
        var events = new List<LiveEvent>();

        for (var tick = 0; tick < (int)Math.Round(seconds * LiveRules.TicksPerSecond); tick++)
            events.AddRange(floor.Step());

        floor.SetInput(heroId, HeroInput.Idle);
        return events;
    }

    [Fact]
    public void CurrentsCarryHeroesAlong()
    {
        var floor = Floor(["#########", "#>>>>>..#", "#########"], 1, 1);
        var hero = floor.AddHero(1, "Casti", "knight", Sturdy);

        Run(floor, 1);

        Assert.InRange(hero.Position.X, 1 + LiveRules.CurrentSpeed * 0.9, 1 + LiveRules.CurrentSpeed * 1.1);
    }

    [Fact]
    public void SporesPoisonForAFewSeconds()
    {
        var floor = Floor(["#######", "#S....#", "#######"], 1, 1);
        var hero = floor.AddHero(1, "Casti", "knight", Sturdy);

        var events = Run(floor, 1.05);
        Assert.Single(events.OfType<HeroPoisoned>());

        Run(floor, 0.5, moveX: 1);
        var later = Run(floor, Game.PoisonTurns + 1);

        var ticks = events.OfType<HeroPoisonDamaged>().Concat(later.OfType<HeroPoisonDamaged>()).ToList();
        Assert.Equal(Game.PoisonTurns, ticks.Count);
        Assert.All(ticks, tick => Assert.Equal(Game.PoisonDamage(Sturdy.MaxHealth), tick.Damage));
        Assert.False(hero.Player.IsPoisoned);
    }

    [Fact]
    public void LavaHurtsMonstersThatAreNotFireproof()
    {
        var floor = Floor(["#######", "#..L..#", "#######"], 1, 1);
        var rat = floor.AddMonster(new Enemy(5, new Position(3, 1), Rat));

        var events = Run(floor, 1.05, heroId: 0);

        var burn = Assert.Single(events.OfType<MonsterBurnedByLava>());
        Assert.Equal(rat.Id, burn.MonsterId);
        Assert.Equal(Game.LavaDamage(rat.MaxHealth), burn.Damage);
    }

    [Fact]
    public void MonstersDoNotWalkIntoLava()
    {
        var floor = Floor(["#########", "#..L....#", "#########"], 1, 1);
        floor.AddHero(1, "Casti", "knight", Sturdy);
        var rat = floor.AddMonster(new Enemy(5, new Position(6, 1), Rat));
        rat.IsAwake = true;

        Run(floor, 3);

        Assert.True(rat.Position.X > 3.4);
    }

    [Fact]
    public void LightningHitsTwiceAsHardInWater()
    {
        var dry = Floor(["#######", "#.....#", "#######"], 1, 1);
        var wet = Floor(["#######", "#..~..#", "#######"], 1, 1);
        var shock = new WeaponDefinition("shock", "Shock", Rarity.Common, WeaponType.Sword, [], FixedElement: Element.Lightning);

        int Strike(LiveFloor floor)
        {
            var player = new Player(new Position(1, 1), Sturdy, "Casti");
            player.Inventory.TryAdd(new ItemInstance(shock, Element.Lightning));
            player.Equip(player.Inventory.Entries[0].Id);
            floor.AddHero(1, player);
            floor.AddMonster(new Enemy(5, new Position(3, 1), Rat));
            floor.SetInput(1, HeroInput.Of(1, 0, false));
            for (var tick = 0; tick < 8; tick++)
                floor.Step();
            floor.SetInput(1, HeroInput.Of(0, 0, true));
            return floor.Step().OfType<MonsterHit>().First().Result.Damage;
        }

        Assert.Equal(Strike(dry) * Game.WaterLightningMultiplier, Strike(wet));
    }

    [Fact]
    public void StrikingACrystalShattersItAndHurtsMonstersAround()
    {
        var floor = Floor(["########", "#.C....#", "########"], 1, 1);
        floor.AddHero(1, "Casti", "knight", Sturdy);
        var rat = floor.AddMonster(new Enemy(5, new Position(3, 1), Rat));

        var events = Run(floor, 0.1, attack: true, aimX: 1);

        Assert.Single(events.OfType<CrystalBroken>());
        Assert.Equal(TileType.Floor, floor.Map.GetTile(new Position(2, 1)));
        Assert.Contains(events.OfType<MonsterShardHit>(), hit => hit.MonsterId == rat.Id);
        Assert.Contains(events.OfType<TilesChanged>(), change => change.Tile == TileType.Floor);
    }

    [Fact]
    public void ObstaclesCanBeSmashed()
    {
        var floor = Floor(["######", "#.o..#", "######"], 1, 1);
        floor.AddHero(1, "Casti", "knight", Sturdy);

        var events = Run(floor, 0.1, attack: true, aimX: 1);

        Assert.Single(events.OfType<ObstacleBroken>());
        Assert.True(floor.Map.IsWalkable(new Position(2, 1)));
    }
}
