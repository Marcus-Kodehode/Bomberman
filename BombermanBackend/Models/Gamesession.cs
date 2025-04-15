using System;
using System.Collections.Generic;
using System.Linq;
// using BombermanBackend.Models.Events; // Add if EventArgs are in subfolder

namespace BombermanBackend.Models
{
    public class GameSession
    {
        // --- Events ---
        public event EventHandler<PlayerEventArgs>? PlayerJoined;
        public event EventHandler<PowerUpCollectedEventArgs>? PowerUpCollected;
        // --------------

        public TileType[,] Map { get; set; }
        public Dictionary<string, Player> Players { get; set; } = new();
        public List<Bomb> Bombs { get; set; } = new();

        public GameSession(int width, int height) { Map = new TileType[width, height]; }

        public void AddPlayer(Player player)
        {
            if (!Players.ContainsKey(player.Id))
            {
                Players[player.Id] = player;
                bool placedOnMap = false;
                if (player.X >= 0 && player.X < Map.GetLength(0) && player.Y >= 0 && player.Y < Map.GetLength(1))
                {
                    if (Map[player.X, player.Y] == TileType.Empty)
                    {
                        Map[player.X, player.Y] = TileType.Player;
                        placedOnMap = true;
                    }
                }
                // Raise event only if player was successfully added and placed
                if (placedOnMap)
                {
                    OnPlayerJoined(new PlayerEventArgs(player));
                }
            }
        }

        public void AddBomb(string playerId, int x, int y, int blastRadius)
        {
            if (x >= 0 && x < Map.GetLength(0) && y >= 0 && y < Map.GetLength(1))
            {
                Bomb bomb = new Bomb(playerId, x, y, blastRadius);
                Bombs.Add(bomb);
                Map[x, y] = TileType.Bomb;
            }
        }

        public bool IsTileEmpty(int x, int y)
        {
            if (x < 0 || x >= Map.GetLength(0) || y < 0 || y >= Map.GetLength(1)) return false;
            return Map[x, y] == TileType.Empty;
        }

        public bool MovePlayer(string playerId, int newX, int newY)
        {
            if (!Players.TryGetValue(playerId, out Player? player)) return false;

            int oldX = player.X;
            int oldY = player.Y;

            if (Math.Abs(newX - oldX) + Math.Abs(newY - oldY) != 1) return false; // Ensure cardinal, 1 step
            if (newX < 0 || newX >= Map.GetLength(0) || newY < 0 || newY >= Map.GetLength(1)) return false; // Bounds check

            TileType targetTile = Map[newX, newY];
            bool isTargetTraversable = (targetTile == TileType.Empty ||
                                       targetTile == TileType.Bomb ||
                                       targetTile == TileType.PowerUpBombCount ||
                                       targetTile == TileType.PowerUpBlastRadius ||
                                       targetTile == TileType.Explosion);

            if (!isTargetTraversable) return false;

            TileType collectedPowerUp = TileType.Empty; // Track if powerup collected
            if (targetTile == TileType.PowerUpBombCount)
            {
                player.MaxBombs++;
                collectedPowerUp = targetTile;
            }
            else if (targetTile == TileType.PowerUpBlastRadius)
            {
                player.BlastRadius++;
                collectedPowerUp = targetTile;
            }

            player.X = newX;
            player.Y = newY;

            if (Map[oldX, oldY] == TileType.Player) Map[oldX, oldY] = TileType.Empty;
            Map[newX, newY] = TileType.Player;

            // Raise PowerUpCollected event if one was picked up
            if (collectedPowerUp != TileType.Empty)
            {
                OnPowerUpCollected(new PowerUpCollectedEventArgs(player, collectedPowerUp, newX, newY));
            }

            return true;
        }

        // --- Protected methods to raise events ---
        protected virtual void OnPlayerJoined(PlayerEventArgs e) => PlayerJoined?.Invoke(this, e);
        protected virtual void OnPowerUpCollected(PowerUpCollectedEventArgs e) => PowerUpCollected?.Invoke(this, e);
        // -----------------------------------------
    }
}