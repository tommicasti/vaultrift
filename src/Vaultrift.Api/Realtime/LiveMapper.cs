using Vaultrift.Api.Contracts;
using Vaultrift.Api.Mapping;
using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Realtime;
using Vaultrift.Engine.World;
using Vaultrift.Infrastructure.Realtime;
using Vaultrift.Infrastructure.Vault;

namespace Vaultrift.Api.Realtime;

public static class LiveMapper
{
    public static LiveWelcome Welcome(WorldSession world, HeroSession session, LiveFloor floor, LiveHero hero, string roomCode, bool isHost, bool isShared, IReadOnlyList<LivePartyMember> party, string resumeToken)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(hero);

        var state = GameMapper.ToResponse(session);

        return new LiveWelcome(
            hero.Id,
            hero.ClassId,
            hero.Name,
            floor.Biome,
            floor.Map.ToRows(),
            state.Player.Equipment,
            LiveRules.TicksPerSecond,
            LiveRules.MonsterWindupSeconds,
            LiveRules.HeroRadius + LiveRules.MonsterReach,
            HeroState(world, session, floor, hero),
            Items(floor),
            roomCode,
            isHost,
            LiveRules.MaxHeroes,
            party,
            floor.Chests.Select(chest => new LiveChestView(chest.Id, chest.Tile.X, chest.Tile.Y)).ToList(),
            floor.Stairs is { } stairs ? new LiveStairsView(stairs.X, stairs.Y, floor.StairsOpen) : null,
            world.Id,
            isShared,
            LiveRules.HeroSpeed(hero.Speed),
            LiveRules.HeroRadius,
            resumeToken,
            LiveRules.SprintFactor,
            LiveRules.CurrentSpeed,
            LiveRules.FixtureRadius,
            RoomHub.Protocol);
    }

    public static LiveHeroState HeroState(WorldSession world, HeroSession session, LiveFloor floor, LiveHero hero)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(hero);

        var state = GameMapper.ToResponse(session);
        var player = session.Player;
        var prices = player.Inventory.Entries.ToDictionary(entry => entry.Id, floor.SellPrice);

        return new LiveHeroState(
            state.Player,
            state.Inventory.Select(entry => entry with { SellPrice = prices.GetValueOrDefault(entry.Id) }).ToList(),
            state.InventoryCapacity,
            floor.FloorNumber,
            Math.Min(world.FloorCount, LiveArena.LastFloor),
            world.Difficulty,
            hero.Deaths,
            world.DeepestFloor,
            world.IsConquered,
            floor.Merchant is { } merchant
                ? new MerchantResponse(
                    merchant.Position.X,
                    merchant.Position.Y,
                    floor.IsAtMerchant(hero.Id),
                    merchant.Offers.Select(offer => new OfferResponse(offer.Id, GameMapper.ToItemResponse(offer.Item), offer.Price, offer.Kind)).ToList(),
                    player.Inventory.CanExpand ? Merchant.UpgradePrice(player.Inventory) : null,
                    Inventory.ExpansionSize)
                : null,
            floor.Waystone is { } waystone
                ? new WaystoneResponse(
                    waystone.X,
                    waystone.Y,
                    floor.IsAtWaystone(hero.Id),
                    Enumerable.Range(1, Math.Min(world.DeepestFloor, LiveArena.LastFloor))
                        .Where(number => number != floor.FloorNumber)
                        .Select(number => new DestinationResponse(number, world.BiomeOf(number), world.IsCleared(number), world.Graves.Any(grave => grave.Floor == number && grave.Owner == session.Id)))
                        .ToList())
                : null,
            floor.Totem is { } totem
                ? new TotemResponse(totem.Tile.X, totem.Tile.Y, floor.IsAtTotem(hero.Id), totem.Price, totem.Boss.Kind.Name, totem.Boss.Kind.Id)
                : null,
            floor.Graves.Select(grave => new LiveGraveView(grave.Id, grave.Tile.X, grave.Tile.Y, grave.Count, grave.OwnerName, grave.Owner == session.Id)).ToList());
    }

    public static IReadOnlyList<LiveItemView> Items(LiveFloor floor) =>
        floor.Items.Select(item => new LiveItemView(item.Id, item.Tile.X, item.Tile.Y, GameMapper.ToItemResponse(item.Item))).ToList();

    public static LiveSnapshot Snapshot(LiveFloor floor, IReadOnlyList<LiveEvent> events, LiveHeroState? hero = null, IReadOnlyList<LiveItemView>? items = null)
    {
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(events);

        return new LiveSnapshot(
            floor.Tick,
            Math.Round(floor.Time, 3),
            floor.Heroes.Select(ToView).ToList(),
            floor.Monsters.Where(monster => !monster.IsDead).Select(monster => ToView(floor, monster)).ToList(),
            events.Select(ToView).ToList(),
            floor.Projectiles.Select(ToView).ToList(),
            hero,
            items);
    }

    private static LiveHeroView ToView(LiveHero hero)
    {
        var profile = AttackProfile.For(hero.Player);

        return new(
            hero.Id,
            hero.Name,
            hero.ClassId,
            Round(hero.Position.X),
            Round(hero.Position.Y),
            Angle(hero.Facing),
            hero.Health,
            hero.MaxHealth,
            hero.Player.Mana,
            hero.Player.MaxMana,
            hero.IsMoving,
            hero.IsDead,
            profile.Style.ToString(),
            profile.Reach,
            profile.ArcDegrees,
            hero.Player.IsInvisible,
            hero.Player.BulwarkTurns > 0,
            hero.Player.FuryTurns > 0,
            hero.IsAway);
    }

    private static LiveProjectileView ToView(LiveProjectile projectile) =>
        new(
            projectile.Id,
            projectile.OwnerId,
            projectile.Kind.ToString(),
            Round(projectile.Position.X),
            Round(projectile.Position.Y),
            Angle(projectile.Direction),
            projectile.Element);

    private static LiveMonsterView ToView(LiveFloor floor, LiveMonster monster) =>
        new(
            monster.Id,
            monster.Kind.Id,
            monster.Kind.Name,
            monster.Rarity,
            monster.Level,
            monster.Kind.IsBoss,
            Round(monster.Position.X),
            Round(monster.Position.Y),
            Angle(monster.Facing),
            monster.Radius,
            monster.Health,
            monster.MaxHealth,
            monster.IsAwake,
            monster.IsMoving,
            monster.IsBurning,
            floor.IsFrozen(monster));

    private static LiveEventView ToView(LiveEvent liveEvent) => liveEvent switch
    {
        HeroSwung swung => new LiveEventView("HeroSwung", HeroId: swung.HeroId),
        MonsterHit hit => new LiveEventView("MonsterHit", hit.HeroId, hit.MonsterId, hit.Result.Damage, hit.Result.IsCritical, hit.Result.Missed, Element: hit.Result.Effectiveness == Effectiveness.Neutral ? null : hit.Result.Element, ItemName: hit.Result.Effectiveness == Effectiveness.Neutral ? null : hit.Result.Effectiveness.ToString()),
        MonsterSlain slain => new LiveEventView("MonsterSlain", slain.HeroId, slain.MonsterId),
        MonsterWoke woke => new LiveEventView("MonsterWoke", MonsterId: woke.MonsterId),
        MonsterWindup windup => new LiveEventView("MonsterWindup", windup.HeroId, windup.MonsterId),
        HeroHit hit => new LiveEventView("HeroHit", hit.HeroId, hit.MonsterId, hit.Result.Damage, hit.Result.IsCritical, hit.Result.Missed, Element: hit.Result.Effectiveness == Effectiveness.Neutral ? null : hit.Result.Element, ItemName: hit.Result.Effectiveness == Effectiveness.Neutral ? null : hit.Result.Effectiveness.ToString()),
        HeroFell fell => new LiveEventView("HeroFell", HeroId: fell.HeroId, Amount: fell.CoinsLost == 0 ? null : fell.CoinsLost),
        HeroRespawned respawned => new LiveEventView("HeroRespawned", HeroId: respawned.HeroId),
        HeroRewarded reward => new LiveEventView("HeroRewarded", reward.HeroId, reward.MonsterId, reward.Coins, Experience: reward.Experience, Levels: reward.LevelsGained),
        LootDropped dropped => new LiveEventView("LootDropped", MonsterId: dropped.MonsterId, ItemId: dropped.ItemId),
        LootPickedUp picked => new LiveEventView("LootPickedUp", picked.HeroId, ItemId: picked.ItemId, ItemName: picked.Item.Name),
        BackpackFull full => new LiveEventView("BackpackFull", HeroId: full.HeroId),
        BoostFaded faded => new LiveEventView("BoostFaded", HeroId: faded.HeroId, ItemName: faded.Source.Name),
        AbilityActivated used => new LiveEventView("AbilityActivated", used.HeroId, Ability: used.Ability.ToString()),
        AbilityFaded ended => new LiveEventView("AbilityFaded", ended.HeroId, Ability: ended.Ability.ToString()),
        MonsterIgnited ignited => new LiveEventView("MonsterIgnited", MonsterId: ignited.MonsterId, Element: Element.Fire),
        MonsterScorched scorched => new LiveEventView("MonsterScorched", scorched.HeroId, scorched.MonsterId, scorched.Damage, Element: Element.Fire),
        MonsterFrozen frozen => new LiveEventView("MonsterFrozen", MonsterId: frozen.MonsterId, Element: Element.Ice),
        MonsterPushed pushed => new LiveEventView("MonsterPushed", MonsterId: pushed.MonsterId, Element: Element.Wind),
        SpellUnleashed spell => new LiveEventView("SpellUnleashed", spell.HeroId, SpellId: spell.Spell.Id, Element: spell.Spell.Element, X: Round(spell.Center.X), Y: Round(spell.Center.Y), Radius: spell.Radius),
        ShotFired shot => new LiveEventView("ShotFired", shot.HeroId, ItemId: shot.ProjectileId),
        ChestLooted looted => new LiveEventView("ChestLooted", looted.HeroId, Amount: looted.Coins, ChestId: looted.ChestId),
        MimicSprang mimic => new LiveEventView("MimicSprang", mimic.HeroId, mimic.MonsterId, ChestId: mimic.ChestId),
        StairsUnsealed stairs => new LiveEventView("StairsUnsealed", X: stairs.At.X, Y: stairs.At.Y),
        HeroAtStairs atStairs => new LiveEventView("HeroAtStairs", HeroId: atStairs.HeroId),
        LiveMerchantGreeted greeted => new LiveEventView("MerchantGreeted", HeroId: greeted.HeroId),
        LiveWaystoneTouched touched => new LiveEventView("WaystoneTouched", HeroId: touched.HeroId),
        LiveGraveDug dug => new LiveEventView("GraveDug", dug.HeroId, Amount: dug.Count),
        LiveGraveRecovered recovered => new LiveEventView("GraveRecovered", recovered.HeroId, Amount: recovered.Recovered, Levels: recovered.Left),
        LiveGraveLost => new LiveEventView("GraveLost"),
        LiveTotemPrimed primed => new LiveEventView(primed.CanAfford ? "TotemPrimed" : "TotemTooExpensive", primed.HeroId, Amount: primed.Price),
        LiveTotemAwakened awakened => new LiveEventView("TotemAwakened", awakened.HeroId, awakened.MonsterId, awakened.Price),
        LiveBossPhaseChanged phase => new LiveEventView("BossPhaseChanged", MonsterId: phase.MonsterId, Ability: phase.Power.ToString(), Phase: phase.Phase),
        TilesChanged changed => new LiveEventView("TilesChanged", MonsterId: changed.MonsterId, Tiles: changed.Tiles.Select(tile => new[] { tile.X, tile.Y }).ToList(), Tile: DungeonMap.SymbolOf(changed.Tile).ToString()),
        HeroPulled pulled => new LiveEventView("HeroPulled", pulled.HeroId, pulled.MonsterId),
        HeroBlownAway blown => new LiveEventView("HeroBlownAway", blown.HeroId, blown.MonsterId, blown.Damage, Element: Element.Wind),
        HeroShardHit shard => new LiveEventView("HeroShardHit", shard.HeroId, shard.MonsterId, shard.Damage),
        HeroScorched scorched => new LiveEventView("HeroScorched", scorched.HeroId, Amount: scorched.Damage, Element: Element.Fire),
        MonsterSummoned summoned => new LiveEventView("MonsterSummoned", MonsterId: summoned.MonsterId),
        MonsterWithered withered => new LiveEventView("MonsterWithered", MonsterId: withered.MonsterId),
        BossSlain slain => new LiveEventView("BossSlain", MonsterId: slain.MonsterId),
        FloorCleared cleared => new LiveEventView("FloorCleared", Amount: cleared.Floor),
        LiveVaultConquered => new LiveEventView("VaultConquered"),
        LiveRewardGranted granted => new LiveEventView("RewardGranted", granted.HeroId, ItemName: granted.ItemName, Missed: granted.Stashed),
        CrystalBroken broken => new LiveEventView("CrystalBroken", broken.HeroId, Tiles: [[broken.Tile.X, broken.Tile.Y]]),
        ObstacleBroken broken => new LiveEventView("ObstacleBroken", broken.HeroId, Tiles: [[broken.Tile.X, broken.Tile.Y]]),
        MonsterShardHit shard => new LiveEventView("MonsterShardHit", shard.HeroId, shard.MonsterId, shard.Damage),
        MonsterBurnedByLava lava => new LiveEventView("MonsterBurnedByLava", MonsterId: lava.MonsterId, Amount: lava.Damage, Element: Element.Fire),
        MonsterPricked pricked => new LiveEventView("MonsterPricked", pricked.HeroId, pricked.MonsterId, pricked.Damage),
        MonsterEnvenomed venom => new LiveEventView("MonsterEnvenomed", venom.HeroId, venom.MonsterId, venom.Damage, Levels: venom.Stacks),
        HeroPoisoned poisoned => new LiveEventView("HeroPoisoned", poisoned.HeroId),
        HeroPoisonDamaged poison => new LiveEventView("HeroPoisonDamaged", poison.HeroId, Amount: poison.Damage),
        HeroLifeDrained drained => new LiveEventView("HeroLifeDrained", drained.HeroId, Amount: drained.Amount),
        HeroManaSiphoned siphoned => new LiveEventView("HeroManaSiphoned", siphoned.HeroId, Amount: siphoned.Amount),
        LiveExoticTriggered exotic => new LiveEventView("ExoticTriggered", exotic.HeroId, exotic.MonsterId, exotic.Amount == 0 ? null : exotic.Amount, Ability: exotic.Power.ToString()),
        _ => throw new ArgumentOutOfRangeException(nameof(liveEvent), liveEvent.GetType().Name, "Unknown live event.")
    };

    private static double Round(double value) => Math.Round(value, 2);

    private static double Angle(Vector facing) => Math.Round(Math.Atan2(facing.X, facing.Y), 2);
}
