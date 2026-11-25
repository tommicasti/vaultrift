using System.Text.Json;
using System.Text.Json.Serialization;
using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Persistence;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.Tests.Fakes;
using Vaultrift.Engine.Tests.Support;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Tests.Persistence;

public class SnapshotTests
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    private static readonly EnemyKind Rat = new("test-rat", "Rat", new Stats(maxHealth: 12, attack: 3, defense: 0), weakness: Element.Fire);
    private static readonly EnemyKind Mimic = new("test-mimic", "Mimic", new Stats(maxHealth: 20, attack: 4, defense: 1), isStationary: true);

    private static readonly PotionDefinition Strength =
        new("test-strength", "Strength", Rarity.Rare, Boost: new StatModifier(StatType.Attack, 3), BoostTurns: 10);

    private static readonly CharacterClass Hero = new(
        "test-hero", "Hero", new Stats(maxHealth: 40, attack: 6, defense: 1, maxMana: 20),
        new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Attack, 1)), Ability: ClassAbility.Fury);

    private static readonly IGameContent Content = new FakeContent()
        .With(TestItems.Sword, TestItems.Axe, TestItems.Ring, TestItems.HealingPotion, Strength)
        .With(Rat, Mimic)
        .With(Hero);

    private static readonly DungeonMap Hall = DungeonMap.Parse(
    [
        "##############",
        "#............#",
        "#..L.....~...#",
        "#............#",
        "##############"
    ]);

    private static (Game Game, SeededRandom Random) NewGame()
    {
        var random = SeededRandom.FromSeed(99);
        var player = new Player(new Position(1, 1), Hero) { Facing = Direction.Right };
        player.Inventory.TryAdd(new ItemInstance(TestItems.Sword, Element.Fire));
        player.Inventory.TryAdd(new ItemInstance(TestItems.HealingPotion));
        player.Inventory.TryAdd(new ItemInstance(TestItems.HealingPotion));
        player.Inventory.TryAdd(new ItemInstance(Strength));
        player.Inventory.TryAdd(new ItemInstance(TestItems.Ring));

        var floor = new Floor(
            Hall,
            player.Position,
            [new Enemy(1, new Position(5, 1), Rat, Rarity.Rare, 2), new Enemy(2, new Position(8, 3), Rat)],
            [new FloorItem(new Position(3, 3), new ItemInstance(TestItems.Axe))],
            new Position(12, 3),
            "test-biome",
            [new Chest(new Position(11, 1), new Enemy(3, new Position(11, 1), Mimic))],
            new Merchant(new Position(1, 3), [new ItemInstance(TestItems.Ring)], floor: 3));

        var game = new Game(player, new SingleFloor(floor), random);
        game.Equip(player.Inventory.Entries[0].Id);
        game.Mark(player.Inventory.Entries.First(entry => entry.Item.Definition == TestItems.Ring).Id, ItemMark.Junk);
        game.UseItem(player.Inventory.Entries.First(entry => entry.Item.Definition == Strength).Id);
        game.UseAbility();
        game.MovePlayer(Direction.Right);
        game.Attack();
        return (game, random);
    }

    private static string Json_(Game game) => JsonSerializer.Serialize(game.Capture(), Json);

    private static void PlayTheSameTurns(Game game)
    {
        foreach (var direction in new[] { Direction.Right, Direction.Right, Direction.Down, Direction.Right, Direction.Up })
        {
            game.MovePlayer(direction);
            game.Attack();
        }
    }

    [Fact]
    public void ARestoredGame_IsIdenticalToTheOriginal()
    {
        var (game, random) = NewGame();
        var snapshot = game.Capture();

        var restored = Game.Restore(snapshot, Content, new SingleFloor(new Floor(Hall, new Position(1, 1), [], [])), new SeededRandom(random.State));

        Assert.Equal(Json_(game), Json_(restored));
    }

    [Fact]
    public void ARestoredGame_PlaysOnExactlyLikeTheOriginal()
    {
        var (game, random) = NewGame();
        var restored = Game.Restore(game.Capture(), Content, new SingleFloor(new Floor(Hall, new Position(1, 1), [], [])), new SeededRandom(random.State));

        PlayTheSameTurns(game);
        PlayTheSameTurns(restored);

        Assert.Equal(Json_(game), Json_(restored));
        Assert.Equal(game.Player.Health, restored.Player.Health);
        Assert.Equal(game.TurnNumber, restored.TurnNumber);
    }

    [Fact]
    public void ASnapshot_SurvivesAJsonRoundTrip()
    {
        var (game, _) = NewGame();
        var json = Json_(game);

        var back = JsonSerializer.Deserialize<GameSnapshot>(json, Json)!;

        Assert.Equal(json, JsonSerializer.Serialize(back, Json));
    }

    [Fact]
    public void TheSnapshot_KeepsWhatTheHeroCarries()
    {
        var (game, _) = NewGame();

        var player = game.Capture().Player;

        Assert.Equal("test-hero", player.ClassId);
        Assert.Equal(Element.Fire, player.Equipment[EquipmentSlot.MainHand].Item.Element);
        Assert.Contains(player.Inventory.Entries, entry => entry.Item.Id == TestItems.Ring.Id && entry.Mark == ItemMark.Junk);
        Assert.Contains(player.Inventory.Entries, entry => entry.Item.Id == TestItems.HealingPotion.Id && entry.Quantity == 2);
        Assert.Equal("test-strength", Assert.Single(player.Boosts).PotionId);
        Assert.True(player.AbilityCooldown > 0);
    }

    [Fact]
    public void TheMapRows_ParseBackToTheSameMap()
    {
        var map = DungeonMap.Parse(["#####", "#.o.#", "#L~C#", "#S^v#", "#<>.#", "#####"]);

        Assert.Equal(map.ToRows(), DungeonMap.Parse(map.ToRows()).ToRows());
        Assert.Equal("#L~C#", map.ToRows()[2]);
    }

    [Fact]
    public void AGraveOnHard_SurvivesSavingAndReloading()
    {
        var brute = new EnemyKind("test-brute", "Brute", new Stats(maxHealth: 500, attack: 99, defense: 5));
        var fragile = new CharacterClass("test-fragile", "Fragile", new Stats(maxHealth: 2, attack: 5, defense: 0), new WeaponMastery(WeaponType.Sword, new StatModifier(StatType.Attack, 1)));
        var content = new FakeContent().With(TestItems.Sword).With(brute).With(fragile);
        var player = new Player(new Position(1, 1), fragile) { Facing = Direction.Right };
        player.Inventory.TryAdd(new ItemInstance(TestItems.Sword));
        var floor = new Floor(Hall, player.Position, [new Enemy(1, new Position(2, 1), brute)], []);
        var game = new Game(player, new SingleFloor(floor), SeededRandom.FromSeed(7)) { Difficulty = Difficulty.Hard };
        game.Equip(player.Inventory.Entries[0].Id);

        for (var turn = 0; turn < 20 && game.Deaths == 0; turn++)
            game.Attack();

        Assert.Equal(1, game.Deaths);
        Assert.NotNull(game.Grave);

        var saved = JsonSerializer.Deserialize<GameSnapshot>(JsonSerializer.Serialize(game.Capture(), Json), Json)!;
        var restored = Game.Restore(saved, content, new SingleFloor(floor), SeededRandom.FromSeed(7));

        Assert.Equal(game.Grave!.Position, restored.Grave?.Position);
        Assert.Contains(restored.Grave!.Items, item => item.Entry.Item.Definition == TestItems.Sword);
    }
}
