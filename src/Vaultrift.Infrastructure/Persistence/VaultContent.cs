using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Persistence;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Persistence;

public sealed class VaultContent : IGameContent
{
    public static VaultContent Instance { get; } = new();

    private VaultContent()
    {
    }

    public ItemDefinition Item(string id) => ItemCatalog.Get(id);

    public EnemyKind EnemyKind(string id) => BestiaryCatalog.Get(id);

    public CharacterClass Class(string id) =>
        ClassCatalog.Find(id)?.Class ?? throw new KeyNotFoundException($"Unknown class '{id}'.");
}
