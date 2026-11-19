using Vaultrift.Engine.Gameplay;
using Vaultrift.Engine.Items;
using Vaultrift.Engine.Persistence;

namespace Vaultrift.Engine.Tests.Fakes;

public sealed class FakeContent : IGameContent
{
    private readonly Dictionary<string, ItemDefinition> _items = [];
    private readonly Dictionary<string, EnemyKind> _kinds = [];
    private readonly Dictionary<string, CharacterClass> _classes = [];

    public FakeContent With(params ItemDefinition[] items)
    {
        foreach (var item in items)
            _items[item.Id] = item;

        return this;
    }

    public FakeContent With(params EnemyKind[] kinds)
    {
        foreach (var kind in kinds)
            _kinds[kind.Id] = kind;

        return this;
    }

    public FakeContent With(CharacterClass characterClass)
    {
        _classes[characterClass.Id] = characterClass;
        return this;
    }

    public ItemDefinition Item(string id) => _items[id];

    public EnemyKind EnemyKind(string id) => _kinds[id];

    public CharacterClass Class(string id) => _classes[id];
}
