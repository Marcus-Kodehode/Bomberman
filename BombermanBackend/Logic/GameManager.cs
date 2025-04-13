using BombermanBackend.Models;
using System;
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

        public bool PlaceBomb(string playerId, int x, int y)
        {
            if (!_session.Players.TryGetValue(playerId, out Player? player)) return false;
            if (player.X != x || player.Y != y) return false;
            if (player.ActiveBombsCount >= player.MaxBombs) return false;

            _session.AddBomb(playerId, x, y);
            player.ActiveBombsCount++;
            return true;
        }

        public void Tick()
        {
            UpdateBombs();
        }

        private void UpdateBombs()
        {
            List<Bomb> bombsToDetonate = new List<Bomb>();
            foreach (var bomb in _session.Bombs.ToList())
            {
                bomb.RemainingFuseTicks--;
                if (bomb.RemainingFuseTicks <= 0)
                {
                    bombsToDetonate.Add(bomb);
                    _session.Bombs.Remove(bomb);
                }
            }

            Queue<Bomb> detonationQueue = new Queue<Bomb>(bombsToDetonate);
            HashSet<Bomb> processedBombs = new HashSet<Bomb>(bombsToDetonate);

            while (detonationQueue.Count > 0)
            {
                var bomb = detonationQueue.Dequeue();
                DetonateBomb(bomb, detonationQueue, processedBombs);
            }
        }

        private void DetonateBomb(Bomb bomb, Queue<Bomb> detonationQueue, HashSet<Bomb> processedBombs)
        {
            if (_session.Players.TryGetValue(bomb.OwnerId, out Player? owner))
            {
                if (owner.ActiveBombsCount > 0) owner.ActiveBombsCount--;
            }

            HashSet<(int, int)> affectedTiles = new HashSet<(int, int)>();
            affectedTiles.Add((bomb.X, bomb.Y)); // Bomb's own tile

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 1; j <= bomb.BlastRadius; j++)
                {
                    int targetX = bomb.X + dx[i] * j;
                    int targetY = bomb.Y + dy[i] * j;

                    if (targetX < 0 || targetX >= _session.Map.GetLength(0) || targetY < 0 || targetY >= _session.Map.GetLength(1))
                        break;

                    affectedTiles.Add((targetX, targetY));
                    TileType tileType = _session.Map[targetX, targetY];

                    // Stop blast if it hits an indestructible wall
                    if (tileType == TileType.Wall)
                        break;

                    // --- ADDED: Handle DestructibleWall ---
                    // If it hits a destructible wall, stop blast *after* this tile
                    if (tileType == TileType.DestructibleWall)
                        break;
                    // --- END ADDED ---

                    // Stop blast propagation *after* hitting a player or another bomb
                    if (tileType == TileType.Player || tileType == TileType.Bomb)
                        break;
                }
            }

            List<Player> playersToRemove = new List<Player>();
            foreach (var (x, y) in affectedTiles)
            {
                // Check for players
                Player? playerOnTile = _session.Players.Values.FirstOrDefault(p => p.X == x && p.Y == y);
                if (playerOnTile != null && !playersToRemove.Contains(playerOnTile))
                {
                    Console.WriteLine($"--- Player {playerOnTile.Id} hit by explosion at ({x},{y})! ---");
                    playersToRemove.Add(playerOnTile);
                }

                // Check for other bombs (chain reaction)
                Bomb? bombOnTile = _session.Bombs.FirstOrDefault(b => b.X == x && b.Y == y);
                if (bombOnTile != null)
                {
                    if (processedBombs.Add(bombOnTile))
                    {
                        Console.WriteLine($"--- Chain reaction: Bomb at ({x},{y}) triggered! ---");
                        _session.Bombs.Remove(bombOnTile);
                        detonationQueue.Enqueue(bombOnTile);
                    }
                }

                // --- MODIFIED: Update map tile ---
                TileType currentTileType = _session.Map[x, y];
                // Clear tile if it's not an indestructible wall
                if (currentTileType != TileType.Wall)
                {
                    // If it was destructible, it's now empty. Otherwise clear it.
                    // Also handles clearing Bomb/Player/Empty tiles.
                    _session.Map[x, y] = TileType.Empty;
                    // TODO: Maybe drop powerup if DestructibleWall was destroyed?
                }
                // --- END MODIFIED ---
            }

            // Remove hit players
            foreach (var playerToRemove in playersToRemove)
            {
                _session.Players.Remove(playerToRemove.Id);
                Console.WriteLine($"--- Player {playerToRemove.Id} removed from game. ---");
            }
        }
    }
}