namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public const int PoisonTurns = 3;
    public const int PoisonDamagePercent = 5;
    public const int MinimumPoisonDamage = 2;

    public static int PoisonDamage(int maxHealth) =>
        Math.Max(MinimumPoisonDamage, maxHealth * PoisonDamagePercent / 100);

    private void BreatheSpores(List<GameEvent> events)
    {
        if (Player.IsDead || !Map.IsSpores(Player.Position))
            return;

        if (!Player.IsPoisoned)
            events.Add(new PlayerPoisoned(PoisonTurns));

        Player.PoisonTurns = PoisonTurns;
    }

    private void SufferPoison(List<GameEvent> events)
    {
        if (Player.IsDead || !Player.IsPoisoned)
            return;

        var damage = PoisonDamage(Player.Stats.MaxHealth);
        Player.TakeDamage(damage);
        Player.PoisonTurns--;
        events.Add(new PlayerPoisonDamaged(damage));
    }
}
