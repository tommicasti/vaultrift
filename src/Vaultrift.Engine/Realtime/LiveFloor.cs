using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Randomness;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public sealed partial class LiveFloor
{
    private readonly List<LiveHero> _heroes = [];
    private readonly List<LiveMonster> _monsters = [];
    private readonly Dictionary<int, FlowField> _fields = [];
    private readonly List<LiveItem> _items = [];
    private readonly DamageCalculator _damage;
    private readonly IRandom _random;
    private readonly ILootTable? _loot;
    private int _nextItemId = 1;

    public LiveFloor(DungeonMap map, Position start, IRandom random, string? biome = null, ILootTable? loot = null, int floorNumber = 1, long startTick = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startTick);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfLessThan(floorNumber, 1);

        Map = map;
        Start = new Vector(start.X, start.Y);
        Biome = biome;
        FloorNumber = floorNumber;
        _damage = new DamageCalculator(random);
        _random = random;
        _loot = loot;
        Tick = startTick;
    }

    public int FloorNumber { get; }

    public IReadOnlyList<LiveItem> Items => _items;

    public int ItemsVersion { get; private set; }

    public int Seconds => (int)(Tick / LiveRules.TicksPerSecond);

    public DungeonMap Map { get; }

    public Vector Start { get; }

    public string? Biome { get; }

    public long Tick { get; private set; }

    public double Time => Tick * LiveRules.TickSeconds;

    public IReadOnlyList<LiveHero> Heroes => _heroes;

    public IReadOnlyList<LiveMonster> Monsters => _monsters;

    public LiveHero AddHero(int id, string name, string classId, Stats stats, Speed speed = Speed.Normal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);
        ArgumentNullException.ThrowIfNull(stats);

        return Join(id, new Player(Collision.TileOf(Start), stats, name), classId, speed, Guid.Empty);
    }

    public LiveHero AddHero(int id, Player player, Guid owner = default)
    {
        ArgumentNullException.ThrowIfNull(player);

        return Join(id, player, player.Class?.Id ?? "hero", player.Class?.Speed ?? Speed.Normal, owner);
    }

    private LiveHero Join(int id, Player player, string classId, Speed speed, Guid owner)
    {
        if (_heroes.Any(hero => hero.Id == id))
            throw new ArgumentException($"A hero with id {id} is already on the floor.", nameof(id));

        if (_heroes.Count >= LiveRules.MaxHeroes)
            throw new InvalidOperationException($"A floor holds at most {LiveRules.MaxHeroes} heroes.");

        var hero = new LiveHero(id, player, classId, speed, Start, owner);
        _heroes.Add(hero);
        ArriveQuietly(hero);
        Rescale();
        return hero;
    }

    public bool RemoveHero(int id)
    {
        if (_heroes.FirstOrDefault(hero => hero.Id == id) is not { } hero)
            return false;

        _heroes.Remove(hero);
        _fields.Remove(id);

        if (hero.IsDead)
        {
            hero.Player.RestoreFullHealth();
            hero.Player.RestoreFullMana();
        }

        Rescale();
        return true;
    }

    public double PartyScale { get; private set; } = 1;

    private void Rescale()
    {
        var extra = Math.Max(0, _heroes.Count - 1);
        PartyScale = 1 + LiveRules.ExtraHeroHealth * extra;
        BossScale = 1 + LiveRules.ExtraHeroBossHealth * extra;

        foreach (var monster in _monsters)
            monster.Rescale(ScaleFor(monster));
    }

    public double BossScale { get; private set; } = 1;

    public static int ReinforcementTier(int heroes) => Math.Max(0, heroes - 1) / LiveRules.HeroesPerReinforcement;

    private double ScaleFor(LiveMonster monster) => monster.Kind.IsBoss ? BossScale : PartyScale;

    public LiveMonster AddMonster(Enemy enemy)
    {
        ArgumentNullException.ThrowIfNull(enemy);

        var monster = new LiveMonster(enemy);
        monster.Rescale(ScaleFor(monster));
        _monsters.Add(monster);
        return monster;
    }

    public void SetInput(int heroId, HeroInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (_heroes.FirstOrDefault(hero => hero.Id == heroId) is { IsAway: false } hero)
            hero.Input = input;
    }

    public void SetAway(int heroId, bool away)
    {
        if (_heroes.FirstOrDefault(hero => hero.Id == heroId) is not { } hero || hero.IsAway == away)
            return;

        hero.IsAway = away;
        hero.Input = HeroInput.Idle;
        hero.IsMoving = false;
        hero.Touch();
    }

    public IReadOnlyList<LiveEvent> Step()
    {
        Tick++;
        var events = new List<LiveEvent>(_pending);
        _pending.Clear();

        foreach (var hero in _heroes)
            UpdateHero(hero, events);

        DriftOnCurrents();
        MoveProjectiles(events);

        if (Tick % LiveRules.TicksPerSecond == 0)
            Upkeep(events);

        RefreshFields();

        foreach (var monster in _monsters.Where(monster => !monster.IsDead))
            UpdateMonster(monster, events);

        SeparateMonsters();
        WatchStairs(events);
        WatchFixtures(events);
        WatchBosses(events);
        return events;
    }

    private void UpdateHero(LiveHero hero, List<LiveEvent> events)
    {
        if (hero.IsDead)
        {
            if (Time >= hero.RespawnAt)
                Respawn(hero, events);

            return;
        }

        var input = hero.Input;
        var from = hero.Position;

        if (!input.Aim.IsZero)
            hero.Facing = input.Aim;

        var sprinting = input.Sprint && !input.Move.IsZero;

        if (!input.Move.IsZero)
        {
            if (input.Aim.IsZero)
                hero.Facing = input.Move.Normalized();

            var speed = LiveRules.HeroSpeed(hero.Speed) * (sprinting ? LiveRules.SprintFactor : 1);
            var step = input.Move * (speed * LiveRules.TickSeconds);
            hero.Position = Collision.Slide(Map, from, step, LiveRules.HeroRadius, point => BlockedByMonster(from, point) || BlockedByFixture(from, point), roundsCorners: true);
        }

        hero.IsMoving = hero.Position != from;
        hero.IsSprinting = sprinting && hero.IsMoving;
        PickUp(hero, events);

        if (input.Attack && !sprinting && Time >= hero.AttackReadyAt)
            Attack(hero, events);
    }

    private void PickUp(LiveHero hero, List<LiveEvent> events)
    {
        var reachable = _items.Where(item => hero.Position.DistanceTo(new Vector(item.Tile.X, item.Tile.Y)) <= LiveRules.PickUpReach).ToList();

        foreach (var item in reachable)
        {
            if (!hero.Player.Inventory.TryAdd(item.Item))
            {
                if (Time >= hero.FullNoticeAt)
                {
                    hero.FullNoticeAt = Time + LiveRules.FullNoticeSeconds;
                    events.Add(new BackpackFull(hero.Id));
                }

                return;
            }

            _items.Remove(item);
            ItemsVersion++;
            hero.Touch();
            events.Add(new LootPickedUp(hero.Id, item.Id, item.Item));
        }
    }

    private void Upkeep(List<LiveEvent> events)
    {
        foreach (var hero in _heroes.Where(hero => !hero.IsDead))
        {
            var player = hero.Player;

            foreach (var boost in player.AdvanceBoosts())
            {
                events.Add(new BoostFaded(hero.Id, boost.Source));
                hero.Touch();
            }

            if (player.AbilityCooldown > 0)
            {
                player.AbilityCooldown--;
                hero.Touch();
            }

            TickAbility(hero, player.BulwarkTurns, turns => player.BulwarkTurns = turns, ClassAbility.Bulwark, events);
            TickAbility(hero, player.FuryTurns, turns => player.FuryTurns = turns, ClassAbility.Fury, events);
            TickAbility(hero, player.InvisibleTurns, turns => player.InvisibleTurns = turns, ClassAbility.Invisibility, events);

            if (Seconds % Game.HealthRegenInterval == 0)
                player.Heal(Game.HealthRegenAmount);

            var regeneration = player.EffectAmount(EffectKind.Regeneration) + (player.FuryTurns > 0 ? player.EffectAmount(EffectKind.FuryRegeneration) : 0);

            if (regeneration > 0)
                player.Heal(regeneration);

            if (Seconds % Progression.ManaRegenInterval == 0)
                player.RestoreMana(Progression.ManaRegenAmount + player.EffectAmount(EffectKind.ManaFlow));
        }

        BurnMonsters(events);
        SpreadVenom(events);
        BurnMonstersOnLava(events);
        BreatheSpores(events);
        BurnHeroesOnLava(events);
        CoolLava(events);
        UseBossPowers(events);
    }

    private static void TickAbility(LiveHero hero, int turns, Action<int> set, ClassAbility ability, List<LiveEvent> events)
    {
        if (turns <= 0)
            return;

        set(turns - 1);
        hero.Touch();

        if (turns == 1)
            events.Add(new AbilityFaded(hero.Id, ability));
    }

    private bool BlockedByMonster(Vector from, Vector point) =>
        _monsters.Any(monster =>
        {
            if (monster.IsDead)
                return false;

            var reach = monster.Radius + LiveRules.HeroRadius;
            var distance = point.DistanceTo(monster.Position);
            return distance < reach && distance < from.DistanceTo(monster.Position);
        });

    private void Reward(LiveHero slayer, LiveMonster monster, List<LiveEvent> events)
    {
        var player = slayer.Player;
        var coins = Progression.CoinsFor(monster.Source);
        var experience = Progression.ExperienceFor(monster.Source, player.EffectAmount(EffectKind.Wisdom), player.Level);
        var levelBefore = player.Level;

        player.GainCoins(coins);
        player.GainExperience(experience);
        slayer.Touch();
        events.Add(new HeroRewarded(slayer.Id, monster.Id, coins, experience, player.Level - levelBefore));

        if (player.FuryTurns > 0)
            player.FuryTurns += player.EffectAmount(EffectKind.FuryExtension);

        if (Math.Min(player.EffectAmount(EffectKind.ManaSiphon), player.MaxMana - player.Mana) is > 0 and var siphoned)
        {
            player.RestoreMana(siphoned);
            events.Add(new HeroManaSiphoned(slayer.Id, siphoned));
        }

        if (_loot is null)
            return;

        foreach (var hero in _heroes)
        {
            var context = new LootContext(hero.ClassId, hero.Player.EffectAmount(EffectKind.Luck), FloorNumber);

            if (_loot.RollDrop(monster.Source, context) is not { } item || DropSpot(Collision.TileOf(monster.Position)) is not { } spot)
                continue;

            var dropped = new LiveItem(_nextItemId++, spot, item);
            _items.Add(dropped);
            ItemsVersion++;
            events.Add(new LootDropped(dropped.Id, monster.Id));
        }
    }

    private Position? DropSpot(Position origin)
    {
        Position[] candidates =
        [
            origin,
            new(origin.X + 1, origin.Y), new(origin.X - 1, origin.Y), new(origin.X, origin.Y + 1), new(origin.X, origin.Y - 1),
            new(origin.X + 1, origin.Y + 1), new(origin.X - 1, origin.Y - 1), new(origin.X + 1, origin.Y - 1), new(origin.X - 1, origin.Y + 1)
        ];

        foreach (var candidate in candidates)
        {
            if (Map.IsWalkable(candidate) && _items.All(item => item.Tile != candidate))
                return candidate;
        }

        return null;
    }

    private void Respawn(LiveHero hero, List<LiveEvent> events)
    {
        hero.Player.RestoreFullHealth();
        hero.Player.RestoreFullMana();
        hero.Position = Start;
        hero.Facing = new Vector(0, 1);
        hero.Input = HeroInput.Idle;
        hero.AttackReadyAt = Time;
        ArriveQuietly(hero);
        events.Add(new HeroRespawned(hero.Id));
    }

    private void RefreshFields()
    {
        foreach (var hero in _heroes)
        {
            var tile = Collision.TileOf(hero.Position);

            if (!_fields.TryGetValue(hero.Id, out var field) || field.Target != tile)
                _fields[hero.Id] = FlowField.Toward(Map, tile);
        }
    }

    private void UpdateMonster(LiveMonster monster, List<LiveEvent> events)
    {
        monster.IsMoving = false;

        if (monster.Kind.IsStationary)
            return;

        if (Time < monster.FrozenUntil || Time < monster.StaggeredUntil)
        {
            monster.WindupEndsAt = null;
            return;
        }

        var target = _heroes
            .Where(hero => hero.IsTargetable)
            .OrderBy(hero => hero.Position.DistanceTo(monster.Position))
            .FirstOrDefault();

        if (!monster.IsAwake)
        {
            if (target is not null && Notices(monster, target))
            {
                monster.IsAwake = true;
                monster.AttackReadyAt = Time + LiveRules.MonsterWindupSeconds;
                events.Add(new MonsterWoke(monster.Id));
            }

            return;
        }

        if (target is null)
        {
            ReturnHome(monster);
            return;
        }

        monster.TargetId = target.Id;
        var offset = target.Position - monster.Position;
        var distance = offset.Length;
        var reach = monster.Radius + LiveRules.HeroRadius + LiveRules.MonsterReach;

        if (distance > 1e-6)
            monster.Facing = offset * (1 / distance);

        if (monster.WindupEndsAt is { } windupEnds)
        {
            if (Time >= windupEnds)
                Strike(monster, target, distance <= reach + LiveRules.MonsterReachGrace, events);

            return;
        }

        if (distance <= reach)
        {
            monster.Heading = Vector.Zero;

            if (Time >= monster.AttackReadyAt)
            {
                monster.WindupEndsAt = Time + LiveRules.MonsterWindupSeconds;
                events.Add(new MonsterWindup(monster.Id, target.Id));
                GuardTheWall(monster, target, events);
            }

            return;
        }

        var heading = Steady(monster, HeadingToward(monster, target));
        var travel = Math.Min(LiveRules.MonsterSpeed(monster.Speed) * LiveRules.TickSeconds, distance - reach + 0.01);
        if (distance > reach + LiveRules.FaceTheWayDistance)
            monster.Facing = heading;

        MoveMonster(monster, heading * travel);
    }

    private bool Notices(LiveMonster monster, LiveHero hero)
    {
        if (!hero.IsTargetable)
            return false;

        var wake = Math.Max(1, LiveRules.WakeDistance - hero.Player.EffectAmount(EffectKind.Stealth)) + (hero.IsSprinting ? LiveRules.SprintNoise : 0);

        if (monster.Position.DistanceTo(hero.Position) > wake)
            return false;

        if (monster.Kind.PassesThroughWalls)
            return true;

        var path = _fields[hero.Id].DistanceFrom(Collision.TileOf(monster.Position));
        return path <= wake + LiveRules.WakePathSlack;
    }

    private static Vector Steady(LiveMonster monster, Vector desired)
    {
        var previous = monster.Heading;
        var blended = previous.IsZero ? desired : previous * LiveRules.HeadingInertia + desired * (1 - LiveRules.HeadingInertia);
        var heading = blended.Length < 0.2 ? desired : blended.Normalized();
        monster.Heading = heading;
        return heading;
    }

    private Vector HeadingToward(LiveMonster monster, LiveHero hero)
    {
        var direct = (hero.Position - monster.Position).Normalized();

        if (monster.Kind.PassesThroughWalls || Collision.ClearPath(Map, monster.Position, hero.Position, monster.Radius))
            return direct;

        var field = _fields[hero.Id];
        var tile = Collision.TileOf(monster.Position);

        if (field.DistanceFrom(tile) <= 1 || field.NextStep(tile) is not { } next)
            return direct;

        return (new Vector(next.X, next.Y) - monster.Position).Normalized();
    }

    private void Strike(LiveMonster monster, LiveHero hero, bool inReach, List<LiveEvent> events)
    {
        monster.WindupEndsAt = null;
        monster.AttackReadyAt = Time + LiveRules.MonsterAttackSeconds(monster.Speed);

        if (!inReach || hero.IsDead)
            return;

        var result = Bulwark(hero, _damage.Calculate(monster.Stats, hero.Stats));

        if (!result.Missed)
            hero.TakeDamage(result.Damage);

        events.Add(new HeroHit(monster.Id, hero.Id, result, hero.Health));
        SwearRevenge(hero, result);

        if (hero.IsDead)
        {
            Fall(hero, events);
            return;
        }

        var player = hero.Player;

        if (result.IsBlocked && player.EffectAmount(EffectKind.BlockReflect) > 0)
            Prick(hero, monster, DamageCalculator.BaseDamage(monster.Stats, hero.Stats), events);

        if (!result.IsDodged && player.EffectAmount(EffectKind.Thorns) is > 0 and var thorns)
            Prick(hero, monster, thorns, events);
    }

    private void Prick(LiveHero hero, LiveMonster monster, int damage, List<LiveEvent> events)
    {
        if (monster.IsDead || damage <= 0)
            return;

        monster.TakeDamage(damage);
        events.Add(new MonsterPricked(hero.Id, monster.Id, damage, monster.Health));

        if (!monster.IsDead)
            return;

        events.Add(new MonsterSlain(hero.Id, monster.Id));
        Reward(hero, monster, events);
    }

    private void Hurt(LiveHero hero, int damage, List<LiveEvent> events)
    {
        if (hero.IsDead || hero.IsAway || damage <= 0)
            return;

        hero.TakeDamage(damage);
        hero.Touch();

        if (hero.IsDead)
            Fall(hero, events);
    }

    private void Fall(LiveHero hero, List<LiveEvent> events)
    {
        hero.RespawnAt = Time + LiveRules.HeroRespawnSeconds;
        hero.IsMoving = false;
        hero.Deaths++;
        hero.Touch();

        var lost = 0;

        if (Falls == FallRule.LoseCoins)
        {
            lost = hero.Player.Coins * Progression.CoinLossPercent / 100;
            if (lost > 0)
                hero.Player.TrySpendCoins(lost);
        }

        events.Add(new HeroFell(hero.Id, lost));

        if (Falls == FallRule.DigGrave)
            DigGrave(hero, events);
    }

    private static AttackResult Bulwark(LiveHero hero, AttackResult result)
    {
        if (hero.Player.BulwarkTurns == 0 || result.Missed)
            return result;

        return hero.Player.HasShield
            ? AttackResult.Blocked
            : result with { Damage = Math.Max(DamageCalculator.MinimumDamage, result.Damage / 2) };
    }

    private void ReturnHome(LiveMonster monster)
    {
        monster.WindupEndsAt = null;
        monster.TargetId = null;
        var offset = monster.Home - monster.Position;

        if (offset.Length < 0.1)
        {
            monster.Reset();
            return;
        }

        var heading = offset.Normalized();
        monster.Facing = heading;
        MoveMonster(monster, heading * Math.Min(LiveRules.MonsterSpeed(monster.Speed) * LiveRules.TickSeconds, offset.Length));
    }

    private void MoveMonster(LiveMonster monster, Vector step)
    {
        var from = monster.Position;
        var next = Collision.Slide(Map, from, step, monster.Radius, passesThroughWalls: monster.Kind.PassesThroughWalls);

        if (StepsIntoLava(monster, next))
            return;

        monster.Position = next;
        monster.IsMoving |= monster.Position != from;
    }

    private void SeparateMonsters()
    {
        var awake = _monsters.Where(monster => !monster.IsDead && monster.IsAwake).ToList();

        for (var first = 0; first < awake.Count; first++)
        {
            for (var second = first + 1; second < awake.Count; second++)
            {
                var a = awake[first];
                var b = awake[second];
                var offset = b.Position - a.Position;
                var distance = offset.Length;
                var overlap = a.Radius + b.Radius - distance;

                if (overlap <= 0)
                    continue;

                var share = overlap * LiveRules.SeparationShare;
                var push = distance < 1e-6 ? new Vector(share, 0) : offset * (share / distance);
                b.Position = Collision.Slide(Map, b.Position, push, b.Radius, passesThroughWalls: b.Kind.PassesThroughWalls);
                a.Position = Collision.Slide(Map, a.Position, push * -1, a.Radius, passesThroughWalls: a.Kind.PassesThroughWalls);
                a.IsMoving |= share > LiveRules.ShuffleThreshold;
                b.IsMoving |= share > LiveRules.ShuffleThreshold;
            }
        }
    }
}
