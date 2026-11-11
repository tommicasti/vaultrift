using System;
using System.Collections.Generic;
using System.Text;
using Vaultrift.Engine.World;

namespace Vaultrift.Engine.Gameplay
{
    public sealed class Game
    {
        public Game(DungeonMap map, Position playerStart)
        {

            ArgumentNullException.ThrowIfNull(map);

            if (!map.IsWalkable(playerStart))
                throw new ArgumentException("The player must start on a floor tile.", nameof(playerStart));

            Map = map;
            PlayerPosition = playerStart;
        }
        public DungeonMap Map { get; }

        public Position PlayerPosition { get; private set; }


        public MoveOutcome MovePlayer(Direction direction)
        {
            var target = PlayerPosition.Move(direction);
            
            if (!Map.IsWalkable(target))
                return MoveOutcome.Blocked;
            
            PlayerPosition = target;    
            return MoveOutcome.Moved;
        }
    }
}
