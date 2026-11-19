using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;

namespace Vaultrift.Engine.Persistence;

public interface IGameContent
{
    ItemDefinition Item(string id);

    EnemyKind EnemyKind(string id);

    CharacterClass Class(string id);
}
