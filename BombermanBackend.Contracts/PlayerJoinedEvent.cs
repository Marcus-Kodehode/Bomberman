namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    public class PlayerJoinedEvent
    {
        // Use the PlayerStateDto to send the full state of the newly joined player
        public PlayerStateDto Player { get; set; } = null!;
    }
}