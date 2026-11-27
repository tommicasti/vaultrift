using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveExoticTests
{
    private static readonly string[] Hall =
    [
        "##############",
        "#............#",
        "#............#",
        "#............#",
        "##############"
    ];

    private static readonly Stats HeroStats = new(maxHealth: 100, attack: 10, defense: 0, critical: 0, maxMana: 20);

    private static readonly EnemyKind Dummy = new("dummy", "Dummy", new Stats(maxHealth: 500, attack: 0, defense: 4, critical: 0), isStationary: true);

    private static readonly EnemyKind Biter = new("biter", "Biter", new Stats(maxHealth: 500, attack: 6, defense: 0, critical: 0));

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private sealed class AlwaysLucky : IRandom
    {
        public double NextDouble() => 0.0;
    }

    private static WeaponDefinition Weapon(WeaponType type, ExoticPower? exotic = null, params ItemEffect[] effects) =>
        new($"w-{type}-{exotic}", "Weapon", Rarity.Exotic, type, [new StatModifier(StatType.Attack, 2)]) { Exotic = exotic, Effects = effects };

    private static (LiveFloor Floor, LiveHero Hero) Arena(WeaponDefinition weapon, IRandom? random = null)
    {
        var floor = new LiveFloor(DungeonMap.Parse(Hall), new Position(2, 2), random ?? new NeverLucky());
        var player = new Player(new Position(2, 2), HeroStats, "Casti");
        player.Inventory.TryAdd(new ItemInstance(weapon));
        player.Equip(player.Inventory.Entries[0].Id);
        return (floor, floor.AddHero(1, player));
    }

    private static List<LiveEvent> Swing(LiveFloor floor, double seconds, double aimX = 1, double moveX = 0)
    {
        floor.SetInput(1, HeroInput.Of(moveX, 0, true, aimX, 0));
        var events = new List<LiveEvent>();

        for (var tick = 0; tick < (int)Math.Round(seconds * LiveRules.TicksPerSecond); tick++)
            events.AddRange(floor.Step());

        floor.SetInput(1, HeroInput.Idle);
        return events;
    }

    private static List<int> Damages(IEnumerable<LiveEvent> events, int monsterId) =>
        events.OfType<MonsterHit>().Where(hit => hit.MonsterId == monsterId).Select(hit => hit.Result.Damage).ToList();

    [Fact]
    public void RiftrenderDoublesEveryThirdBlowOnTheSameTarget()
    {
        var (floor, _) = Arena(Weapon(WeaponType.Sword, ExoticPower.Riftrender));
        var dummy = floor.AddMonster(new Enemy(5, new Position(3, 2), Dummy));

        var events = Swing(floor, 3);
        var damages = Damages(events, dummy.Id);

        Assert.True(damages.Count >= 3);
        Assert.True(damages[2] > damages[0] * 2 - 1);
        Assert.Contains(events.OfType<LiveExoticTriggered>(), e => e.Power == ExoticPower.Riftrender);
    }

    [Fact]
    public void OathboundReturnsTheDamageTakenOnTheNextBlow()
    {
        var (floor, hero) = Arena(Weapon(WeaponType.Sword, ExoticPower.Oathbound));
        var biter = floor.AddMonster(new Enemy(5, new Position(3, 2), Biter));
        biter.IsAwake = true;

        var events = new List<LiveEvent>();
        for (var tick = 0; tick < 40; tick++)
            events.AddRange(floor.Step());

        Assert.NotEmpty(events.OfType<HeroHit>());
        var swing = Swing(floor, 0.1);

        Assert.Contains(swing.OfType<LiveExoticTriggered>(), e => e.Power == ExoticPower.Oathbound && e.Amount > 0);
    }

    [Fact]
    public void HuntressMarksAndHealsOnTheKill()
    {
        var (floor, hero) = Arena(Weapon(WeaponType.Sword, ExoticPower.HuntressPromise));
        var weak = floor.AddMonster(new Enemy(5, new Position(3, 2), new EnemyKind("weak", "Weak", new Stats(maxHealth: 20, attack: 0, defense: 0, critical: 0), isStationary: true)));
        hero.TakeDamage(50);

        var events = Swing(floor, 3);

        Assert.True(weak.IsDead);
        Assert.Contains(events.OfType<LiveExoticTriggered>(), e => e.Power == ExoticPower.HuntressPromise && e.Amount == HeroStats.MaxHealth * Game.HuntressHealPercent / 100);
    }

    [Fact]
    public void WidowsKissPoisonsWithGrowingStacks()
    {
        var (floor, _) = Arena(Weapon(WeaponType.Dagger, ExoticPower.WidowsKiss));
        var dummy = floor.AddMonster(new Enemy(5, new Position(3, 2), Dummy));

        var events = Swing(floor, 2.05);

        var venom = events.OfType<MonsterEnvenomed>().ToList();
        Assert.NotEmpty(venom);
        Assert.True(dummy.VenomStacks > 1);
        Assert.All(venom, tick => Assert.Equal(tick.Stacks * Game.WidowDamagePerStack, tick.Damage));
    }

    [Fact]
    public void WorldsplitterStaggersWhatItHits()
    {
        var (floor, _) = Arena(Weapon(WeaponType.Axe, ExoticPower.Worldsplitter));
        var biter = floor.AddMonster(new Enemy(5, new Position(3, 2), Biter));
        biter.IsAwake = true;

        var events = Swing(floor, 0.1);

        Assert.Contains(events.OfType<LiveExoticTriggered>(), e => e.Power == ExoticPower.Worldsplitter && e.MonsterId == biter.Id);
        var during = new List<LiveEvent>();
        for (var tick = 0; tick < 15; tick++)
            during.AddRange(floor.Step());
        Assert.Empty(during.OfType<MonsterWindup>());
    }

    [Fact]
    public void HeadsmansMoonReadiesTheNextBlowAfterAKill()
    {
        var (floor, hero) = Arena(Weapon(WeaponType.Axe, ExoticPower.HeadsmansMoon));
        floor.AddMonster(new Enemy(5, new Position(3, 2), new EnemyKind("weak", "Weak", new Stats(maxHealth: 1, attack: 0, defense: 0, critical: 0), isStationary: true)));

        var events = Swing(floor, 0.1);

        Assert.Contains(events.OfType<LiveExoticTriggered>(), e => e.Power == ExoticPower.HeadsmansMoon);
        Assert.Equal(2, events.OfType<HeroSwung>().Count());
    }

    [Fact]
    public void ShadowstepLeapsToAFoeJustOutOfReach()
    {
        var (floor, hero) = Arena(Weapon(WeaponType.Dagger, ExoticPower.Shadowstep));
        var dummy = floor.AddMonster(new Enemy(5, new Position(4, 2), Dummy));

        var events = Swing(floor, 0.05);

        Assert.Contains(events.OfType<LiveExoticTriggered>(), e => e.Power == ExoticPower.Shadowstep && e.MonsterId == dummy.Id);
        Assert.True(hero.Position.X > 2.5);
        Assert.True(events.OfType<MonsterHit>().Single().Result.IsCritical);
    }

    [Fact]
    public void StarfallLanceReachesOneTileFurther()
    {
        var (plain, _) = Arena(Weapon(WeaponType.Spear));
        var (lance, _) = Arena(Weapon(WeaponType.Spear, ExoticPower.StarfallLance));
        plain.AddMonster(new Enemy(5, new Position(5, 2), Dummy));
        lance.AddMonster(new Enemy(5, new Position(5, 2), Dummy));

        Assert.Empty(Swing(plain, 0.1).OfType<MonsterHit>());
        Assert.NotEmpty(Swing(lance, 0.1).OfType<MonsterHit>());
    }

    [Fact]
    public void LastWallStrikesFoesThatWindUpInFront()
    {
        var (floor, _) = Arena(Weapon(WeaponType.Sword, ExoticPower.LastWall));
        var biter = floor.AddMonster(new Enemy(5, new Position(3, 2), Biter));
        biter.IsAwake = true;
        floor.SetInput(1, HeroInput.Of(0, 0, false, 1, 0));

        var events = new List<LiveEvent>();
        for (var tick = 0; tick < 20; tick++)
            events.AddRange(floor.Step());

        Assert.Contains(events.OfType<LiveExoticTriggered>(), e => e.Power == ExoticPower.LastWall && e.MonsterId == biter.Id);
        Assert.NotEmpty(Damages(events, biter.Id));
    }

    [Fact]
    public void LifeStealHealsFromTheDamageDealt()
    {
        var (floor, hero) = Arena(Weapon(WeaponType.Sword, null, new ItemEffect(EffectKind.LifeSteal, 50)));
        floor.AddMonster(new Enemy(5, new Position(3, 2), Dummy));
        hero.TakeDamage(60);

        var events = Swing(floor, 0.1);

        Assert.NotEmpty(events.OfType<HeroLifeDrained>());
        Assert.True(hero.Health > 40);
    }

    [Fact]
    public void ThornsPrickTheAttacker()
    {
        var (floor, _) = Arena(Weapon(WeaponType.Sword, null, new ItemEffect(EffectKind.Thorns, 4)));
        var biter = floor.AddMonster(new Enemy(5, new Position(3, 2), Biter));
        biter.IsAwake = true;

        var events = new List<LiveEvent>();
        for (var tick = 0; tick < 40; tick++)
            events.AddRange(floor.Step());

        Assert.Contains(events.OfType<MonsterPricked>(), prick => prick.MonsterId == biter.Id && prick.Damage == 4);
    }

    [Fact]
    public void DoubleShotSometimesLoosesASecondArrow()
    {
        var (floor, _) = Arena(Weapon(WeaponType.Bow, null, new ItemEffect(EffectKind.DoubleShot, 100)), new AlwaysLucky());

        var events = Swing(floor, 0.05);

        Assert.Equal(2, events.OfType<ShotFired>().Count());
    }

    [Fact]
    public void ManaSiphonRefillsManaOnKills()
    {
        var (floor, hero) = Arena(Weapon(WeaponType.Sword, null, new ItemEffect(EffectKind.ManaSiphon, 3)));
        floor.AddMonster(new Enemy(5, new Position(3, 2), new EnemyKind("weak", "Weak", new Stats(maxHealth: 1, attack: 0, defense: 0, critical: 0), isStationary: true)));
        hero.Player.SpendMana(hero.Player.Mana);

        var events = Swing(floor, 0.1);

        Assert.Equal(3, Assert.Single(events.OfType<HeroManaSiphoned>()).Amount);
    }
}
