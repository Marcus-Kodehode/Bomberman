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
            CleanupExplosions();
            UpdateBombs();
        }

        private void CleanupExplosions()
        {
            int width = _session.Map.GetLength(0);
            int height = _session.Map.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_session.Map[x, y] == TileType.Explosion)
                    {
                        _session.Map[x, y] = TileType.Empty;
                    }
                }
            }
        }

        private void UpdateBombs()
        {
            List<Bomb> bombsReadyToDetonate = new List<Bomb>();
            foreach (var bomb in _session.Bombs.ToList())
            {
                bomb.RemainingFuseTicks--;
                if (bomb.RemainingFuseTicks <= 0)
                {
                    bombsReadyToDetonate.Add(bomb);
                    _session.Bombs.Remove(bomb);
                }
            }

            if (!bombsReadyToDetonate.Any()) return;

            Queue<Bomb> detonationQueue = new Queue<Bomb>(bombsReadyToDetonate);
            HashSet<Bomb> processedBombs = new HashSet<Bomb>(bombsReadyToDetonate);
            List<string> ownerIdsOfDetonatedBombs = new List<string>();

            while (detonationQueue.Count > 0)
            {
                var bombToDetonate = detonationQueue.Dequeue();
                ownerIdsOfDetonatedBombs.Add(bombToDetonate.OwnerId);
                DetonateBomb(bombToDetonate, detonationQueue, processedBombs);
            }

            var ownerCounts = ownerIdsOfDetonatedBombs.GroupBy(id => id)
                                                      .Select(g => new { OwnerId = g.Key, Count = g.Count() });

            foreach (var ownerInfo in ownerCounts)
            {
                if (_session.Players.TryGetValue(ownerInfo.OwnerId, out Player? owner))
                {
                    owner.ActiveBombsCount = Math.Max(0, owner.ActiveBombsCount - ownerInfo.Count);
                }
            }
        }

        private void DetonateBomb(Bomb bomb, Queue<Bomb> detonationQueue, HashSet<Bomb> processedBombs)
        {
            // Note: Decrementing owner count is now handled reliably in UpdateBombs *after* this method runs

            HashSet<(int, int)> affectedTiles = new HashSet<(int, int)>();
            affectedTiles.Add((bomb.X, bomb.Y));

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 1; j <= bomb.BlastRadius; j++)
                {
                    int targetX = bomb.X + dx[i] * j;
                    int targetY = bomb.Y + dy[i] * j;
                    if (targetX < 0 || targetX >= _session.Map.GetLength(0) || targetY < 0 || targetY >= _session.Map.GetLength(1)) break;
                    affectedTiles.Add((targetX, targetY));
                    TileType tileType = _session.Map[targetX, targetY];
                    if (tileType == TileType.Wall) break;
                    if (tileType == TileType.DestructibleWall) break;
                    if (tileType == TileType.Player || tileType == TileType.Bomb) break;
                }
            }

            List<Player> playersToRemove = new List<Player>();
            foreach (var (x, y) in affectedTiles)
            {
                TileType currentTileType = _session.Map[x, y];
                Player? playerOnTile = _session.Players.Values.FirstOrDefault(p => p.X == x && p.Y == y);
                if (playerOnTile != null && !playersToRemove.Contains(playerOnTile))
                {
                    // Removed debug Console.WriteLine for player hit
                    playersToRemove.Add(playerOnTile);
                }
                Bomb? bombOnTile = _session.Bombs.FirstOrDefault(b => b.X == x && b.Y == y);
                if (bombOnTile != null)
                {
                    if (processedBombs.Add(bombOnTile))
                    {
                        // Removed debug Console.WriteLine for chain reaction
                        _session.Bombs.Remove(bombOnTile);
                        detonationQueue.Enqueue(bombOnTile);
                    }
                }
                if (currentTileType != TileType.Wall)
                {
                    _session.Map[x, y] = TileType.Explosion;
                }
            }
            foreach (var playerToRemove in playersToRemove)
            {
                _session.Players.Remove(playerToRemove.Id);
                // Removed debug Console.WriteLine for player removal
            }
        }
    }
}