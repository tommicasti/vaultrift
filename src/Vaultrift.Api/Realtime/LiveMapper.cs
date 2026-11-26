using Vaultrift.Api.Mapping;
using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Realtime;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Api.Realtime;

public static class LiveMapper
{
    public static LiveWelcome Welcome(GameSession session, LiveFloor floor, LiveHero hero, string roomCode, bool isHost, IReadOnlyList<LivePartyMember> party)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(hero);

        var state = GameMapper.ToResponse(session);

        return new LiveWelcome(
            hero.Id,
            hero.ClassId,
            hero.Name,
            floor.Biome,
            state.Map,
            state.Player.Equipment,
            LiveRules.TicksPerSecond,
            LiveRules.MonsterWindupSeconds,
            LiveRules.HeroRadius + LiveRules.MonsterReach,
            HeroState(session, floor, hero),
            Items(floor),
            roomCode,
            isHost,
            LiveRules.MaxHeroes,
            party);
    }

    public static LiveHeroState HeroState(GameSession session, LiveFloor floor, LiveHero hero)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(floor);
        ArgumentNullException.ThrowIfNull(hero);

        var state = GameMapper.ToResponse(session);

        return new LiveHeroState(state.Player, state.Inventory, state.InventoryCapacity, floor.FloorNumber, state.FloorCount, state.Difficulty, hero.Deaths);
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
            hero.Player.FuryTurns > 0);
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
        HeroHit hit => new LiveEventView("HeroHit", hit.HeroId, hit.MonsterId, hit.Result.Damage, hit.Result.IsCritical, hit.Result.Missed),
        HeroFell fell => new LiveEventView("HeroFell", HeroId: fell.HeroId),
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
        _ => throw new ArgumentOutOfRangeException(nameof(liveEvent), liveEvent.GetType().Name, "Unknown live event.")
    };

    private static double Round(double value) => Math.Round(value, 3);

    private static double Angle(Vector facing) => Math.Round(Math.Atan2(facing.X, facing.Y), 3);
}
