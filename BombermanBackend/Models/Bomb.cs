using System;

namespace BombermanBackend.Models
{
    public class Bomb
    {
        public string OwnerId { get; }
        public int X { get; }
        public int Y { get; }
        public int RemainingFuseTicks { get; set; }
        public int BlastRadius { get; } // Stores the blast radius *at the time of placement*

        // Constructor now takes blastRadius from the placing player
        public Bomb(string ownerId, int x, int y, int blastRadius, int initialFuseTicks = 5) // Default fuse 5 ticks
        {
            OwnerId = ownerId;
            X = x;
            Y = y;
            RemainingFuseTicks = initialFuseTicks;
            BlastRadius = blastRadius; // Set from parameter
        }
    }
}