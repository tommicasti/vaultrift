using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Realtime;

public abstract record LiveEvent;

public sealed record HeroSwung(int HeroId) : LiveEvent;

public sealed record MonsterHit(int HeroId, int MonsterId, AttackResult Result, int HealthLeft) : LiveEvent;

public sealed record MonsterSlain(int HeroId, int MonsterId) : LiveEvent;

public sealed record MonsterWoke(int MonsterId) : LiveEvent;

public sealed record MonsterWindup(int MonsterId, int HeroId) : LiveEvent;

public sealed record HeroHit(int MonsterId, int HeroId, AttackResult Result, int HealthLeft) : LiveEvent;

public sealed record HeroFell(int HeroId) : LiveEvent;

public sealed record HeroRespawned(int HeroId) : LiveEvent;

public sealed record HeroRewarded(int HeroId, int MonsterId, int Coins, int Experience, int LevelsGained) : LiveEvent;

public sealed record LootDropped(int ItemId, int MonsterId) : LiveEvent;

public sealed record LootPickedUp(int HeroId, int ItemId, ItemInstance Item) : LiveEvent;

public sealed record BackpackFull(int HeroId) : LiveEvent;

public sealed record BoostFaded(int HeroId, ItemDefinition Source) : LiveEvent;

public sealed record AbilityActivated(int HeroId, ClassAbility Ability) : LiveEvent;

public sealed record AbilityFaded(int HeroId, ClassAbility Ability) : LiveEvent;

public sealed record MonsterIgnited(int MonsterId) : LiveEvent;

public sealed record MonsterScorched(int HeroId, int MonsterId, int Damage, int HealthLeft) : LiveEvent;

public sealed record MonsterFrozen(int MonsterId) : LiveEvent;

public sealed record MonsterPushed(int MonsterId) : LiveEvent;

public sealed record SpellUnleashed(int HeroId, SpellDefinition Spell, Vector Center, double Radius) : LiveEvent;

public sealed record ShotFired(int HeroId, int ProjectileId) : LiveEvent;
