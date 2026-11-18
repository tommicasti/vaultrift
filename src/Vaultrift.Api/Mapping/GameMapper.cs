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
            ToPlayerResponse(game.Player, game.FreeStepsLeft),
            game.Enemies
                .Where(enemy => !enemy.IsDead)
                .Select(ToEnemyResponse)
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

    private static PlayerResponse ToPlayerResponse(Player player, int freeSteps)
    {
        var combat = player.CombatStats;
        var quickSlot = player.QuickSlot;

        return new PlayerResponse(
            player.Class?.Id,
            player.Name,
            player.Level,
            player.Position.X,
            player.Position.Y,
            player.Facing,
            player.Health,
            player.Stats.MaxHealth,
            combat.Attack,
            combat.Defense,
            Math.Min(combat.Evasion, DamageCalculator.MaxEvasion),
            Math.Min(combat.Critical, DamageCalculator.MaxCritical),
            freeSteps,
            player.Equipment
                .Select(pair => new EquippedItemResponse(pair.Key, pair.Value.Id))
                .ToList(),
            quickSlot is null ? null : ToItemResponse(new ItemInstance(quickSlot)),
            quickSlot is null ? 0 : player.Inventory.Entries
                .Where(entry => entry.Item.Id == quickSlot.Id)
                .Sum(entry => entry.Quantity),
            player.Boosts
                .Select(boost => new ActiveBoostResponse(
                    ToItemResponse(new ItemInstance(boost.Source)),
                    ToModifierResponse(boost.Modifier),
                    boost.RemainingTurns))
                .ToList(),
            player.Resistance);
    }

    private static EnemyResponse ToEnemyResponse(Enemy enemy) =>
        new(
            enemy.Id,
            enemy.Kind.Id,
            enemy.Kind.Name,
            enemy.Rarity,
            enemy.Level,
            enemy.Position.X,
            enemy.Position.Y,
            enemy.Health,
            enemy.Stats.MaxHealth,
            enemy.IsAwake,
            enemy.Kind.Weakness,
            enemy.Kind.Resistance,
            enemy.Kind.AttackElement,
            enemy.IsBurning,
            enemy.IsFrozen);

    private static EventResponse ToResponse(GameEvent gameEvent) => gameEvent switch
    {
        PlayerMoved e => new("PlayerMoved", X: e.To.X, Y: e.To.Y),
        PlayerTurned => new("PlayerTurned"),
        PlayerAttackedEnemy e => Attack("PlayerAttackedEnemy", e.EnemyId, e.Result),
        PlayerAttackedNothing => new("PlayerAttackedNothing"),
        EnemyDefeated e => new("EnemyDefeated", EnemyId: e.EnemyId),
        EnemyAttackedPlayer e => Attack("EnemyAttackedPlayer", e.EnemyId, e.Result),
        EnemyWokeUp e => new("EnemyWokeUp", EnemyId: e.EnemyId),
        EnemyMoved e => new("EnemyMoved", EnemyId: e.EnemyId, X: e.To.X, Y: e.To.Y),
        PlayerDefeated => new("PlayerDefeated"),
        PlayerRespawned e => new("PlayerRespawned", X: e.At.X, Y: e.At.Y),
        ItemPickedUp e => new("ItemPickedUp", Item: ToItemResponse(e.Item)),
        InventoryFull e => new("InventoryFull", Item: ToItemResponse(e.Item)),
        ItemEquipped e => new("ItemEquipped", Item: ToItemResponse(e.Item)),
        EquipRejected e => new("EquipRejected", Reason: e.Reason.ToString()),
        ItemUsed e => new("ItemUsed", Item: ToItemResponse(e.Item)),
        UseRejected e => new("UseRejected", Reason: e.Reason.ToString()),
        QuickSlotAssigned e => new("QuickSlotAssigned", Item: ToItemResponse(new ItemInstance(e.Item))),
        BoostExpired e => new("BoostExpired", Item: ToItemResponse(new ItemInstance(e.Item))),
        EnemyIgnited e => new("EnemyIgnited", EnemyId: e.EnemyId, Element: Element.Fire),
        EnemyBurned e => new("EnemyBurned", EnemyId: e.EnemyId, Damage: e.Damage, Element: Element.Fire),
        EnemyFrozen e => new("EnemyFrozen", EnemyId: e.EnemyId, Element: Element.Ice),
        EnemyThawed e => new("EnemyThawed", EnemyId: e.EnemyId, Element: Element.Ice),
        EnemyShocked e => Attack("EnemyShocked", e.EnemyId, e.Result),
        EnemyKnockedBack e => new("EnemyKnockedBack", EnemyId: e.EnemyId, X: e.To.X, Y: e.To.Y, Element: Element.Wind),
        ArrowFlew e => new("ArrowFlew", Tiles: ToPositions(e.Path)),
        PlayerSwept e => new("PlayerSwept", Tiles: ToPositions(e.Tiles)),
        _ => throw new ArgumentOutOfRangeException(nameof(gameEvent), gameEvent.GetType().Name, "Unknown game event.")
    };

    private static EventResponse Attack(string type, int enemyId, AttackResult result) =>
        new(
            type,
            EnemyId: enemyId,
            Damage: result.Damage,
            IsCritical: result.IsCritical,
            IsDodged: result.IsDodged,
            Element: result.Element,
            Effectiveness: result.Element is null ? null : result.Effectiveness);

    private static IReadOnlyList<PositionResponse> ToPositions(IEnumerable<Position> positions) =>
        positions.Select(position => new PositionResponse(position.X, position.Y)).ToList();

    private static ItemResponse ToItemResponse(ItemInstance instance)
    {
        var item = instance.Definition;
        var potion = item as PotionDefinition;

        return new ItemResponse(
            instance.Id,
            instance.Name,
            instance.Rarity,
            CategoryOf(item),
            item.Description,
            ModifiersOf(item),
            potion is { Healing: > 0 } ? potion.Healing : null,
            potion?.Boost is { } boost ? ToModifierResponse(boost) : null,
            potion?.Boost is not null ? potion.BoostTurns : null,
            instance.Element,
            EffectChanceOf(instance));
    }

    private static int? EffectChanceOf(ItemInstance instance) =>
        instance.Element is { } element && instance.Definition is WeaponDefinition
            ? (int)Math.Round(ElementRules.EffectChance(element, instance.Rarity) * 100)
            : null;

    private static IReadOnlyList<ModifierResponse> ModifiersOf(ItemDefinition item) =>
        item is EquipmentDefinition equipment
            ? equipment.Modifiers.Select(ToModifierResponse).ToList()
            : [];

    private static ModifierResponse ToModifierResponse(StatModifier modifier) =>
        new(modifier.Stat, modifier.Amount);

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