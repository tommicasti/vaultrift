namespace Vaultrift.Api.Realtime;

public sealed class HeroInWorldException(Guid heroId) : Exception($"Hero {heroId} is playing in a world.")
{
    public Guid HeroId { get; } = heroId;
}
