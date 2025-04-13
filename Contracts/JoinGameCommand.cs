namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    public class JoinGameCommand
    {
        // Suggest an ID, backend might confirm or assign a different one
        public string DesiredPlayerId { get; set; } = string.Empty;
        // Could add preferred starting team/color etc. later
    }
}