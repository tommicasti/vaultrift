using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public sealed partial class Game
{
    public TurnResult UseAbility()
    {
        if (IsLost)
            return Defeated();

        var events = new List<GameEvent>();
        var result = CheckAbility();

        if (result != AbilityResult.Used)
        {
            events.Add(new AbilityRejected(result));
            return new TurnResult(TurnPassed: false, events);
        }

        var ability = Player.Ability!.Value;
        Player.AbilityCooldown = CooldownFor(ability);
        events.Add(new AbilityUsed(ability));

        switch (ability)
        {
            case ClassAbility.Bulwark:
                Player.BulwarkTurns = AbilityRules.BulwarkTurns + Player.EffectAmount(EffectKind.BulwarkExtension);
                break;
            case ClassAbility.Fury:
                Player.FuryTurns = AbilityRules.FuryTurns;
                break;
            case ClassAbility.Invisibility:
                Player.InvisibleTurns = AbilityRules.InvisibilityTurns + Player.EffectAmount(EffectKind.Phantom);
                break;
            case ClassAbility.Overload:
                Player.IsOverloaded = true;
                break;
            case ClassAbility.PowerShot:
                BeginAmbush();
                PowerStrike(events);
                EndAmbush(events);
                EndTurn(events);
                return new TurnResult(TurnPassed: true, events);
            default:
                throw new ArgumentOutOfRangeException(nameof(ability), ability, "Unknown ability.");
        }

        return new TurnResult(TurnPassed: false, events);
    }

    private AbilityResult CheckAbility()
    {
        if (Player.Ability is not { } ability)
            return AbilityResult.NoAbility;

        if (Player.AbilityCooldown > 0)
            return AbilityResult.OnCooldown;

        return AbilityResult.Used;
    }

    private int CooldownFor(ClassAbility ability)
    {
        var reduction = Player.EffectAmount(EffectKind.Haste);

        if (ability == ClassAbility.PowerShot && Player.EffectAmount(EffectKind.PowerShotMastery) > 0)
            reduction += AbilityRules.PowerShotMasteryReductionPercent;

        return AbilityRules.ReducedCooldown(ability, reduction);
    }

    private void TickAbilities(List<GameEvent> events)
    {
        if (Player.AbilityCooldown > 0)
            Player.AbilityCooldown--;

        if (Tick(Player.BulwarkTurns, turns => Player.BulwarkTurns = turns))
            events.Add(new AbilityEnded(ClassAbility.Bulwark));

        if (Tick(Player.FuryTurns, turns => Player.FuryTurns = turns))
            events.Add(new AbilityEnded(ClassAbility.Fury));

        if (Tick(Player.InvisibleTurns, turns => Player.InvisibleTurns = turns))
            events.Add(new AbilityEnded(ClassAbility.Invisibility));
    }

    private static bool Tick(int turns, Action<int> set)
    {
        if (turns <= 0)
            return false;

        set(turns - 1);
        return turns == 1;
    }

    private int WakeDistance => Math.Max(1, EnemyWakeDistance - Player.EffectAmount(EffectKind.Stealth));

    private void Regenerate()
    {
        if (TurnNumber % HealthRegenInterval == 0)
            Player.Heal(HealthRegenAmount);

        var extra = Player.EffectAmount(EffectKind.Regeneration)
            + (Player.FuryTurns > 0 ? Player.EffectAmount(EffectKind.FuryRegeneration) : 0);

        if (extra > 0)
            Player.Heal(extra);

        if (TurnNumber % Progression.ManaRegenInterval == 0)
            Player.RestoreMana(Progression.ManaRegenAmount + Player.EffectAmount(EffectKind.ManaFlow));
    }
}
