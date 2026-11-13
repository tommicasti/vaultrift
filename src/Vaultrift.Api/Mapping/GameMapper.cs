using Vaultrift.Api.Contracts;
using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Api.Mapping;

public static class GameMapper
{
    public static GameStateResponse ToResponse(Guid id, Game game) =>
        new(
            id,
            ToRows(game.Map),
            ToPlayerResponse(game.Player),
            game.Enemies
                .Where(enemy => !enemy.IsDead)
                .Select(enemy => new EnemyResponse(
                    enemy.Id,
                    enemy.Position.X,
                    enemy.Position.Y,
                    enemy.Health,
                    enemy.Stats.MaxHealth,
                    enemy.IsAwake))
                .ToList(),
            game.FloorItems
                .Select(pair => new FloorItemResponse(pair.Key.X, pair.Key.Y, ToItemResponse(pair.Value)))
                .ToList(),
            game.Player.Inventory.Entries
                .Select(entry => new InventoryEntryResponse(entry.Id, ToItemResponse(entry.Item), entry.Quantity))
                .ToList(),
            game.Player.Inventory.Capacity,
            game.Deaths);

    public static TurnResponse ToResponse(Guid id, Game game, TurnResult result) =>
        new(
            result.TurnPassed,
            result.Events.Select(ToResponse).ToList(),
            ToResponse(id, game));

    private static PlayerResponse ToPlayerResponse(Player player)
    {
        var combat = player.CombatStats;

        return new PlayerResponse(
            player.Position.X,
            player.Position.Y,
            player.Facing,
            player.Health,
            player.Stats.MaxHealth,
            combat.Attack,
            combat.Defense,
            Math.Min(combat.Evasion, DamageCalculator.MaxEvasion),
            player.Equipment
                .Select(pair => new EquippedItemResponse(pair.Key, pair.Value.Id))
                .ToList());
    }

    private static EventResponse ToResponse(GameEvent gameEvent) => gameEvent switch
    {
        PlayerMoved e => new("PlayerMoved", X: e.To.X, Y: e.To.Y),
        PlayerTurned => new("PlayerTurned"),
        PlayerAttackedEnemy e => new("PlayerAttackedEnemy", EnemyId: e.EnemyId, Damage: e.Result.Damage, IsCritical: e.Result.IsCritical, IsDodged: e.Result.IsDodged),
        PlayerAttackedNothing => new("PlayerAttackedNothing"),
        EnemyDefeated e => new("EnemyDefeated", EnemyId: e.EnemyId),
        EnemyAttackedPlayer e => new("EnemyAttackedPlayer", EnemyId: e.EnemyId, Damage: e.Result.Damage, IsCritical: e.Result.IsCritical, IsDodged: e.Result.IsDodged),
        EnemyWokeUp e => new("EnemyWokeUp", EnemyId: e.EnemyId),
        EnemyMoved e => new("EnemyMoved", EnemyId: e.EnemyId, X: e.To.X, Y: e.To.Y),
        PlayerDefeated => new("PlayerDefeated"),
        PlayerRespawned e => new("PlayerRespawned", X: e.At.X, Y: e.At.Y),
        ItemPickedUp e => new("ItemPickedUp", Item: ToItemResponse(e.Item)),
        InventoryFull e => new("InventoryFull", Item: ToItemResponse(e.Item)),
        ItemEquipped e => new("ItemEquipped", Item: ToItemResponse(e.Item)),
        EquipRejected e => new("EquipRejected", Reason: e.Reason.ToString()),
        _ => throw new ArgumentOutOfRangeException(nameof(gameEvent), gameEvent.GetType().Name, "Unknown game event.")
    };

    private static ItemResponse ToItemResponse(ItemDefinition item) =>
        new(item.Id, item.Name, item.Rarity, CategoryOf(item));

    private static string CategoryOf(ItemDefinition item) => item switch
    {
        WeaponDefinition weapon => weapon.WeaponType.ToString(),
        GearDefinition gear => gear.Kind.ToString(),
        PotionDefinition => "Potion",
        _ => throw new ArgumentOutOfRangeException(nameof(item), item.GetType().Name, "Unknown item type.")
    };

    private static IReadOnlyList<string> ToRows(DungeonMap map)
    {
        var rows = new List<string>(map.Height);

        for (var y = 0; y < map.Height; y++)
        {
            var row = new char[map.Width];

            for (var x = 0; x < map.Width; x++)
                row[x] = map.GetTile(new Position(x, y)) == TileType.Wall ? '#' : '.';

            rows.Add(new string(row));
        }

        return rows;
    }
}