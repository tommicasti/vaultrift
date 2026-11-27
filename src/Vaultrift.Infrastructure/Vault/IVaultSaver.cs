using Vaultrift.Engine.Items;

namespace Vaultrift.Infrastructure.Vault;

public interface IVaultSaver
{
    void Save(HeroSession hero);

    void Save(WorldSession world);

    void Lose(WorldSession world);

    void Keep(Guid accountId, ItemInstance item);
}
