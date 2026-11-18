using Vaultrift.Api.Contracts;
using Vaultrift.Engine.Gameplay;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Api.Mapping;

public static class ClassMapper
{
    public static ClassResponse ToResponse(ClassDefinition definition)
    {
        var playerClass = definition.Class;
        var stats = playerClass.BaseStats;
        var bonus = playerClass.Mastery.Bonus;

        return new ClassResponse(
            playerClass.Id,
            playerClass.Name,
            definition.Description,
            stats.MaxHealth,
            stats.Attack,
            stats.Defense,
            stats.Evasion,
            stats.Critical,
            playerClass.Speed,
            playerClass.Mastery.WeaponType,
            new ModifierResponse(bonus.Stat, bonus.Amount),
            stats.MaxMana,
            playerClass.Ability,
            playerClass.Ability is { } ability ? AbilityRules.Cooldown(ability) : 0);
    }
}
