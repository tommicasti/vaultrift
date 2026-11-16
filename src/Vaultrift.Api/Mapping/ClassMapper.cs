using Vaultrift.Api.Contracts;
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
            playerClass.Mastery.WeaponType,
            new ModifierResponse(bonus.Stat, bonus.Amount));
    }
}