using BombermanBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
// using BombermanBackend.Models.Events; // Add if EventArgs are in subfolder

namespace BombermanBackend.Logic
{
    public class GameManager
    {
        // --- Events ---
        public event EventHandler<PlayerEventArgs>? PlayerDied;
        public event EventHandler<BombExplodedEventArgs>? BombExploded;
        public event EventHandler<GameOverEventArgs>? GameOver;
        // --------------

        private readonly GameSession _session;

        public GameManager(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
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

            _session.AddBomb(playerId, x, y, player.BlastRadius);
            player.ActiveBombsCount++;
            return true;
        }

        public void Tick()
        {
            CleanupExplosions();
            UpdateBombs();
            CheckGameOver(); // Check win condition after updates
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
            foreach (var bomb in _session.Bombs) bomb.RemainingFuseTicks--;

            List<Bomb> bombsToDetonate = _session.Bombs.Where(b => b.RemainingFuseTicks <= 0).ToList();
            if (!bombsToDetonate.Any()) return;

            Queue<Bomb> detonationQueue = new Queue<Bomb>(bombsToDetonate);
            HashSet<Bomb> detonatedBombs = new HashSet<Bomb>();
            List<string> ownerIdsOfDetonatedBombs = new List<string>();

            while (detonationQueue.Count > 0)
            {
                var bomb = detonationQueue.Dequeue();
                if (detonatedBombs.Add(bomb))
                {
                    ownerIdsOfDetonatedBombs.Add(bomb.OwnerId);
                    // DetonateBomb now returns data needed for the event
                    var explosionData = DetonateBomb(bomb, detonationQueue, detonatedBombs);
                    // Raise BombExploded event
                    OnBombExploded(explosionData);
                }
            }

            var ownerCounts = ownerIdsOfDetonatedBombs.GroupBy(id => id).Select(g => new { OwnerId = g.Key, Count = g.Count() });
            foreach (var ownerInfo in ownerCounts)
            {
                if (_session.Players.TryGetValue(ownerInfo.OwnerId, out Player? owner))
                {
                    owner.ActiveBombsCount = Math.Max(0, owner.ActiveBombsCount - ownerInfo.Count);
                }
            }
            _session.Bombs.RemoveAll(b => detonatedBombs.Contains(b));
        }

        // Modified to return data for the event
        private BombExplodedEventArgs DetonateBomb(Bomb bomb, Queue<Bomb> detonationQueue, HashSet<Bomb> detonatedBombs)
        {
            HashSet<(int, int)> affectedTiles = new HashSet<(int, int)>();
            affectedTiles.Add((bomb.X, bomb.Y));

            if (bomb.X >= 0 && bomb.X < _session.Map.GetLength(0) && bomb.Y >= 0 && bomb.Y < _session.Map.GetLength(1))
                _session.Map[bomb.X, bomb.Y] = TileType.Explosion;

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 1; j <= bomb.BlastRadius; j++)
                {
                    int currentX = bomb.X + dx[i] * j;
                    int currentY = bomb.Y + dy[i] * j;
                    if (currentX < 0 || currentX >= _session.Map.GetLength(0) || currentY < 0 || currentY >= _session.Map.GetLength(1)) break;

                    affectedTiles.Add((currentX, currentY));
                    TileType tile = _session.Map[currentX, currentY];

                    if (tile == TileType.Wall) break;
                    if (tile == TileType.DestructibleWall) { _session.Map[currentX, currentY] = TileType.Explosion; break; }
                    if (tile == TileType.Bomb)
                    {
                        var bombToChain = _session.Bombs.FirstOrDefault(b => b.X == currentX && b.Y == currentY);
                        if (bombToChain != null && !detonatedBombs.Contains(bombToChain) && !detonationQueue.Contains(bombToChain))
                            detonationQueue.Enqueue(bombToChain);
                        _session.Map[currentX, currentY] = TileType.Explosion;
                        break;
                    }
                    if (tile == TileType.Explosion) break;
                    _session.Map[currentX, currentY] = TileType.Explosion;
                }
            }

            List<string> playersHitIds = new List<string>();
            List<Player> playersToRemove = new List<Player>(); // Keep track of players to remove
            foreach (var (x, y) in affectedTiles)
            {
                var playersAtTile = _session.Players.Values.Where(p => p.X == x && p.Y == y).ToList();
                foreach (var player in playersAtTile)
                {
                    if (!playersHitIds.Contains(player.Id))
                    {
                        playersHitIds.Add(player.Id);
                        playersToRemove.Add(player); // Add to removal list
                        OnPlayerDied(new PlayerEventArgs(player)); // Raise PlayerDied event HERE
                    }
                }
            }

            // Remove players after iterating and raising events
            foreach (var player in playersToRemove)
            {
                _session.Players.Remove(player.Id);
            }

            // Return data needed for the BombExploded event
            return new BombExplodedEventArgs(bomb, affectedTiles.ToList(), playersHitIds);
        }

        private void CheckGameOver()
        {
            // Simple game over: 1 or 0 players left
            if (_session.Players.Count <= 1)
            {
                string? winnerId = _session.Players.Values.FirstOrDefault()?.Id;
                OnGameOver(new GameOverEventArgs(winnerId));
                // TODO: Add logic to stop the game session ticking or remove it from manager
            }
        }

        // --- Protected methods to raise events ---
        protected virtual void OnPlayerDied(PlayerEventArgs e) => PlayerDied?.Invoke(this, e);
        protected virtual void OnBombExploded(BombExplodedEventArgs e) => BombExploded?.Invoke(this, e);
        protected virtual void OnGameOver(GameOverEventArgs e) => GameOver?.Invoke(this, e);
        // -----------------------------------------
    }
}