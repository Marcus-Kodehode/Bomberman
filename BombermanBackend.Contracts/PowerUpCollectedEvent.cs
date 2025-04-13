namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    // Mirror the relevant power-ups from Models.TileType for contract stability
    public enum PowerUpTypeDto
    {
        Unknown = 0,
        BombCount = 1,
        BlastRadius = 2
        // Add others like Speed later, ensuring values match backend logic mapping
    }

    public class PowerUpCollectedEvent
    {
        public string PlayerId { get; set; } = string.Empty;
        public PowerUpTypeDto PowerUpType { get; set; }
        public int NewValue { get; set; } // e.g., the new MaxBombs or new BlastRadius after collection
        public int X { get; set; } // Where it was collected from
        public int Y { get; set; }
    }
}