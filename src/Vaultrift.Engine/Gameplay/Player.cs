using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public sealed class Player : Creature
{
    public const int MaxNameLength = 16;
    public const string DefaultName = "Hero";

    private readonly Dictionary<EquipmentSlot, InventoryEntry> _equipment = [];
    private readonly Dictionary<string, ActiveBoost> _boosts = [];
    private readonly Dictionary<StatPoint, int> _spentPoints = [];
    private readonly Stats _baseStats;

    public Player(Position position, Stats stats, string? name = null)
        : base(position, stats)
    {
        Facing = Direction.Down;
        Name = NormalizeName(name, DefaultName);
        _baseStats = stats;
        Mana = stats.MaxMana;
    }

    public Player(Position position, CharacterClass characterClass, string? name = null)
        : this(position, characterClass?.BaseStats ?? throw new ArgumentNullException(nameof(characterClass)), name)
    {
        Class = characterClass;
        Name = NormalizeName(name, characterClass.Name);
    }

    public string Name { get; }

    public CharacterClass? Class { get; }

    public int Level { get; private set; } = 1;

    public int Experience { get; private set; }

    public int ExperienceToNextLevel => Progression.ExperienceToNextLevel(Level);

    public int UnspentPoints { get; private set; }

    public IReadOnlyDictionary<StatPoint, int> SpentPoints => _spentPoints;

    public int Mana { get; private set; }

    public int MaxMana => CombatStats.MaxMana;

    public Direction Facing { get; internal set; }

    public Inventory Inventory { get; } = new();

    public IReadOnlyDictionary<EquipmentSlot, InventoryEntry> Equipment => _equipment;

    public IReadOnlyCollection<ActiveBoost> Boosts => _boosts.Values;

    public PotionDefinition? QuickSlot { get; private set; }

    public int StepsPerTurn => Class?.Speed == Speed.Fast ? 2 : 1;

    public ClassAbility? Ability => Class?.Ability;

    public int AbilityCooldown { get; internal set; }

    public int BulwarkTurns { get; internal set; }

    public int FuryTurns { get; internal set; }

    public int InvisibleTurns { get; internal set; }

    public bool IsInvisible => InvisibleTurns > 0;

    public ItemInstance? Weapon => WeaponIn(EquipmentSlot.MainHand);

    public ItemInstance? OffHandWeapon => WeaponIn(EquipmentSlot.OffHand);

    public bool HasShield =>
        _equipment.TryGetValue(EquipmentSlot.OffHand, out var entry) &&
        entry.Item.Definition is GearDefinition { Kind: EquipmentKind.Shield };

    public Element? Resistance => EquippedIn(EquipmentSlot.Cloak)?.Element;

    public override Stats CombatStats => StatsFor(EquipmentSlot.MainHand);

    public Stats OffHandStats => StatsFor(EquipmentSlot.OffHand);

    public int EffectAmount(EffectKind kind) =>
        _equipment.Values
            .Select(entry => entry.Item.Definition)
            .OfType<EquipmentDefinition>()
            .SelectMany(item => item.Effects)
            .Where(effect => effect.Kind == kind)
            .Sum(effect => effect.Amount);

    public EquipResult Equip(int entryId, EquipmentSlot? hand = null)
    {
        if (_equipment.Values.Any(equipped => equipped.Id == entryId))
            return EquipResult.AlreadyEquipped;

        var entry = Inventory.Find(entryId);

        if (entry is null)
            return EquipResult.ItemNotFound;

        if (entry.Item.Definition is not EquipmentDefinition equipment)
            return EquipResult.NotEquippable;

        if (equipment is GearDefinition { Kind: EquipmentKind.ClassMod } classMod && classMod.ClassId != Class?.Id)
            return EquipResult.WrongClass;

        var slot = SlotFor(equipment, hand);
        var displaced = SlotsFreedBy(equipment, slot)
            .Where(_equipment.ContainsKey)
            .ToList();

        if (Inventory.Entries.Count - 1 + displaced.Count > Inventory.Capacity)
            return EquipResult.BackpackFull;

        Inventory.Take(entryId);

        foreach (var freed in displaced)
        {
            Inventory.TryPut(_equipment[freed]);
            _equipment.Remove(freed);
        }

        _equipment[slot] = entry;
        ClampMana();
        return EquipResult.Equipped;
    }

    public UnequipResult Unequip(EquipmentSlot slot)
    {
        if (!_equipment.TryGetValue(slot, out var entry))
            return UnequipResult.SlotEmpty;

        if (!Inventory.TryPut(entry))
            return UnequipResult.BackpackFull;

        _equipment.Remove(slot);
        ClampMana();
        return UnequipResult.Unequipped;
    }

    public SpendResult SpendPoint(StatPoint stat)
    {
        if (UnspentPoints == 0)
            return SpendResult.NoPointsLeft;

        if (IsAtCap(stat))
            return SpendResult.StatAtCap;

        _spentPoints[stat] = _spentPoints.GetValueOrDefault(stat) + 1;
        UnspentPoints--;
        Stats = Progression.Apply(_baseStats, _spentPoints);

        if (stat == StatPoint.Health)
            Heal(Progression.HealthPerPoint);

        if (stat == StatPoint.Mana)
            RestoreMana(Progression.ManaPerPoint);

        return SpendResult.Spent;
    }

    internal int GainExperience(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var levelsGained = 0;
        Experience += amount;

        while (Experience >= ExperienceToNextLevel)
        {
            Experience -= ExperienceToNextLevel;
            Level++;
            UnspentPoints += Progression.PointsPerLevel;
            levelsGained++;
        }

        if (levelsGained > 0)
        {
            RestoreFullHealth();
            RestoreFullMana();
        }

        return levelsGained;
    }

    public UseResult Use(int entryId)
    {
        var entry = Inventory.Find(entryId);

        if (entry is null)
            return UseResult.ItemNotFound;

        if (entry.Item.Definition is not PotionDefinition potion)
            return UseResult.NotUsable;

        if (potion.Healing > 0 && Health == Stats.MaxHealth)
            return UseResult.AlreadyAtFullHealth;

        if (potion.ManaRestore > 0 && Mana == MaxMana)
            return UseResult.AlreadyAtFullMana;

        if (potion.Healing > 0)
            Heal(potion.Healing);

        if (potion.ManaRestore > 0)
            RestoreMana(potion.ManaRestore);

        if (potion.Boost is { } boost)
            _boosts[potion.Id] = new ActiveBoost(potion, boost, potion.BoostTurns);

        Inventory.RemoveOne(entry.Id);
        return UseResult.Used;
    }

    public UseResult UseQuickSlot()
    {
        if (QuickSlot is null)
            return UseResult.QuickSlotEmpty;

        var entry = Inventory.Entries.FirstOrDefault(candidate => candidate.Item.Id == QuickSlot.Id);

        return entry is null ? UseResult.ItemNotFound : Use(entry.Id);
    }

    public bool AssignQuickSlot(int entryId)
    {
        if (Inventory.Find(entryId)?.Item.Definition is not PotionDefinition potion)
            return false;

        QuickSlot = potion;
        return true;
    }

    internal void RestoreMana(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Mana = Math.Min(MaxMana, Mana + amount);
    }

    internal void RestoreFullMana() => Mana = MaxMana;

    internal IReadOnlyList<ActiveBoost> AdvanceBoosts()
    {
        var expired = new List<ActiveBoost>();

        foreach (var boost in _boosts.Values.ToList())
        {
            boost.RemainingTurns--;

            if (boost.RemainingTurns > 0)
                continue;

            _boosts.Remove(boost.Source.Id);
            expired.Add(boost);
        }

        return expired;
    }

    internal void ClearBoosts() => _boosts.Clear();

    internal void ClearAbilityStatuses()
    {
        BulwarkTurns = 0;
        FuryTurns = 0;
        InvisibleTurns = 0;
    }

    private Stats StatsFor(EquipmentSlot hand)
    {
        var stats = Stats.Apply(EquippedModifiers(hand).Concat(BoostModifiers()).Concat(MasteryModifiers()));

        return FuryTurns > 0
            ? stats.WithAttack((int)Math.Round(stats.Attack * AbilityRules.FuryAttackMultiplier, MidpointRounding.AwayFromZero))
            : stats;
    }

    private void ClampMana() => Mana = Math.Min(Mana, MaxMana);

    private static string NormalizeName(string? name, string fallback)
    {
        var trimmed = name?.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return fallback;

        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"A name cannot be longer than {MaxNameLength} characters.", nameof(name));

        return trimmed;
    }

    private bool IsAtCap(StatPoint stat) => stat switch
    {
        StatPoint.Evasion => Stats.Evasion + Progression.EvasionPerPoint > DamageCalculator.MaxEvasion,
        StatPoint.Critical => Stats.Critical + Progression.CriticalPerPoint > DamageCalculator.MaxCritical,
        _ => false
    };

    private ItemInstance? EquippedIn(EquipmentSlot slot) =>
        _equipment.TryGetValue(slot, out var entry) ? entry.Item : null;

    private ItemInstance? WeaponIn(EquipmentSlot hand) =>
        EquippedIn(hand) is { Definition: WeaponDefinition } weapon ? weapon : null;

    private bool HoldsTwoHanded =>
        Weapon?.Definition is WeaponDefinition { IsTwoHanded: true };

    private EquipmentSlot SlotFor(EquipmentDefinition equipment, EquipmentSlot? hand) => equipment switch
    {
        WeaponDefinition { IsTwoHanded: true } => EquipmentSlot.MainHand,
        WeaponDefinition => HandForOneHanded(hand),
        GearDefinition gear => gear.Kind switch
        {
            EquipmentKind.Shield => EquipmentSlot.OffHand,
            EquipmentKind.Head => EquipmentSlot.Head,
            EquipmentKind.Body => EquipmentSlot.Body,
            EquipmentKind.Feet => EquipmentSlot.Feet,
            EquipmentKind.Ring => FreeRingSlot(),
            EquipmentKind.Cloak => EquipmentSlot.Cloak,
            EquipmentKind.ClassMod => EquipmentSlot.ClassMod,
            _ => throw new ArgumentOutOfRangeException(nameof(equipment), gear.Kind, "Unknown equipment kind.")
        },
        _ => throw new ArgumentOutOfRangeException(nameof(equipment), equipment.GetType().Name, "Unknown equipment.")
    };

    private EquipmentSlot HandForOneHanded(EquipmentSlot? hand)
    {
        if (hand is EquipmentSlot.MainHand or EquipmentSlot.OffHand)
            return hand.Value;

        if (!_equipment.ContainsKey(EquipmentSlot.MainHand))
            return EquipmentSlot.MainHand;

        if (!HoldsTwoHanded && !_equipment.ContainsKey(EquipmentSlot.OffHand))
            return EquipmentSlot.OffHand;

        return EquipmentSlot.MainHand;
    }

    private IEnumerable<EquipmentSlot> SlotsFreedBy(EquipmentDefinition equipment, EquipmentSlot slot)
    {
        yield return slot;

        if (equipment is WeaponDefinition { IsTwoHanded: true })
            yield return EquipmentSlot.OffHand;
        else if (slot == EquipmentSlot.OffHand && HoldsTwoHanded)
            yield return EquipmentSlot.MainHand;
    }

    private EquipmentSlot FreeRingSlot()
    {
        if (!_equipment.ContainsKey(EquipmentSlot.Ring1))
            return EquipmentSlot.Ring1;

        if (!_equipment.ContainsKey(EquipmentSlot.Ring2))
            return EquipmentSlot.Ring2;

        return EquipmentSlot.Ring1;
    }

    private IEnumerable<StatModifier> EquippedModifiers(EquipmentSlot hand) =>
        _equipment
            .Where(pair => !IsOtherHandWeapon(pair.Key, pair.Value, hand))
            .Select(pair => pair.Value.Item.Definition)
            .OfType<EquipmentDefinition>()
            .SelectMany(item => item.Modifiers);

    private static bool IsOtherHandWeapon(EquipmentSlot slot, InventoryEntry entry, EquipmentSlot hand) =>
        slot is EquipmentSlot.MainHand or EquipmentSlot.OffHand &&
        slot != hand &&
        entry.Item.Definition is WeaponDefinition;

    private IEnumerable<StatModifier> BoostModifiers() =>
        _boosts.Values.Select(boost => boost.Modifier);

    private IEnumerable<StatModifier> MasteryModifiers()
    {
        if (Class is null)
            yield break;

        var mastered = new[] { Weapon, OffHandWeapon }
            .Any(weapon => weapon?.Definition is WeaponDefinition definition && definition.WeaponType == Class.Mastery.WeaponType);

        if (mastered)
            yield return Class.Mastery.Bonus;
    }
}
