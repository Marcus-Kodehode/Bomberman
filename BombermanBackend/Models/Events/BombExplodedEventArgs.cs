using System;
using System.Collections.Generic;

namespace BombermanBackend.Models // Or Models.Events
{
    public class BombExplodedEventArgs : EventArgs
    {
        // Consider sending Bomb details or just coordinates/radius
        public int OriginX { get; }
        public int OriginY { get; }
        public int BlastRadius { get; }
        public string OwnerId { get; }
        public List<(int X, int Y)> AffectedTiles { get; }
        public List<string> HitPlayerIds { get; } // IDs of players hit by this specific explosion

        public BombExplodedEventArgs(Bomb bomb, List<(int X, int Y)> affectedTiles, List<string> hitPlayerIds)
        {
            OwnerId = bomb.OwnerId;
            OriginX = bomb.X;
            OriginY = bomb.Y;
            BlastRadius = bomb.BlastRadius;
            AffectedTiles = affectedTiles ?? new List<(int X, int Y)>();
            HitPlayerIds = hitPlayerIds ?? new List<string>();
        }
    }
}