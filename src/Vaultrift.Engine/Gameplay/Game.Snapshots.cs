using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Persistence;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    private Game(Player player, IFloorProvider floors, IRandom random, ILootTable? loot, GameSnapshot snapshot, IGameContent content)
    {
        Player = player;
        _floors = floors;
        _random = random;
        _loot = loot;
        _damageCalculator = new DamageCalculator(random);

        Map = DungeonMap.Parse(snapshot.Map);
        _enemies = snapshot.Enemies.Select(enemy => RestoreEnemy(enemy, content)).ToList();

        foreach (var item in snapshot.FloorItems)
            _floorItems[item.Position] = Player.RestoreItem(item.Item, content);

        foreach (var chest in snapshot.Chests)
            _chests[chest.Position] = new Chest(chest.Position, chest.Mimic is { } mimic ? RestoreEnemy(mimic, content) : null);

        foreach (var lava in snapshot.CoolingLava)
            _coolingLava[lava.Position] = lava.TurnsLeft;

        Merchant = snapshot.Merchant is { } merchant
            ? Merchant.Restore(merchant.Position,
                merchant.Offers.Select(offer => new Offer(offer.Id, Player.RestoreItem(offer.Item, content), offer.Price, offer.Kind)))
            : null;

        FloorNumber = snapshot.FloorNumber;
        Deaths = snapshot.Deaths;
        TurnNumber = snapshot.TurnNumber;
        _stepsThisTurn = snapshot.StepsThisTurn;
        _bossDefeatAnnounced = snapshot.BossDefeatAnnounced;
        IsConquered = snapshot.IsConquered && FloorNumber >= FloorCount;
        Biome = snapshot.Biome;
        Stairs = snapshot.Stairs ?? (FloorNumber < FloorCount ? snapshot.PlayerStart : null);
        _playerStart = snapshot.PlayerStart;
        Difficulty = snapshot.Difficulty;
        IsLost = snapshot.IsLost;
        Grave = snapshot.Grave is { } grave
            ? new Grave(grave.Position, grave.Items.Select(item => (item.Slot, Player.RestoreEntry(item.Entry, content))).ToList(), grave.Floor > 0 ? grave.Floor : snapshot.FloorNumber)
            : null;
        DeepestFloor = snapshot.DeepestFloor ?? FloorNumber;
        TotemUses = snapshot.TotemUses;
        Totem = snapshot.Totem is { } totem ? new BossTotem(totem.Position, RestoreEnemy(totem.Boss, content)) : null;

        foreach (var cleared in snapshot.ClearedFloors ?? ClearedBefore(snapshot))
            _clearedFloors.Add(cleared);

        Waystone = snapshot.DeepestFloor is null ? PlaceWaystone() : snapshot.Waystone;
    }

    private IEnumerable<int> ClearedBefore(GameSnapshot snapshot) =>
        Enumerable.Range(1, Math.Max(0, snapshot.FloorNumber - 1))
            .Concat(HasBoss && BossesDefeated ? [snapshot.FloorNumber] : []);

    public static Game Restore(
        GameSnapshot snapshot,
        IGameContent content,
        IFloorProvider floors,
        IRandom random,
        ILootTable? loot = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(floors);
        ArgumentNullException.ThrowIfNull(random);

        return new Game(Player.Restore(snapshot.Player, content), floors, random, loot, snapshot, content);
    }

    public GameSnapshot Capture() => new(
        FloorNumber,
        Deaths,
        TurnNumber,
        _stepsThisTurn,
        _bossDefeatAnnounced,
        IsConquered,
        Biome,
        Stairs,
        _playerStart,
        Map.ToRows(),
        Player.Capture(),
        _enemies.Select(CaptureEnemy).ToList(),
        _floorItems.Select(pair => new FloorItemSnapshot(pair.Key, Player.CaptureItem(pair.Value))).ToList(),
        _chests.Values.Select(chest => new ChestSnapshot(chest.Position, chest.Mimic is { } mimic ? CaptureEnemy(mimic) : null)).ToList(),
        Merchant is { } merchant
            ? new MerchantSnapshot(merchant.Position,
                merchant.Offers.Select(offer => new OfferSnapshot(offer.Id, Player.CaptureItem(offer.Item), offer.Price, offer.Kind)).ToList())
            : null,
        _coolingLava.Select(pair => new CoolingLavaSnapshot(pair.Key, pair.Value)).ToList(),
        Difficulty,
        IsLost,
        Grave is { } grave
            ? new GraveSnapshot(grave.Position, grave.Items.Select(item => new GraveItemSnapshot(item.Slot, Player.CaptureEntry(item.Entry))).ToList(), grave.Floor)
            : null,
        DeepestFloor,
        _clearedFloors.ToList(),
        Waystone,
        Totem is { } totem ? new TotemSnapshot(totem.Position, CaptureEnemy(totem.Boss)) : null,
        TotemUses);

    private static EnemySnapshot CaptureEnemy(Enemy enemy) => new(
        enemy.Id,
        enemy.Kind.Id,
        enemy.Rarity,
        enemy.Level,
        enemy.StartPosition,
        enemy.Position,
        enemy.Health,
        enemy.IsAwake,
        enemy.IsResting,
        enemy.BurnTurnsLeft,
        enemy.BurnDamage,
        enemy.FrozenTurnsLeft,
        enemy.FreezeImmunityTurnsLeft,
        enemy.PowerCharge,
        enemy.SummonerId,
        enemy.MarkedTurns,
        enemy.VenomStacks,
        enemy.StaggeredTurns);

    private static Enemy RestoreEnemy(EnemySnapshot snapshot, IGameContent content)
    {
        var enemy = new Enemy(snapshot.Id, snapshot.Start, content.EnemyKind(snapshot.KindId), snapshot.Rarity, snapshot.Level)
        {
            SummonerId = snapshot.SummonerId,
            Position = snapshot.Position,
            IsAwake = snapshot.IsAwake,
            IsResting = snapshot.IsResting,
            BurnTurnsLeft = snapshot.BurnTurnsLeft,
            BurnDamage = snapshot.BurnDamage,
            FrozenTurnsLeft = snapshot.FrozenTurnsLeft,
            FreezeImmunityTurnsLeft = snapshot.FreezeImmunityTurnsLeft,
            PowerCharge = snapshot.PowerCharge,
            MarkedTurns = snapshot.MarkedTurns,
            VenomStacks = snapshot.VenomStacks,
            StaggeredTurns = snapshot.StaggeredTurns
        };

        enemy.RestoreHealth(snapshot.Health);
        return enemy;
    }
}
