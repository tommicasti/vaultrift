import { useMemo } from 'react'
import type { GameState } from '../api/types'
import { layoutKey, remember, visibleTiles } from '../scene3d/vision'

export function useExplored(game: GameState | null): ReadonlySet<string> {
  const map = game?.map
  const id = game?.id
  const floor = game?.floor
  const x = game?.player.x
  const y = game?.player.y

  return useMemo(() => {
    if (!map || id === undefined || floor === undefined || x === undefined || y === undefined) return new Set<string>()
    return remember(layoutKey(id, floor, map), map, visibleTiles(map, x, y))
  }, [map, id, floor, x, y])
}
