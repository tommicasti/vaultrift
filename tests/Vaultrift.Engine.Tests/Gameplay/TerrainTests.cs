using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Gameplay;

public class TerrainTests
{
    private const double Succeeds = 0.01;

    private static readonly EnemyKind Dummy = new("test-dummy", "Dummy", new Stats(maxHealth: 40, attack: 1, defense: 0));

    private static readonly EnemyKind Salamander =
        new("test-salamander", "Salamander", new Stats(maxHealth: 40, attack: 1, defense: 0), resistance: Element.Fire);

    private static Player NewPlayer(int x = 1, int y = 1) =>
        new(new Position(x, y), new Stats(maxHealth: 40, attack: 5, defense: 0)) { Facing = Direction.Right };

    private static Game NewGame(string[] rows, Player player, params Enemy[] enemies) =>
        new(DungeonMap.Parse(rows), player, enemies, new FixedRandom(Dice.AlwaysFail()));

    private static Game NewGame(string[] rows, Player player, double[] firstRolls, params Enemy[] enemies) =>
        new(DungeonMap.Parse(rows), player, enemies, new FixedRandom(Dice.Then(firstRolls)));

    [Fact]
    public void LavaAndWater_AreWalkable()
    {
        var map = DungeonMap.Parse(["#####", "#.L~#", "#####"]);

        Assert.True(map.IsWalkable(new Position(2, 1)));
        Assert.True(map.IsWalkable(new Position(3, 1)));
        Assert.True(map.IsLava(new Position(2, 1)));
        Assert.True(map.IsWater(new Position(3, 1)));
    }

    [Fact]
    public void EndingATurnOnLava_BurnsTheHero()
    {
        var player = NewPlayer();
        var game = NewGame(["#####", "#.L.#", "#####"], player);

        var result = game.MovePlayer(Direction.Right);

        Assert.Contains(new PlayerScorched(Game.LavaDamage(40)), result.Events);
        Assert.Equal(40 - Game.LavaDamage(40), player.Health);
    }

    [Fact]
    public void AFireCloak_ProtectsFromLava()
    {
        var player = NewPlayer();
        player.Give(new ItemInstance(new GearDefinition("test-cloak", "Cloak", Rarity.Common, EquipmentKind.Cloak, [new StatModifier(StatType.Evasion, 1)]), Element.Fire));
        var game = NewGame(["#####", "#.L.#", "#####"], player);

        var result = game.MovePlayer(Direction.Right);

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is PlayerScorched);
    }

    [Fact]
    public void Monsters_WalkAroundLava()
    {
        var enemy = new Enemy(1, new Position(4, 1), Dummy) { IsAwake = true };
        var game = NewGame(["######", "#..L.#", "#....#", "######"], NewPlayer(), enemy);

        game.MovePlayer(Direction.Down);

        Assert.Equal(new Position(4, 2), enemy.Position);
    }

    [Fact]
    public void FireproofMonsters_WalkStraightThroughLava()
    {
        var enemy = new Enemy(1, new Position(4, 1), Salamander) { IsAwake = true };
        var game = NewGame(["######", "#..L.#", "#....#", "######"], NewPlayer(), enemy);

        var result = game.MovePlayer(Direction.Down);

        Assert.Equal(new Position(3, 1), enemy.Position);
        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyScorched);
    }

    [Fact]
    public void Wind_CanPushAMonsterIntoLava()
    {
        var player = NewPlayer();
        player.Give(new ItemInstance(TestItems.Sword, Element.Wind));
        var enemy = new Enemy(1, new Position(2, 1), Dummy);
        var game = NewGame(["######", "#..L.#", "######"], player, [Dice.Fails, Succeeds], enemy);

        var result = game.Attack();

        Assert.Contains(new EnemyKnockedBack(1, new Position(3, 1)), result.Events);
        Assert.Contains(new EnemyScorched(1, Game.LavaDamage(40)), result.Events);
    }

    [Fact]
    public void Lightning_DealsDoubleDamage_ToACreatureInWater()
    {
        var player = NewPlayer();
        player.Give(new ItemInstance(TestItems.Sword, Element.Lightning));
        var enemy = new Enemy(1, new Position(2, 1), Dummy);
        var game = NewGame(["#####", "#.~.#", "#####"], player, [Dice.Fails, Dice.Fails], enemy);

        var hit = Assert.Single(game.Attack().Events.OfType<PlayerAttackedEnemy>()).Result;

        Assert.Equal(16, hit.Damage);
        Assert.Equal(Effectiveness.Effective, hit.Effectiveness);
    }

    [Fact]
    public void Fire_CannotIgniteACreatureInWater()
    {
        var player = NewPlayer();
        player.Give(new ItemInstance(TestItems.Sword, Element.Fire));
        var enemy = new Enemy(1, new Position(2, 1), Dummy);
        var game = NewGame(["#####", "#.~.#", "#####"], player, [Dice.Fails, Succeeds], enemy);

        var result = game.Attack();

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyIgnited);
        Assert.False(enemy.IsBurning);
    }

    [Fact]
    public void Water_PutsOutABurningMonster()
    {
        var enemy = new Enemy(1, new Position(3, 1), Dummy) { BurnTurnsLeft = 3, BurnDamage = 2 };
        var game = NewGame(["######", "#..~.#", "######"], NewPlayer(1, 1), enemy);

        var result = game.MovePlayer(Direction.Right);

        Assert.DoesNotContain(result.Events, gameEvent => gameEvent is EnemyBurned);
        Assert.False(enemy.IsBurning);
    }

    [Fact]
    public void LavaRing_TheBossSurroundsItselfWithLava_EveryThirdTurn()
    {
        var golemKind = new EnemyKind("test-golem", "Golem", new Stats(maxHealth: 60, attack: 1, defense: 0),
            resistance: Element.Fire, isBoss: true, power: BossPower.LavaRing);
        var golem = new Enemy(1, new Position(5, 2), golemKind) { IsAwake = true };
        var player = NewPlayer(1, 2);
        var game = NewGame(["#########", "#.......#", "#.......#", "#.......#", "#########"], player, golem);

        game.MovePlayer(Direction.Up);
        game.MovePlayer(Direction.Down);
        var third = game.MovePlayer(Direction.Up);

        var spread = Assert.Single(third.Events.OfType<LavaSpread>());
        Assert.NotEmpty(spread.Tiles);
        Assert.All(spread.Tiles, tile => Assert.True(game.Map.IsLava(tile)));
    }

    [Fact]
    public void TentaclePull_DragsTheHeroCloser()
    {
        var krakenKind = new EnemyKind("test-kraken", "Kraken", new Stats(maxHealth: 60, attack: 1, defense: 0),
            isBoss: true, isStationary: true, power: BossPower.TentaclePull);
        var kraken = new Enemy(1, new Position(6, 1), krakenKind) { IsAwake = true };
        var player = NewPlayer(1, 1);
        var game = NewGame(["#########", "#.......#", "#.......#", "#########"], player, kraken);

        game.MovePlayer(Direction.Down);
        game.MovePlayer(Direction.Up);
        var third = game.MovePlayer(Direction.Right);

        var pull = Assert.Single(third.Events.OfType<PlayerPulled>());
        Assert.Equal(new Position(3, 1), pull.To);
        Assert.Equal(new Position(3, 1), player.Position);
    }

    [Fact]
    public void TheGenerator_PoolsTerrainInsideRooms_AwayFromTheStartAndTheBoss()
    {
        var layout = FloorGenerator.Generate(new SystemRandom(new Random(3)), terrain: new TerrainSpec(TileType.Lava, 0.3));
        var lava = new List<Position>();

        for (var y = 0; y < layout.Map.Height; y++)
        {
            for (var x = 0; x < layout.Map.Width; x++)
            {
                if (layout.Map.IsLava(new Position(x, y)))
                    lava.Add(new Position(x, y));
            }
        }

        Assert.NotEmpty(lava);
        Assert.DoesNotContain(lava, tile => layout.StartRoom.Contains(tile));
        Assert.DoesNotContain(layout.BossRoom.Center, lava);
        Assert.All(lava, tile => Assert.Contains(layout.Rooms, room => room.Contains(tile)));
    }
}
