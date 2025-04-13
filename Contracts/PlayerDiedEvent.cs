namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    public class PlayerDiedEvent
    {
        public string PlayerId { get; set; } = string.Empty;
        public int X { get; set; } // Position where player died
        public int Y { get; set; }
        // Could add 'killedByPlayerId' later if needed
    }
}