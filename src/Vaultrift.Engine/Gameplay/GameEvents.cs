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

public sealed record EnemyAttackedPlayer(int EnemyId, AttackResult Result) : GameEvent;

public sealed record EnemyWokeUp(int EnemyId) : GameEvent;

public sealed record EnemyMoved(int EnemyId, Position To) : GameEvent;

public sealed record PlayerDefeated : GameEvent;

public sealed record PlayerRespawned(Position At) : GameEvent;

public sealed record ItemPickedUp(ItemDefinition Item) : GameEvent;

public sealed record InventoryFull(ItemDefinition Item) : GameEvent;