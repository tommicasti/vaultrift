using Vaultrift.Engine.Combat;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Gameplay;

public sealed record WeaponMastery(WeaponType WeaponType, StatModifier Bonus);

public sealed record CharacterClass(string Id, string Name, Stats BaseStats, WeaponMastery Mastery);