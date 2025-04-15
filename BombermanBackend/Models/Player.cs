namespace BombermanBackend.Models
{
    public class Player
    {
        public string Id { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int MaxBombs { get; set; } = 1; // Default max bombs
        public int ActiveBombsCount { get; set; } = 0; // Bombs currently placed by this player
        public int BlastRadius { get; set; } = 1; // Default blast radius
        // Future: Add Speed, CanKickBomb, etc.
    }
}