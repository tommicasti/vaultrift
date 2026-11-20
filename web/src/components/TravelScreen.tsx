import type { WaystoneState } from '../api/types'
import { biomeNames } from '../game/biomes'

interface TravelScreenProps {
  waystone: WaystoneState
  currentFloor: number
  disabled: boolean
  onTravel: (floor: number) => void
  onClose: () => void
}

export function TravelScreen({ waystone, currentFloor, disabled, onTravel, onClose }: TravelScreenProps) {
  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label="Waystone">
      <section className="victory-screen travel-screen">
        <h2>Waystone</h2>
        <p className="label">
          You are on floor {currentFloor}. Floors you travel to are rebuilt from scratch. On cleared floors a totem stands where the boss was: pay it
          to fight the boss again.
        </p>
        {!waystone.isNearby && <p className="label">Stand next to the waystone to travel.</p>}
        <ul className="travel-list">
          {waystone.destinations.map(destination => (
            <li key={destination.floor}>
              <button disabled={disabled || !waystone.isNearby} onClick={() => onTravel(destination.floor)}>
                <span>
                  Floor {destination.floor}
                  {destination.biome && <span className="label"> · {biomeNames[destination.biome] ?? destination.biome}</span>}
                </span>
                <span className="label">{destination.isCleared ? 'Totem' : 'Boss still there'}</span>
              </button>
            </li>
          ))}
        </ul>
        <div className="victory-actions">
          <button onClick={onClose}>Close</button>
        </div>
      </section>
    </div>
  )
}
