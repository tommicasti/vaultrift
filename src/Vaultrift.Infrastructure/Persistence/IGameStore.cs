using System;
using System.Collections.Generic;
using System.Text;
using Vaultrift.Engine.Gameplay;

namespace Vaultrift.Infrastructure.Persistence
{
    public interface IGameStore
    {
        Guid Add (Game game);

        Game? Find(Guid id);    
    }
}
