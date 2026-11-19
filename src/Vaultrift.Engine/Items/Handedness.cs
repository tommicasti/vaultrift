namespace Vaultrift.Engine.Items;

public static class Handedness
{
    public static bool IsTwoHanded(this WeaponType weaponType) => weaponType switch
    {
        WeaponType.Sword or WeaponType.Dagger => false,
        WeaponType.Axe or WeaponType.Bow or WeaponType.Spear or WeaponType.Staff => true,
        _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, "Unknown weapon type.")
    };
}
