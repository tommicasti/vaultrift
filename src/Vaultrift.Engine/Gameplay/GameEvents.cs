using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public abstract record GameEvent;

public sealed record PlayerMoved(Position To) : GameEvent;

public sealed record PlayerTurned(Direction Facing) : GameEvent;

public sealed record PlayerAttackedEnemy(int EnemyId, AttackResult Result) : GameEvent;

public sealed record PlayerAttackedNothing : GameEvent;

public sealed record EnemyDefeated(int EnemyId) : GameEvent;

public sealed record ExperienceGained(int Amount) : GameEvent;

public sealed record LeveledUp(int Level) : GameEvent;

public sealed record StatPointSpent(StatPoint Stat) : GameEvent;

public sealed record SpendRejected(SpendResult Reason) : GameEvent;

public sealed record EnemyAttackedPlayer(int EnemyId, AttackResult Result) : GameEvent;

public sealed record EnemyWokeUp(int EnemyId) : GameEvent;

public sealed record EnemyRespawned(int EnemyId) : GameEvent;

public sealed record EnemyMoved(int EnemyId, Position To) : GameEvent;

public sealed record PlayerDefeated : GameEvent;

public sealed record PlayerRespawned(Position At) : GameEvent;

public sealed record ItemPickedUp(ItemInstance Item) : GameEvent;

public sealed record InventoryFull(ItemInstance Item) : GameEvent;

public sealed record ItemDropped(int EnemyId, ItemInstance Item, Position At) : GameEvent;

public sealed record ItemDiscarded(ItemInstance Item) : GameEvent;

public sealed record ChestOpened(Position At, ItemInstance? Item) : GameEvent;

public sealed record MimicRevealed(int EnemyId, Position At) : GameEvent;

public sealed record ItemEquipped(ItemInstance Item) : GameEvent;

public sealed record EquipRejected(EquipResult Reason) : GameEvent;

public sealed record ItemUnequipped(ItemInstance Item) : GameEvent;

public sealed record UnequipRejected(UnequipResult Reason) : GameEvent;

public sealed record ItemUsed(ItemInstance Item) : GameEvent;

public sealed record UseRejected(UseResult Reason) : GameEvent;

public sealed record QuickSlotAssigned(ItemDefinition Item, int Slot = 0) : GameEvent;

public sealed record BoostExpired(ItemDefinition Item) : GameEvent;

public sealed record EnemyIgnited(int EnemyId) : GameEvent;

public sealed record EnemyBurned(int EnemyId, int Damage) : GameEvent;

public sealed record EnemyFrozen(int EnemyId) : GameEvent;

public sealed record EnemyThawed(int EnemyId) : GameEvent;

public sealed record EnemyShocked(int EnemyId, AttackResult Result) : GameEvent;

public sealed record EnemyKnockedBack(int EnemyId, Position To) : GameEvent;

public sealed record ArrowFlew(IReadOnlyList<Position> Path, bool Powered = false) : GameEvent;

public sealed record PlayerSwept(IReadOnlyList<Position> Tiles) : GameEvent;

public sealed record PlayerThrust(IReadOnlyList<Position> Tiles) : GameEvent;

public sealed record EnemySlammed(int EnemyId, int Damage) : GameEvent;

public sealed record EnemyPricked(int EnemyId, int Damage) : GameEvent;

public sealed record LifeDrained(int Amount) : GameEvent;

public sealed record AbilityUsed(ClassAbility Ability) : GameEvent;

public sealed record AbilityRejected(AbilityResult Reason) : GameEvent;

public sealed record AbilityEnded(ClassAbility Ability) : GameEvent;

public sealed record StairsSealed : GameEvent;

public sealed record StairsOpened(Position At) : GameEvent;

public sealed record FloorDescended(int Floor, Position At) : GameEvent;

public sealed record VaultConquered : GameEvent;

public sealed record PlayerScorched(int Damage) : GameEvent;

public sealed record EnemyScorched(int EnemyId, int Damage) : GameEvent;

public sealed record LavaSpread(int EnemyId, IReadOnlyList<Position> Tiles) : GameEvent;

public sealed record LavaCooled(IReadOnlyList<Position> Tiles) : GameEvent;

public sealed record PlayerPulled(int EnemyId, Position To) : GameEvent;

public sealed record CrystalShattered(Position At) : GameEvent;

public sealed record EnemyShardHit(int EnemyId, int Damage) : GameEvent;

public sealed record PlayerShardHit(int Damage) : GameEvent;

public sealed record CrystalsGrew(int EnemyId, IReadOnlyList<Position> Tiles) : GameEvent;

public sealed record CrystalsBurst(int EnemyId, IReadOnlyList<Position> Tiles) : GameEvent;

public sealed record PlayerPoisoned(int Turns) : GameEvent;

public sealed record PlayerPoisonDamaged(int Damage) : GameEvent;

public sealed record PlayerDrifted(Position To) : GameEvent;

public sealed record EnemyDrifted(int EnemyId, Position To) : GameEvent;

public sealed record PlayerBlownAway(int EnemyId, Position To, int Damage) : GameEvent;

public sealed record EnemySummoned(int EnemyId, int MinionId, Position At) : GameEvent;

public sealed record EnemyWithered(int EnemyId) : GameEvent;

public sealed record ItemMarked(ItemInstance Item, ItemMark Mark) : GameEvent;

public sealed record JunkDiscarded(int Count) : GameEvent;

public sealed record SpellCast(SpellDefinition Spell, IReadOnlyList<Position> Path, IReadOnlyList<Position> Area) : GameEvent;

public sealed record SpellSelected(int Index) : GameEvent;

public sealed record SpellLearned(SpellDefinition Spell, int Key, SpellDefinition? Replaced = null) : GameEvent;

public sealed record SpellRejected(SpellResult Reason) : GameEvent;

public sealed record ManaSiphoned(int Amount) : GameEvent;

public sealed record EnemyPulled(int EnemyId, Position To) : GameEvent;

public sealed record CoinsGained(int Amount) : GameEvent;

public sealed record CoinsLost(int Amount) : GameEvent;

public sealed record MerchantGreeted : GameEvent;

public sealed record ItemBought(ItemInstance Item, int Price) : GameEvent;

public sealed record ItemSold(ItemInstance Item, int Price) : GameEvent;

public sealed record JunkSold(int Count, int Price) : GameEvent;

public sealed record BackpackExpanded(int Capacity, int Price) : GameEvent;

public sealed record TradeRejected(TradeResult Reason) : GameEvent;

public sealed record BossPhaseChanged(int EnemyId, int Phase, BossPower Power) : GameEvent;

public sealed record GraveDug(Position At, int Count) : GameEvent;

public sealed record GraveRecovered(int Count, int Left) : GameEvent;

public sealed record GraveLost(Position At, int Count) : GameEvent;

public sealed record SpareWeaponTaken(ItemInstance Item) : GameEvent;

public sealed record HeroLost : GameEvent;

public sealed record WeaponImbued(ItemInstance Weapon) : GameEvent;

public sealed record ExoticTriggered(ExoticPower Power, int? EnemyId = null, int Amount = 0) : GameEvent;

public sealed record EnemyEnvenomed(int EnemyId, int Damage, int Stacks) : GameEvent;

public sealed record Traveled(int Floor, Position Start) : GameEvent;

public sealed record TravelRejected(TravelResult Reason) : GameEvent;

public sealed record TotemAwakened(int EnemyId, int Price) : GameEvent;

public sealed record TotemRejected(TotemResult Reason) : GameEvent;
