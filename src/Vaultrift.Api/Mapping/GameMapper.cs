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
            game.Deaths,
            game.FloorNumber,
            game.FloorCount,
            game.Stairs is { } stairs ? new StairsResponse(stairs.X, stairs.Y, game.StairsOpen) : null,
            game.IsConquered,
            game.Biome,
            ToPositions(game.Chests));

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
            player.Experience,
            player.ExperienceToNextLevel,
            player.UnspentPoints,
            player.Mana,
            player.MaxMana,
            player.Position.X,
            player.Position.Y,
            player.Facing,
            player.Health,
            player.Stats.MaxHealth,
            combat.Attack,
            combat.Defense,
            Math.Min(combat.Evasion, DamageCalculator.MaxEvasion),
            Math.Min(combat.Critical, DamageCalculator.MaxCritical),
            Math.Min(combat.Block, DamageCalculator.MaxBlock),
            freeSteps,
            ToAbilityResponse(player),
            player.Equipment
                .Select(pair => new EquippedItemResponse(pair.Key, pair.Value.Id, ToItemResponse(pair.Value.Item)))
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

    private static AbilityResponse? ToAbilityResponse(Player player)
    {
        if (player.Ability is not { } ability)
            return null;

        var activeTurns = ability switch
        {
            ClassAbility.Bulwark => player.BulwarkTurns,
            ClassAbility.Fury => player.FuryTurns,
            ClassAbility.Invisibility => player.InvisibleTurns,
            _ => 0
        };

        return new AbilityResponse(ability, player.AbilityCooldown, AbilityRules.Cooldown(ability), activeTurns);
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
            enemy.IsFrozen,
            enemy.Kind.IsBoss);

    private static EventResponse ToResponse(GameEvent gameEvent) => gameEvent switch
    {
        PlayerMoved e => new("PlayerMoved", X: e.To.X, Y: e.To.Y),
        PlayerTurned => new("PlayerTurned"),
        PlayerAttackedEnemy e => Attack("PlayerAttackedEnemy", e.EnemyId, e.Result),
        PlayerAttackedNothing => new("PlayerAttackedNothing"),
        EnemyDefeated e => new("EnemyDefeated", EnemyId: e.EnemyId),
        ExperienceGained e => new("ExperienceGained", Amount: e.Amount),
        LeveledUp e => new("LeveledUp", Level: e.Level),
        StatPointSpent e => new("StatPointSpent", Stat: e.Stat),
        SpendRejected e => new("SpendRejected", Reason: e.Reason.ToString()),
        EnemyAttackedPlayer e => Attack("EnemyAttackedPlayer", e.EnemyId, e.Result),
        EnemyWokeUp e => new("EnemyWokeUp", EnemyId: e.EnemyId),
        EnemyRespawned e => new("EnemyRespawned", EnemyId: e.EnemyId),
        EnemyMoved e => new("EnemyMoved", EnemyId: e.EnemyId, X: e.To.X, Y: e.To.Y),
        PlayerDefeated => new("PlayerDefeated"),
        PlayerRespawned e => new("PlayerRespawned", X: e.At.X, Y: e.At.Y),
        ItemPickedUp e => new("ItemPickedUp", Item: ToItemResponse(e.Item)),
        InventoryFull e => new("InventoryFull", Item: ToItemResponse(e.Item)),
        LifeDrained e => new("LifeDrained", Amount: e.Amount),
        EnemyPricked e => new("EnemyPricked", EnemyId: e.EnemyId, Damage: e.Damage),
        EnemySlammed e => new("EnemySlammed", EnemyId: e.EnemyId, Damage: e.Damage, Element: Element.Wind),
        AbilityUsed e => new("AbilityUsed", Ability: e.Ability),
        AbilityRejected e => new("AbilityRejected", Reason: e.Reason.ToString()),
        AbilityEnded e => new("AbilityEnded", Ability: e.Ability),
        ItemDropped e => new("ItemDropped", EnemyId: e.EnemyId, X: e.At.X, Y: e.At.Y, Item: ToItemResponse(e.Item)),
        ItemDiscarded e => new("ItemDiscarded", Item: ToItemResponse(e.Item)),
        ChestOpened e => new("ChestOpened", X: e.At.X, Y: e.At.Y, Item: e.Item is null ? null : ToItemResponse(e.Item)),
        MimicRevealed e => new("MimicRevealed", EnemyId: e.EnemyId, X: e.At.X, Y: e.At.Y),
        ItemEquipped e => new("ItemEquipped", Item: ToItemResponse(e.Item)),
        EquipRejected e => new("EquipRejected", Reason: e.Reason.ToString()),
        ItemUnequipped e => new("ItemUnequipped", Item: ToItemResponse(e.Item)),
        UnequipRejected e => new("UnequipRejected", Reason: e.Reason.ToString()),
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
        ArrowFlew e => new("ArrowFlew", Tiles: ToPositions(e.Path), Powered: e.Powered),
        PlayerSwept e => new("PlayerSwept", Tiles: ToPositions(e.Tiles)),
        PlayerThrust e => new("PlayerThrust", Tiles: ToPositions(e.Tiles)),
        StairsSealed => new("StairsSealed"),
        StairsOpened e => new("StairsOpened", X: e.At.X, Y: e.At.Y),
        FloorDescended e => new("FloorDescended", X: e.At.X, Y: e.At.Y, Floor: e.Floor),
        VaultConquered => new("VaultConquered"),
        _ => throw new ArgumentOutOfRangeException(nameof(gameEvent), gameEvent.GetType().Name, "Unknown game event.")
    };

    private static EventResponse Attack(string type, int enemyId, AttackResult result) =>
        new(
            type,
            EnemyId: enemyId,
            Damage: result.Damage,
            IsCritical: result.IsCritical,
            IsDodged: result.IsDodged,
            IsBlocked: result.IsBlocked,
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
            EffectChanceOf(instance),
            EffectsOf(item),
            item is WeaponDefinition { IsTwoHanded: true },
            (item as GearDefinition)?.ClassId,
            potion is { ManaRestore: > 0 } ? potion.ManaRestore : null);
    }

    private static IReadOnlyList<EffectResponse> EffectsOf(ItemDefinition item) =>
        item is EquipmentDefinition equipment
            ? equipment.Effects.Select(effect => new EffectResponse(effect.Kind, effect.Amount)).ToList()
            : [];

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

    private static char SymbolOf(TileType tile) => tile switch
    {
        TileType.Wall => '#',
        TileType.Obstacle => 'o',
        _ => '.'
    };

    private static IReadOnlyList<string> ToRows(DungeonMap map)
    {
        var rows = new List<string>(map.Height);

        for (var y = 0; y < map.Height; y++)
        {
            var row = new char[map.Width];

            for (var x = 0; x < map.Width; x++)
                row[x] = SymbolOf(map.GetTile(new Position(x, y)));

            rows.Add(new string(row));
        }

        return rows;
    }
}