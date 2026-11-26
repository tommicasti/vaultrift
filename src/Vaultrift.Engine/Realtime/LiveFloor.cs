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

    public LiveFloor(DungeonMap map, Position start, IRandom random, string? biome = null, ILootTable? loot = null, int floorNumber = 1)
    {
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

        return Join(id, new Player(Collision.TileOf(Start), stats, name), classId, speed);
    }

    public LiveHero AddHero(int id, Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return Join(id, player, player.Class?.Id ?? "hero", player.Class?.Speed ?? Speed.Normal);
    }

    private LiveHero Join(int id, Player player, string classId, Speed speed)
    {
        if (_heroes.Any(hero => hero.Id == id))
            throw new ArgumentException($"A hero with id {id} is already on the floor.", nameof(id));

        if (_heroes.Count >= LiveRules.MaxHeroes)
            throw new InvalidOperationException($"A floor holds at most {LiveRules.MaxHeroes} heroes.");

        var hero = new LiveHero(id, player, classId, speed, Start);
        _heroes.Add(hero);
        Rescale();
        return hero;
    }

    public bool RemoveHero(int id)
    {
        var removed = _heroes.RemoveAll(hero => hero.Id == id) > 0;
        _fields.Remove(id);

        if (removed)
            Rescale();

        return removed;
    }

    public double PartyScale { get; private set; } = 1;

    private void Rescale()
    {
        var scale = 1 + LiveRules.ExtraHeroHealth * Math.Max(0, _heroes.Count - 1);

        foreach (var monster in _monsters)
            monster.Rescale(scale);

        PartyScale = scale;
    }

    public LiveMonster AddMonster(Enemy enemy)
    {
        ArgumentNullException.ThrowIfNull(enemy);

        var monster = new LiveMonster(enemy);
        monster.Rescale(PartyScale);
        _monsters.Add(monster);
        return monster;
    }

    public void SetInput(int heroId, HeroInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (_heroes.FirstOrDefault(hero => hero.Id == heroId) is { } hero)
            hero.Input = input;
    }

    public IReadOnlyList<LiveEvent> Step()
    {
        Tick++;
        var events = new List<LiveEvent>(_pending);
        _pending.Clear();

        foreach (var hero in _heroes)
            UpdateHero(hero, events);

        MoveProjectiles(events);

        if (Tick % LiveRules.TicksPerSecond == 0)
            Upkeep(events);

        RefreshFields();

        foreach (var monster in _monsters.Where(monster => !monster.IsDead))
            UpdateMonster(monster, events);

        SeparateMonsters();
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

        if (!input.Move.IsZero)
        {
            if (input.Aim.IsZero)
                hero.Facing = input.Move.Normalized();

            var step = input.Move * (LiveRules.HeroSpeed(hero.Speed) * LiveRules.TickSeconds);
            hero.Position = Collision.Slide(Map, from, step, LiveRules.HeroRadius, point => BlockedByMonster(from, point));
        }

        hero.IsMoving = hero.Position != from;
        PickUp(hero, events);

        if (input.Attack && Time >= hero.AttackReadyAt)
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

            if (player.EffectAmount(EffectKind.Regeneration) is > 0 and var regeneration)
                player.Heal(regeneration);

            if (Seconds % Progression.ManaRegenInterval == 0)
                player.RestoreMana(Progression.ManaRegenAmount + player.EffectAmount(EffectKind.ManaFlow));
        }

        BurnMonsters(events);
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

    private LiveMonster? AutoAim(LiveHero hero) =>
        _monsters
            .Where(monster => !monster.IsDead && monster.Position.DistanceTo(hero.Position) <= LiveRules.HeroReach + monster.Radius)
            .OrderBy(monster => monster.Position.DistanceTo(hero.Position))
            .FirstOrDefault();

    private void Respawn(LiveHero hero, List<LiveEvent> events)
    {
        hero.Player.RestoreFullHealth();
        hero.Player.RestoreFullMana();
        hero.Position = Start;
        hero.Facing = new Vector(0, 1);
        hero.Input = HeroInput.Idle;
        hero.AttackReadyAt = Time;
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

        if (Time < monster.FrozenUntil)
        {
            monster.WindupEndsAt = null;
            return;
        }

        var target = _heroes
            .Where(hero => !hero.IsDead && !hero.Player.IsInvisible)
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
            if (Time >= monster.AttackReadyAt)
            {
                monster.WindupEndsAt = Time + LiveRules.MonsterWindupSeconds;
                events.Add(new MonsterWindup(monster.Id, target.Id));
            }

            return;
        }

        var heading = HeadingToward(monster, target);
        var travel = Math.Min(LiveRules.MonsterSpeed(monster.Kind.Speed) * LiveRules.TickSeconds, distance - reach + 0.01);
        MoveMonster(monster, heading * travel);
    }

    private bool Notices(LiveMonster monster, LiveHero hero)
    {
        if (hero.Player.IsInvisible)
            return false;

        if (monster.Position.DistanceTo(hero.Position) > LiveRules.WakeDistance)
            return false;

        if (monster.Kind.PassesThroughWalls)
            return true;

        var path = _fields[hero.Id].DistanceFrom(Collision.TileOf(monster.Position));
        return path <= LiveRules.WakeDistance + LiveRules.WakePathSlack;
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
        monster.AttackReadyAt = Time + LiveRules.MonsterAttackSeconds(monster.Kind.Speed);

        if (!inReach || hero.IsDead)
            return;

        var result = Bulwark(hero, _damage.Calculate(monster.Stats, hero.Stats));

        if (!result.Missed)
            hero.TakeDamage(result.Damage);

        events.Add(new HeroHit(monster.Id, hero.Id, result, hero.Health));

        if (!hero.IsDead)
            return;

        hero.RespawnAt = Time + LiveRules.HeroRespawnSeconds;
        hero.IsMoving = false;
        hero.Deaths++;
        hero.Touch();
        events.Add(new HeroFell(hero.Id));
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
        MoveMonster(monster, heading * Math.Min(LiveRules.MonsterSpeed(monster.Kind.Speed) * LiveRules.TickSeconds, offset.Length));
    }

    private void MoveMonster(LiveMonster monster, Vector step)
    {
        var from = monster.Position;
        monster.Position = Collision.Slide(Map, from, step, monster.Radius, passesThroughWalls: monster.Kind.PassesThroughWalls);
        monster.IsMoving = monster.Position != from;
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
            }
        }
    }
}
