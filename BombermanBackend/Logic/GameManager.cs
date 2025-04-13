using BombermanBackend.Models;
using System.Collections.Generic;
using System.Linq;

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
            return _session.MovePlayer(playerId, newX, newY);
        }

        // Updated PlaceBomb to enforce limits and return success/failure
        public bool PlaceBomb(string playerId, int x, int y)
        {
            // 1. Find Player
            if (!_session.Players.TryGetValue(playerId, out Player? player))
            {
                // Player not found
                return false;
            }

            // 2. Check Player's Current Location (should match placement coords)
            if (player.X != x || player.Y != y)
            {
                // Trying to place bomb not at player's feet - disallow for now
                // Or handle differently based on game rules (e.g., power-ups)
                return false;
            }

            // 3. Check Bomb Limit
            if (player.ActiveBombsCount >= player.MaxBombs)
            {
                // Player has reached their bomb limit
                return false;
            }

            // Optional: Check if tile is suitable (e.g., maybe cannot place on Wall tile even if player is somehow there?)
            // TileType currentTile = _session.Map[x,y];
            // if(currentTile == TileType.Wall) return false;

            // 4. If all checks pass, add bomb via session and update player state
            _session.AddBomb(playerId, x, y);
            player.ActiveBombsCount++; // Increment active bombs for this player
            return true; // Placement succeeded
        }

        public void Tick()
        {
            UpdateBombs();
        }

        private void UpdateBombs()
        {
            List<Bomb> bombsToDetonate = new List<Bomb>();
            // Use ToList() for safe iteration while potentially modifying underlying list later
            foreach (var bomb in _session.Bombs.ToList())
            {
                bomb.RemainingFuseTicks--;
                if (bomb.RemainingFuseTicks <= 0)
                {
                    bombsToDetonate.Add(bomb);
                    _session.Bombs.Remove(bomb);
                }
            }

            foreach (var bomb in bombsToDetonate)
            {
                DetonateBomb(bomb);
            }
        }

        // Updated DetonateBomb to decrement player's active count
        private void DetonateBomb(Bomb bomb)
        {
            Console.WriteLine($"*** Detonating Bomb by {bomb.OwnerId} at ({bomb.X}, {bomb.Y})! ***");

            // Decrement owner's active bomb count
            if (_session.Players.TryGetValue(bomb.OwnerId, out Player? owner))
            {
                if (owner.ActiveBombsCount > 0) // Prevent going negative
                {
                    owner.ActiveBombsCount--;
                }
            }

            // Clear bomb tile (placeholder explosion)
            if (bomb.X >= 0 && bomb.X < _session.Map.GetLength(0) && bomb.Y >= 0 && bomb.Y < _session.Map.GetLength(1))
            {
                _session.Map[bomb.X, bomb.Y] = TileType.Empty;
            }

            // TODO: Implement actual blast logic here
        }
    }
}