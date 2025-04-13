namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    // Represents the state of a single player for frontend updates
    public class PlayerStateDto
    {
        public string Id { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int MaxBombs { get; set; }
        public int ActiveBombsCount { get; set; }
        public int BlastRadius { get; set; }
        public bool IsAlive { get; set; } // Explicitly track if the player is considered alive
    }
}