using BombermanBackend.Models;
using System.Collections.Generic;
using System.Linq; // Needed for ToList

namespace BombermanBackend.Logic
{
    public class GameManager
    {
        private readonly GameSession _session;

        public GameManager(GameSession session)
        {
            _session = session;
        }

        public bool MovePlayer(string playerId, int newX, int newY)
        {
            // Delegate movement logic to GameSession for now
            return _session.MovePlayer(playerId, newX, newY);
        }

        public void PlaceBomb(string playerId, int x, int y)
        {
            // Delegate bomb placement to GameSession
            _session.AddBomb(playerId, x, y);
        }

        // New method to update game state, called periodically (e.g., every second)
        public void Tick()
        {
            UpdateBombs();
            // Future: Update other game elements (enemies, power-up timers, etc.)
        }

        private void UpdateBombs()
        {
            // Use ToList() to create a copy, allowing removal from the original list during iteration
            List<Bomb> bombsToDetonate = new List<Bomb>();
            foreach (var bomb in _session.Bombs.ToList())
            {
                bomb.RemainingFuseTicks--;
                if (bomb.RemainingFuseTicks <= 0)
                {
                    bombsToDetonate.Add(bomb);
                    _session.Bombs.Remove(bomb); // Remove from active bombs list
                }
            }

            foreach (var bomb in bombsToDetonate)
            {
                DetonateBomb(bomb);
            }
        }

        // Placeholder for actual explosion logic
        private void DetonateBomb(Bomb bomb)
        {
            Console.WriteLine($"*** Detonating Bomb by {bomb.OwnerId} at ({bomb.X}, {bomb.Y})! ***");

            // --- Stage 1: Simple Tile Clearing (as simulated before) ---
            if (bomb.X >= 0 && bomb.X < _session.Map.GetLength(0) && bomb.Y >= 0 && bomb.Y < _session.Map.GetLength(1))
            {
                _session.Map[bomb.X, bomb.Y] = TileType.Empty; // Clear the bomb's own tile
            }

            // --- Stage 2: Implement Actual Blast Logic Here (NEXT STEP) ---
            // TODO:
            // 1. Calculate blast coordinates in all 4 directions up to bomb.BlastRadius.
            // 2. Stop blast propagation in a direction if it hits a Wall tile.
            // 3. Check what's on each affected tile:
            //    - Empty: Maybe place a temporary 'Explosion' tile type?
            //    - Player: Damage/eliminate player.
            //    - Destructible Wall (if added): Destroy wall, maybe reveal power-up.
            //    - Other Bombs: Trigger chain reaction (call DetonateBomb recursively or add to detonation list).
            //    - Indestructible Wall: Blast stops.
            // 4. Update the _session.Map accordingly.
        }
    }
}