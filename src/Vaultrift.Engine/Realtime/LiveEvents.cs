using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Realtime;

public abstract record LiveEvent;

public sealed record HeroSwung(int HeroId) : LiveEvent;

public sealed record MonsterHit(int HeroId, int MonsterId, AttackResult Result, int HealthLeft) : LiveEvent;

public sealed record MonsterSlain(int HeroId, int MonsterId) : LiveEvent;

public sealed record MonsterWoke(int MonsterId) : LiveEvent;

public sealed record MonsterWindup(int MonsterId, int HeroId) : LiveEvent;

public sealed record HeroHit(int MonsterId, int HeroId, AttackResult Result, int HealthLeft) : LiveEvent;

public sealed record HeroFell(int HeroId, int CoinsLost = 0) : LiveEvent;

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

public sealed record ChestLooted(int HeroId, int ChestId, int Coins) : LiveEvent;

public sealed record MimicSprang(int HeroId, int ChestId, int MonsterId) : LiveEvent;

public sealed record StairsUnsealed(Position At) : LiveEvent;

public sealed record HeroAtStairs(int HeroId) : LiveEvent;

public sealed record LiveMerchantGreeted(int HeroId) : LiveEvent;

public sealed record LiveWaystoneTouched(int HeroId) : LiveEvent;

public sealed record LiveGraveDug(int HeroId, int GraveId, int Count) : LiveEvent;

public sealed record LiveGraveRecovered(int HeroId, int GraveId, int Recovered, int Left) : LiveEvent;

public sealed record LiveGraveLost(int GraveId, Guid Owner) : LiveEvent;

public sealed record LiveTotemPrimed(int HeroId, int Price, bool CanAfford) : LiveEvent;

public sealed record LiveTotemAwakened(int HeroId, int MonsterId, int Price) : LiveEvent;

public sealed record LiveBossPhaseChanged(int MonsterId, int Phase, BossPower Power) : LiveEvent;

public sealed record TilesChanged(IReadOnlyList<Position> Tiles, TileType Tile, int? MonsterId = null) : LiveEvent;

public sealed record HeroPulled(int HeroId, int MonsterId) : LiveEvent;

public sealed record HeroBlownAway(int HeroId, int MonsterId, int Damage) : LiveEvent;

public sealed record HeroShardHit(int HeroId, int MonsterId, int Damage) : LiveEvent;

public sealed record HeroScorched(int HeroId, int Damage) : LiveEvent;

public sealed record MonsterSummoned(int MonsterId, int SummonerId) : LiveEvent;

public sealed record MonsterWithered(int MonsterId) : LiveEvent;

public sealed record BossSlain(int MonsterId) : LiveEvent;

public sealed record FloorCleared(int Floor) : LiveEvent;

public sealed record LiveVaultConquered : LiveEvent;

public sealed record CrystalBroken(int HeroId, Position Tile) : LiveEvent;

public sealed record ObstacleBroken(int HeroId, Position Tile) : LiveEvent;

public sealed record MonsterShardHit(int HeroId, int MonsterId, int Damage, int Health) : LiveEvent;

public sealed record MonsterBurnedByLava(int MonsterId, int Damage, int Health) : LiveEvent;

public sealed record HeroPoisoned(int HeroId) : LiveEvent;

public sealed record HeroPoisonDamaged(int HeroId, int Damage) : LiveEvent;

public sealed record HeroLifeDrained(int HeroId, int Amount) : LiveEvent;

public sealed record HeroManaSiphoned(int HeroId, int Amount) : LiveEvent;

public sealed record MonsterPricked(int HeroId, int MonsterId, int Damage, int Health) : LiveEvent;

public sealed record MonsterEnvenomed(int HeroId, int MonsterId, int Damage, int Stacks, int Health) : LiveEvent;

public sealed record LiveExoticTriggered(int HeroId, ExoticPower Power, int? MonsterId = null, int Amount = 0) : LiveEvent;

public sealed record LiveRewardGranted(int HeroId, string ItemName, bool Stashed) : LiveEvent;
