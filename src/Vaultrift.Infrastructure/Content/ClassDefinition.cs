using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Infrastructure.Content;

public sealed record ClassDefinition(CharacterClass Class, string Description, IReadOnlyList<string> StartingItemIds);