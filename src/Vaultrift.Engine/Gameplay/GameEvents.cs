using Vaultrift.Engine.Combat;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay;

public abstract record GameEvent;

public sealed record PlayerMoved(Position To) : GameEvent;

public sealed record PlayerTurned(Direction Facing) : GameEvent;

public sealed record PlayerAttackedEnemy(int EnemyId, AttackResult Result) : GameEvent;

public sealed record PlayerAttackedNothing : GameEvent;

public sealed record EnemyDefeated(int EnemyId) : GameEvent;

public sealed record EnemyAttackedPlayer(int EnemyId, AttackResult Result) : GameEvent;