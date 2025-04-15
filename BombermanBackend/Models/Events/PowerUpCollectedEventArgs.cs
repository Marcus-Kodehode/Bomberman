using System;

namespace BombermanBackend.Models // Or Models.Events
{
    public class PowerUpCollectedEventArgs : EventArgs
    {
        public Player Player { get; }
        public TileType PowerUpType { get; } // The type collected
        public int X { get; } // Location where collected
        public int Y { get; }

        public PowerUpCollectedEventArgs(Player player, TileType powerUpType, int x, int y)
        {
            Player = player;
            PowerUpType = powerUpType;
            X = x;
            Y = y;
        }
    }
}