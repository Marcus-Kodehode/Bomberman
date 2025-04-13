namespace BombermanBackend.Models
{
    public class Player
    {
        public string Id { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int MaxBombs { get; set; } = 1;
        public int ActiveBombsCount { get; set; } = 0;
        public int BlastRadius { get; set; } = 1; // Default blast radius 1, moved from Bomb
        // Future: Add Speed, CanKickBomb, etc.
    }
}