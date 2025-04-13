namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    // Represents the state of a single bomb for frontend updates
    public class BombStateDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int RemainingFuseTicks { get; set; } // Frontend might use this for animation
        // OwnerId might be useful for styling bombs differently per player
        public string OwnerId { get; set; } = string.Empty;
    }
}