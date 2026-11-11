using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Infrastructure.Persistence
{
    public sealed class InMemoryGameStore : IGameStore
    {
        private readonly ConcurrentDictionary<Guid, Game> _games = new();

        public Guid Add(Game game)
        {
            ArgumentNullException.ThrowIfNull(game);

            var id = Guid.NewGuid();
            _games[id] = game;
            return id;
        }

        public Game? Find(Guid id) =>
            _games.TryGetValue(id, out var game) ? game : null;
        
    }
}
