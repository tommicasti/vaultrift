using Vaultrift.Infrastructure.Content;

namespace Vaultrift.Infrastructure.Persistence;

public interface IGameStore
{
    GameSession Create(ClassDefinition playerClass, string? playerName);

    GameSession? Find(Guid id);

    GameSession? FindByCode(string code);

    void Save(GameSession session);
}
