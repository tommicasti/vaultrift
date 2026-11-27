using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Realtime;

public sealed class LiveCombatTests
{
    private static readonly string[] Hall =
    [
        "################",
        "#..............#",
        "#..............#",
        "#..............#",
        "#..............#",
        "################"
    ];

    private static readonly SpellDefinition Bolt = new("test-bolt", "Bolt", Element.Fire, SpellShape.Bolt, power: 4, manaCost: 3);
    private static readonly SpellDefinition Fireball = new("test-fireball", "Fireball", Element.Fire, SpellShape.Burst, power: 5, manaCost: 6, size: 1);
    private static readonly SpellDefinition Nova = new("test-nova", "Frost Nova", Element.Ice, SpellShape.Nova, power: 3, manaCost: 6, size: 1, alwaysTriggers: true);
    private static readonly SpellDefinition Squall = new("test-squall", "Squall", Element.Wind, SpellShape.Line, power: 5, manaCost: 6, size: 2);

    private static readonly WeaponDefinition Staff =
        new("test-staff", "Staff", Rarity.Common, WeaponType.Staff, [new StatModifier(StatType.Attack, 2)]) { Spell = Bolt };

    private static WeaponDefinition Elemental(Element element) =>
        new($"test-{element}-sword", $"{element} Sword", Rarity.Legendary, WeaponType.Sword, [new StatModifier(StatType.Attack, 1)], FixedElement: element);

    private sealed class NeverLucky : IRandom
    {
        public double NextDouble() => 0.99;
    }

    private sealed class AlwaysLucky : IRandom
    {
        public double NextDouble() => 0.0;
    }

    private static Player Hero(ClassAbility? ability = null, int maxMana = 0) =>
        new(new Position(3, 2), new CharacterClass("hero", "Hero", new Stats(maxHealth: 40, attack: 6, defense: 0, critical: 0, maxMana: maxMana),
            new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Attack, 0)), Ability: ability));

    private static (LiveFloor Floor, LiveHero Hero) Setup(Player player, IRandom? random = null)
    {
        var floor = new LiveFloor(DungeonMap.Parse(Hall), new Position(3, 2), random ?? new NeverLucky());
        return (floor, floor.AddHero(1, player));
    }

    private static LiveMonster Dummy(LiveFloor floor, int id, double x, double y, int health = 60)
    {
        var monster = floor.AddMonster(new Enemy(id, new Position((int)x, (int)y), new Stats(maxHealth: health, attack: 1, defense: 0)));
        monster.Position = new Vector(x, y);
        return monster;
    }

    private static List<LiveEvent> Run(LiveFloor floor, double seconds)
    {
        var events = new List<LiveEvent>();
        for (var tick = 0; tick < (int)Math.Round(seconds * LiveRules.TicksPerSecond); tick++)
            events.AddRange(floor.Step());
        return events;
    }

    private static List<LiveEvent> Swing(LiveFloor floor, double aimX = 1, double aimY = 0)
    {
        floor.SetInput(1, HeroInput.Of(0, 0, true, aimX, aimY));
        var events = floor.Step().ToList();
        floor.SetInput(1, HeroInput.Idle);
        return events;
    }

    [Fact]
    public void AxesSweepTheWholeFrontGrazingTheSides()
    {
        var player = Hero();
        player.Give(new ItemInstance(TestItems.Axe));
        var (floor, _) = Setup(player);
        var front = Dummy(floor, 10, 4, 2);
        var side = Dummy(floor, 11, 3.2, 3);
        var behind = Dummy(floor, 12, 2, 2);

        var hits = Swing(floor).OfType<MonsterHit>().ToList();

        Assert.Contains(hits, hit => hit.MonsterId == 10);
        Assert.Contains(hits, hit => hit.MonsterId == 11);
        Assert.DoesNotContain(hits, hit => hit.MonsterId == 12);
        Assert.True(front.MaxHealth - front.Health > side.MaxHealth - side.Health);
        Assert.Equal(behind.MaxHealth, behind.Health);
    }

    [Fact]
    public void SpearsReachTwoTilesAndPierceTheFirstFoe()
    {
        var player = Hero();
        player.Give(new ItemInstance(TestGear.Spear));
        var (floor, _) = Setup(player);
        Dummy(floor, 10, 4, 2);
        Dummy(floor, 11, 5, 2);

        var hits = Swing(floor).OfType<MonsterHit>().Select(hit => hit.MonsterId).ToList();

        Assert.Equal([10, 11], hits);
    }

    [Fact]
    public void SwordsHitOnlyTheClosestFoe()
    {
        var player = Hero();
        player.Give(new ItemInstance(TestItems.Sword));
        var (floor, _) = Setup(player);
        Dummy(floor, 10, 4, 2);
        Dummy(floor, 11, 3.9, 2.6);

        Assert.Single(Swing(floor).OfType<MonsterHit>());
    }

    [Fact]
    public void ArrowsFlyAcrossTheRoom()
    {
        var player = Hero();
        player.Give(new ItemInstance(TestGear.Bow));
        var (floor, _) = Setup(player);
        var far = Dummy(floor, 10, 8, 2);

        var events = Swing(floor);
        Assert.Single(floor.Projectiles);
        Assert.Contains(events, e => e is ShotFired);

        events.AddRange(Run(floor, 0.6));

        Assert.Contains(events, e => e is MonsterHit { MonsterId: 10 });
        Assert.True(far.Health < far.MaxHealth);
        Assert.Empty(floor.Projectiles);
    }

    [Fact]
    public void ArrowsShotAtPointBlankOnlyGraze()
    {
        var near = Hero();
        near.Give(new ItemInstance(TestGear.Bow));
        var (closeFloor, _) = Setup(near);
        var close = Dummy(closeFloor, 10, 4, 2);
        Swing(closeFloor);
        Run(closeFloor, 0.3);

        var distant = Hero();
        distant.Give(new ItemInstance(TestGear.Bow));
        var (farFloor, _) = Setup(distant);
        var far = Dummy(farFloor, 10, 7, 2);
        Swing(farFloor);
        Run(farFloor, 0.5);

        Assert.True(close.MaxHealth - close.Health < far.MaxHealth - far.Health);
    }

    [Fact]
    public void ArrowsStopAtWalls()
    {
        var player = Hero();
        player.Give(new ItemInstance(TestGear.Bow));
        var (floor, _) = Setup(player);

        Swing(floor, 0, -1);
        Run(floor, 0.3);

        Assert.Empty(floor.Projectiles);
    }

    [Fact]
    public void StavesCastTheActiveSpellAndSpendMana()
    {
        var player = Hero(maxMana: 20);
        player.Give(new ItemInstance(Staff));
        var (floor, hero) = Setup(player);
        var target = Dummy(floor, 10, 7, 2);
        var mana = player.Mana;

        Swing(floor);
        var events = Run(floor, 0.6);

        Assert.True(player.Mana < mana);
        Assert.Contains(events, e => e is MonsterHit { MonsterId: 10 });
        Assert.True(target.Health < target.MaxHealth);
        Assert.Equal(AttackStyle.Cast, AttackProfile.For(hero.Player).Style);
    }

    [Fact]
    public void WithoutManaTheStaffOnlyBonks()
    {
        var player = Hero(maxMana: 20);
        player.Give(new ItemInstance(Staff));
        player.SpendMana(player.Mana);
        var (floor, _) = Setup(player);
        Dummy(floor, 10, 4, 2);

        var events = Swing(floor);

        Assert.Empty(floor.Projectiles);
        Assert.Contains(events, e => e is MonsterHit { MonsterId: 10 });
    }

    [Fact]
    public void FireballsExplodeOnImpact()
    {
        var player = Hero(maxMana: 30);
        var (floor, hero) = Setup(player);
        player.Inventory.TryAdd(new ItemInstance(new ScrollDefinition("test-scroll", "Scroll of Fire", Rarity.Rare, Fireball)));
        var entry = player.Inventory.Entries.Single();
        Dummy(floor, 10, 8, 2);
        Dummy(floor, 11, 8, 3);
        floor.SetInput(1, HeroInput.Of(0, 0, false, 1, 0));
        floor.Step();

        var used = floor.ReadScroll(hero.Id, entry.Id);
        var events = Run(floor, 0.8);

        Assert.IsType<ItemUsed>(Assert.Single(used.Events));
        Assert.Contains(events, e => e is SpellUnleashed);
        Assert.Contains(events, e => e is MonsterHit { MonsterId: 10 });
        Assert.Contains(events, e => e is MonsterHit { MonsterId: 11 });
        Assert.Empty(player.Inventory.Entries);
    }

    [Fact]
    public void NovasStrikeEverythingAroundAndFreeze()
    {
        var player = Hero(maxMana: 30);
        var (floor, hero) = Setup(player);
        player.Inventory.TryAdd(new ItemInstance(new ScrollDefinition("test-nova-scroll", "Scroll of Frost", Rarity.Rare, Nova)));
        var left = Dummy(floor, 10, 2, 2);
        var right = Dummy(floor, 11, 4, 2);

        floor.ReadScroll(hero.Id, player.Inventory.Entries.Single().Id);
        var events = floor.Step();

        Assert.Contains(events, e => e is MonsterFrozen { MonsterId: 10 });
        Assert.Contains(events, e => e is MonsterFrozen { MonsterId: 11 });
        Assert.True(floor.IsFrozen(left));
        Assert.True(left.Health < left.MaxHealth && right.Health < right.MaxHealth);
    }

    [Fact]
    public void LinesHitEveryFoeInTheirPath()
    {
        var player = Hero(maxMana: 30);
        var (floor, hero) = Setup(player);
        player.Inventory.TryAdd(new ItemInstance(new ScrollDefinition("test-line", "Scroll of Wind", Rarity.Rare, Squall)));
        Dummy(floor, 10, 5, 2);
        Dummy(floor, 11, 8, 2);
        Dummy(floor, 12, 5, 4);
        floor.SetInput(1, HeroInput.Of(0, 0, false, 1, 0));
        floor.Step();

        floor.ReadScroll(hero.Id, player.Inventory.Entries.Single().Id);
        var hits = floor.Step().OfType<MonsterHit>().Select(hit => hit.MonsterId).ToList();

        Assert.Contains(10, hits);
        Assert.Contains(11, hits);
        Assert.DoesNotContain(12, hits);
    }

    [Fact]
    public void FrozenMonstersStandStill()
    {
        var player = Hero();
        player.Give(new ItemInstance(Elemental(Element.Ice)));
        var (floor, _) = Setup(player, new AlwaysLucky());
        var monster = Dummy(floor, 10, 4, 2);
        monster.IsAwake = true;

        var events = Swing(floor);
        var frozenAt = monster.Position;
        Run(floor, 0.5);

        Assert.Contains(events, e => e is MonsterFrozen { MonsterId: 10 });
        Assert.Equal(frozenAt, monster.Position);
    }

    [Fact]
    public void BurningMonstersLoseHealthEverySecond()
    {
        var player = Hero();
        player.Give(new ItemInstance(Elemental(Element.Fire)));
        var (floor, _) = Setup(player, new AlwaysLucky());
        var monster = Dummy(floor, 10, 4, 2, health: 200);

        var events = Swing(floor);
        Assert.Contains(events, e => e is MonsterIgnited { MonsterId: 10 });
        floor.SetInput(1, HeroInput.Of(-1, 0, false));
        var afterIgnite = monster.Health;

        var burns = Run(floor, ElementRules.BurnTurns + 1).OfType<MonsterScorched>().ToList();

        Assert.Equal(ElementRules.BurnTurns, burns.Count);
        Assert.True(monster.Health < afterIgnite);
        Assert.False(monster.IsBurning);
    }

    [Fact]
    public void WindPushesMonstersAway()
    {
        var player = Hero();
        player.Give(new ItemInstance(Elemental(Element.Wind)));
        var (floor, _) = Setup(player);
        var monster = Dummy(floor, 10, 4, 2);

        var events = Swing(floor);

        Assert.Contains(events, e => e is MonsterPushed { MonsterId: 10 });
        Assert.True(monster.Position.X > 4.5);
    }

    [Fact]
    public void LightningJumpsToANeighbour()
    {
        var player = Hero();
        player.Give(new ItemInstance(Elemental(Element.Lightning)));
        var (floor, _) = Setup(player);
        Dummy(floor, 10, 4, 2);
        var neighbour = Dummy(floor, 11, 5, 2);

        Swing(floor);

        Assert.True(neighbour.Health < neighbour.MaxHealth);
    }

    [Fact]
    public void FuryRaisesAttackForAWhile()
    {
        var player = Hero(ClassAbility.Fury);
        var (floor, hero) = Setup(player);
        var attack = player.CombatStats.Attack;

        var used = floor.UseAbility(hero.Id);
        var events = floor.Step();

        Assert.IsType<AbilityUsed>(Assert.Single(used.Events));
        Assert.Contains(events, e => e is AbilityActivated { Ability: ClassAbility.Fury });
        Assert.True(player.CombatStats.Attack > attack);
        Assert.True(player.AbilityCooldown > 0);

        var later = Run(floor, AbilityRules.FuryTurns + 1);
        Assert.Contains(later, e => e is AbilityFaded { Ability: ClassAbility.Fury });
        Assert.Equal(attack, player.CombatStats.Attack);
    }

    [Fact]
    public void AbilitiesWaitForTheirCooldown()
    {
        var (floor, hero) = Setup(Hero(ClassAbility.Fury));

        floor.UseAbility(hero.Id);
        var again = floor.UseAbility(hero.Id);

        Assert.Equal(AbilityResult.OnCooldown, Assert.IsType<AbilityRejected>(Assert.Single(again.Events)).Reason);
    }

    [Fact]
    public void BulwarkHalvesTheBlows()
    {
        var (floor, hero) = Setup(Hero(ClassAbility.Bulwark));
        var brute = floor.AddMonster(new Enemy(10, new Position(4, 2), new Stats(maxHealth: 50, attack: 20, defense: 0)));
        floor.UseAbility(hero.Id);

        var hit = Run(floor, 1.5).OfType<HeroHit>().First();

        Assert.Equal(10, hit.Result.Damage);
        Assert.NotNull(brute);
    }

    [Fact]
    public void InvisibleHeroesAreIgnoredAndStrikeFromTheShadows()
    {
        var player = Hero(ClassAbility.Invisibility);
        player.Give(new ItemInstance(TestItems.Sword));
        var (floor, hero) = Setup(player);
        var monster = Dummy(floor, 10, 5, 2);
        floor.UseAbility(hero.Id);

        var quiet = Run(floor, 1);
        Assert.DoesNotContain(quiet, e => e is MonsterWoke);

        monster.Position = new Vector(4, 2);
        var events = Swing(floor);

        var hit = Assert.Single(events.OfType<MonsterHit>());
        Assert.True(hit.Result.IsCritical);
        Assert.False(player.IsInvisible);
    }

    [Fact]
    public void PowerShotPiercesTheWholeRow()
    {
        var player = Hero(ClassAbility.PowerShot);
        player.Give(new ItemInstance(TestGear.Bow));
        var (floor, hero) = Setup(player);
        Dummy(floor, 10, 6, 2);
        Dummy(floor, 11, 9, 2);
        floor.SetInput(1, HeroInput.Of(0, 0, false, 1, 0));
        floor.Step();

        floor.UseAbility(hero.Id);
        var hits = Run(floor, 1).OfType<MonsterHit>().Select(hit => hit.MonsterId).ToList();

        Assert.Contains(10, hits);
        Assert.Contains(11, hits);
    }

    [Fact]
    public void OverloadMakesTheNextSpellFree()
    {
        var player = Hero(ClassAbility.Overload, maxMana: 20);
        player.Give(new ItemInstance(Staff));
        var (floor, hero) = Setup(player);
        Dummy(floor, 10, 7, 2);
        floor.UseAbility(hero.Id);
        var mana = player.Mana;

        Swing(floor);

        Assert.Equal(mana, player.Mana);
        Assert.False(player.IsOverloaded);
    }
}
