namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    public class GameOverEvent
    {
        // Can be null/empty if it's a draw or no winner determined
        public string? WinnerPlayerId { get; set; }
        // Could add final scores, game duration etc.
    }
}