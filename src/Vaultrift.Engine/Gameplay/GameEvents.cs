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

public sealed record QuickSlotAssigned(ItemDefinition Item) : GameEvent;

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
