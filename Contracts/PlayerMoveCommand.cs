namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    public class PlayerMoveCommand
    {
        public string PlayerId { get; set; } = string.Empty;
        // Dx/Dy should be validated by the sender/receiver:
        // only one can be non-zero, and its value must be 1 or -1.
        public int Dx { get; set; }
        public int Dy { get; set; }
    }
}