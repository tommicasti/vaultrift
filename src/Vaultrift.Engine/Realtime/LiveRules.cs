using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Engine.Realtime;

public static class LiveRules
{
    public const int TicksPerSecond = 20;
    public const int MaxHeroes = 6;
    public const double ExtraHeroHealth = 0.3;
    public const double ExtraHeroBossHealth = 0.6;
    public const int HeroesPerReinforcement = 2;
    public const double ReinforcementShare = 0.35;
    public const double SeparationShare = 0.25;
    public const double TickSeconds = 1.0 / TicksPerSecond;

    public const double HeroRadius = 0.3;
    public const double MonsterRadius = 0.35;
    public const double BossRadius = 0.55;

    public const double HeroReach = 1.0;
    public const double HeroAttackSeconds = 0.55;
    public const double ShotRange = 6.0;
    public const double ArrowSpeed = 18.0;
    public const double SpellSpeed = 11.0;
    public const double ProjectileStep = 0.25;
    public const double ProjectileRadius = 0.12;
    public const double PointBlankDistance = 1.3;
    public const int PiercePercent = 75;
    public const double LineHalfWidth = 0.45;
    public const double ShockDistance = 1.8;
    public const double HeroRespawnSeconds = 3.0;
    public const double PickUpReach = 0.6;
    public const double FullNoticeSeconds = 2.0;
    public const double FixtureReach = 1.3;
    public const double FixtureRadius = 0.35;
    public const double TotemPrimeSeconds = 3.0;
    public const int FirstSpawnedMonsterId = 1_000_000;
    public const int LavaCoolingSeconds = 3;
    public const double PullStep = 1.0;
    public const double GustDistance = 3.0;
    public const double GustRange = 3.0;
    public const double CurrentSpeed = 3.0;
    public const double ShardRadius = 1.5;
    public const double SmashReach = 0.95;
    public const double ShadowstepCooldownSeconds = 3.0;
    public const double ShadowstepExtraReach = 1.6;
    public const double StaggerSeconds = 1.0;
    public const double LastWallCooldownSeconds = 1.0;
    public const double MarkSeconds = 3.0;
    public const double StarfallExtraReach = 1.0;
    public const double DoubleShotSpread = 0.12;
    public const double AimConeCosine = 0.866;

    public const double MonsterReach = 0.45;
    public const double MonsterReachGrace = 0.3;
    public const double MonsterWindupSeconds = 0.5;
    public const double WakeDistance = 5.0;
    public const int ChaseDistance = 10;
    public const double LeashDistance = 12;
    public const double LoseSightSeconds = 4;
    public const double RespawnShieldSeconds = 3;
    public const int BossChaseDistance = 24;
    public const double ReturnHomeSeconds = 10;
    public const double SprintFactor = 1.6;
    public const double HeadingInertia = 0.6;
    public const double ShuffleThreshold = 0.01;
    public const double FaceTheWayDistance = 1.0;
    public const double SprintNoise = 2.0;
    public const int WakePathSlack = 2;

    public static double HeroSpeed(Speed speed) => speed == Speed.Fast ? 4.6 : 3.8;

    public static double MonsterSpeed(Speed speed) => speed switch
    {
        Speed.Slow => 1.8,
        Speed.Fast => 3.6,
        _ => 2.6
    };

    public static double MonsterAttackSeconds(Speed speed) => speed switch
    {
        Speed.Slow => 1.6,
        Speed.Fast => 0.9,
        _ => 1.2
    };
}
