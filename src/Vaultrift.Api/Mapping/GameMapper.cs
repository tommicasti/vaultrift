using Vaultrift.Api.Contracts;
using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Api.Mapping;

public static class GameMapper
{
    public static GameStateResponse ToResponse(GameSession session) => ToResponse(session.Id, session.Code, session.Game, session.BiomeOf);

    public static TurnResponse ToResponse(GameSession session, TurnResult result) =>
        new(
            result.TurnPassed,
            result.Events.Select(ToResponse).ToList(),
            ToResponse(session));

    private static GameStateResponse ToResponse(Guid id, string saveCode, Game game, Func<int, string?> biomeOf) =>
        new(
            id,
            saveCode,
            ToRows(game.Map),
            ToPlayerResponse(game, game.Player, game.FreeStepsLeft),
            game.Enemies
                .Where(enemy => !enemy.IsDead)
                .Select(ToEnemyResponse)
                .ToList(),
            game.FloorItems
                .Select(pair => new FloorItemResponse(pair.Key.X, pair.Key.Y, ToItemResponse(pair.Value)))
                .ToList(),
            game.Player.Inventory.Entries
                .Select(entry => new InventoryEntryResponse(entry.Id, ToItemResponse(entry.Item), entry.Quantity, entry.Mark, game.SellPrice(entry)))
                .ToList(),
            game.Player.Inventory.Capacity,
            game.Deaths,
            game.FloorNumber,
            game.FloorCount,
            game.Stairs is { } stairs ? new StairsResponse(stairs.X, stairs.Y, game.StairsOpen) : null,
            game.IsConquered,
            game.Biome,
            ToPositions(game.Chests),
            ToMerchantResponse(game),
            game.Difficulty,
            game.IsLost,
            game.Grave is { } grave ? new GraveResponse(grave.Position.X, grave.Position.Y, grave.Count) : null,
            game.Waystone is { } waystone
                ? new WaystoneResponse(waystone.X, waystone.Y, game.IsNextToWaystone,
                    game.TravelDestinations.Select(floor => new DestinationResponse(floor, biomeOf(floor), game.ClearedFloors.Contains(floor))).ToList())
                : null,
            game.Totem is { } totem
                ? new TotemResponse(totem.Position.X, totem.Position.Y, game.IsNextToTotem, game.TotemPrice, totem.Boss.Kind.Name, totem.Boss.Kind.Id)
                : null,
            game.DeepestFloor);

    private static PlayerResponse ToPlayerResponse(Game game, Player player, int freeSteps)
    {
        var combat = player.CombatStats;

        return new PlayerResponse(
            player.Class?.Id,
            player.Name,
            player.Level,
            player.Experience,
            player.ExperienceToNextLevel,
            player.UnspentPoints,
            player.Mana,
            player.MaxMana,
            player.Coins,
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
                .Select(pair => new EquippedItemResponse(pair.Key, pair.Value.Id, ToItemResponse(pair.Value.Item), pair.Value.Mark, game.SellPrice(pair.Value)))
                .ToList(),
            Enumerable.Range(0, Player.QuickSlotCount).Select(slot => ToQuickSlotResponse(player, slot)).ToList(),
            player.Spells.Select(spell => spell is null ? null : ToSpellResponse(spell, player.SpellCost(spell))).ToList(),
            player.Spells.ElementAtOrDefault(player.ActiveSpellIndex) is null ? 0 : player.ActiveSpellIndex,
            player.IsOverloaded,
            player.Boosts
                .Select(boost => new ActiveBoostResponse(
                    ToItemResponse(new ItemInstance(boost.Source)),
                    ToModifierResponse(boost.Modifier),
                    boost.RemainingTurns))
                .ToList(),
            player.Resistance,
            player.PoisonTurns);
    }

    private static MerchantResponse? ToMerchantResponse(Game game)
    {
        if (game.Merchant is not { } merchant)
            return null;

        var inventory = game.Player.Inventory;

        return new MerchantResponse(
            merchant.Position.X,
            merchant.Position.Y,
            game.IsNextToMerchant,
            merchant.Offers.Select(offer => new OfferResponse(offer.Id, ToItemResponse(offer.Item), offer.Price, offer.Kind)).ToList(),
            inventory.CanExpand ? Merchant.UpgradePrice(inventory) : null,
            Inventory.ExpansionSize);
    }

    private static QuickSlotResponse ToQuickSlotResponse(Player player, int slot)
    {
        if (player.QuickSlots[slot] is not { } item)
            return new QuickSlotResponse(null, 0);

        var quantity = player.Inventory.Entries.Where(entry => entry.Item.Id == item.Id).Sum(entry => entry.Quantity);
        return new QuickSlotResponse(ToItemResponse(new ItemInstance(item)), quantity);
    }

    private static SpellResponse ToSpellResponse(SpellDefinition spell, int manaCost) =>
        new(spell.Id, spell.Name, spell.Element, spell.Shape, spell.Power, manaCost, spell.Size, spell.ManaPercent);

    private static AbilityResponse? ToAbilityResponse(Player player)
    {
        if (player.Ability is not { } ability)
            return null;

        var activeTurns = ability switch
        {
            ClassAbility.Bulwark => player.BulwarkTurns,
            ClassAbility.Fury => player.FuryTurns,
            ClassAbility.Invisibility => player.InvisibleTurns,
            ClassAbility.Overload => player.IsOverloaded ? 1 : 0,
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
            enemy.Kind.IsBoss,
            enemy.MarkedTurns,
            enemy.VenomStacks,
            enemy.StaggeredTurns > 0);

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
        ItemMarked e => new("ItemMarked", Item: ToItemResponse(e.Item), Reason: e.Mark.ToString()),
        JunkDiscarded e => new("JunkDiscarded", Amount: e.Count),
        ChestOpened e => new("ChestOpened", X: e.At.X, Y: e.At.Y, Item: e.Item is null ? null : ToItemResponse(e.Item)),
        MimicRevealed e => new("MimicRevealed", EnemyId: e.EnemyId, X: e.At.X, Y: e.At.Y),
        ItemEquipped e => new("ItemEquipped", Item: ToItemResponse(e.Item)),
        EquipRejected e => new("EquipRejected", Reason: e.Reason.ToString()),
        ItemUnequipped e => new("ItemUnequipped", Item: ToItemResponse(e.Item)),
        UnequipRejected e => new("UnequipRejected", Reason: e.Reason.ToString()),
        ItemUsed e => new("ItemUsed", Item: ToItemResponse(e.Item)),
        UseRejected e => new("UseRejected", Reason: e.Reason.ToString()),
        QuickSlotAssigned e => new("QuickSlotAssigned", Item: ToItemResponse(new ItemInstance(e.Item)), Slot: e.Slot),
        SpellCast e => new("SpellCast", Element: e.Spell.Element, Path: ToPositions(e.Path), Tiles: ToPositions(e.Area),
            Spell: ToSpellResponse(e.Spell, e.Spell.ManaCost)),
        SpellSelected e => new("SpellSelected", Amount: e.Index),
        SpellLearned e => new("SpellLearned", Spell: ToSpellResponse(e.Spell, e.Spell.ManaCost), Slot: e.Key,
            Replaced: e.Replaced is null ? null : ToSpellResponse(e.Replaced, e.Replaced.ManaCost)),
        SpellRejected e => new("SpellRejected", Reason: e.Reason.ToString()),
        ManaSiphoned e => new("ManaSiphoned", Amount: e.Amount),
        CoinsGained e => new("CoinsGained", Amount: e.Amount),
        CoinsLost e => new("CoinsLost", Amount: e.Amount),
        MerchantGreeted => new("MerchantGreeted"),
        ItemBought e => new("ItemBought", Item: ToItemResponse(e.Item), Price: e.Price),
        ItemSold e => new("ItemSold", Item: ToItemResponse(e.Item), Price: e.Price),
        JunkSold e => new("JunkSold", Amount: e.Count, Price: e.Price),
        BackpackExpanded e => new("BackpackExpanded", Amount: e.Capacity, Price: e.Price),
        TradeRejected e => new("TradeRejected", Reason: e.Reason.ToString()),
        EnemyPulled e => new("EnemyPulled", EnemyId: e.EnemyId, X: e.To.X, Y: e.To.Y, Element: Element.Wind),
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
        PlayerScorched e => new("PlayerScorched", Damage: e.Damage, Element: Element.Fire),
        EnemyScorched e => new("EnemyScorched", EnemyId: e.EnemyId, Damage: e.Damage, Element: Element.Fire),
        LavaSpread e => new("LavaSpread", EnemyId: e.EnemyId, Tiles: ToPositions(e.Tiles)),
        PlayerPulled e => new("PlayerPulled", EnemyId: e.EnemyId, X: e.To.X, Y: e.To.Y),
        LavaCooled e => new("LavaCooled", Tiles: ToPositions(e.Tiles)),
        CrystalShattered e => new("CrystalShattered", X: e.At.X, Y: e.At.Y),
        EnemyShardHit e => new("EnemyShardHit", EnemyId: e.EnemyId, Damage: e.Damage),
        PlayerShardHit e => new("PlayerShardHit", Damage: e.Damage),
        CrystalsGrew e => new("CrystalsGrew", EnemyId: e.EnemyId, Tiles: ToPositions(e.Tiles)),
        CrystalsBurst e => new("CrystalsBurst", EnemyId: e.EnemyId, Tiles: ToPositions(e.Tiles)),
        PlayerPoisoned e => new("PlayerPoisoned", Amount: e.Turns),
        PlayerPoisonDamaged e => new("PlayerPoisonDamaged", Damage: e.Damage),
        PlayerDrifted e => new("PlayerDrifted", X: e.To.X, Y: e.To.Y),
        EnemyDrifted e => new("EnemyDrifted", EnemyId: e.EnemyId, X: e.To.X, Y: e.To.Y),
        PlayerBlownAway e => new("PlayerBlownAway", EnemyId: e.EnemyId, X: e.To.X, Y: e.To.Y, Damage: e.Damage, Element: Element.Wind),
        EnemySummoned e => new("EnemySummoned", EnemyId: e.MinionId, X: e.At.X, Y: e.At.Y),
        EnemyWithered e => new("EnemyWithered", EnemyId: e.EnemyId),
        GraveDug e => new("GraveDug", X: e.At.X, Y: e.At.Y, Amount: e.Count),
        GraveRecovered e => new("GraveRecovered", Amount: e.Count, Level: e.Left),
        GraveLost e => new("GraveLost", X: e.At.X, Y: e.At.Y, Amount: e.Count),
        HeroLost => new("HeroLost"),
        Traveled e => new("Traveled", X: e.Start.X, Y: e.Start.Y, Floor: e.Floor),
        TravelRejected e => new("TravelRejected", Reason: e.Reason.ToString()),
        TotemAwakened e => new("TotemAwakened", EnemyId: e.EnemyId, Price: e.Price),
        TotemRejected e => new("TotemRejected", Reason: e.Reason.ToString()),
        ExoticTriggered e => new("ExoticTriggered", EnemyId: e.EnemyId, Amount: e.Amount, Power: e.Power),
        EnemyEnvenomed e => new("EnemyEnvenomed", EnemyId: e.EnemyId, Damage: e.Damage, Amount: e.Stacks),
        SpareWeaponTaken e => new("SpareWeaponTaken", Item: ToItemResponse(e.Item)),
        WeaponImbued e => new("WeaponImbued", Item: ToItemResponse(e.Weapon)),
        BossPhaseChanged e => new("BossPhaseChanged", EnemyId: e.EnemyId, Level: e.Phase, Reason: e.Power.ToString()),
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
            instance.Element ?? (item as RuneDefinition)?.Element,
            EffectChanceOf(instance),
            EffectsOf(item),
            item is WeaponDefinition { IsTwoHanded: true },
            (item as GearDefinition)?.ClassId,
            potion is { ManaRestore: > 0 } ? potion.ManaRestore : null,
            SpellOf(item) is { } spell ? ToSpellResponse(spell, spell.ManaCost) : null);
    }

    private static SpellDefinition? SpellOf(ItemDefinition item) => item switch
    {
        WeaponDefinition weapon => weapon.Spell,
        ScrollDefinition scroll => scroll.Spell,
        _ => null
    };

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
        ScrollDefinition => "Scroll",
        RuneDefinition => "Rune",
        _ => throw new ArgumentOutOfRangeException(nameof(item), item.GetType().Name, "Unknown item type.")
    };

    private static char SymbolOf(TileType tile) => tile switch
    {
        TileType.Wall => '#',
        TileType.Obstacle => 'o',
        TileType.Lava => 'L',
        TileType.Water => '~',
        TileType.Crystal => 'C',
        TileType.Spores => 'S',
        TileType.CurrentUp => '^',
        TileType.CurrentDown => 'v',
        TileType.CurrentLeft => '<',
        TileType.CurrentRight => '>',
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