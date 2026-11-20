using Vaultrift.Engine.Gameplay;
using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Persistence;

public interface IGameStore
{
    GameSession Create(ClassDefinition playerClass, string? playerName, Difficulty difficulty = Difficulty.Normal);

    GameSession? Find(Guid id);

    GameSession? FindByCode(string code);

    void Save(GameSession session);
}
