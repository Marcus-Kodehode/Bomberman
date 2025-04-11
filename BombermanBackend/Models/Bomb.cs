using System; // Retained for potential future use

namespace BombermanBackend.Models
{
    public class Bomb
    {
        public string OwnerId { get; }
        public int X { get; }
        public int Y { get; }
        public int RemainingFuseTicks { get; set; }
        public int BlastRadius { get; set; }

        public Bomb(string ownerId, int x, int y, int initialFuseTicks = 5, int blastRadius = 1)
        {
            OwnerId = ownerId;
            X = x;
            Y = y;
            RemainingFuseTicks = initialFuseTicks;
            BlastRadius = blastRadius;
        }
    }
}