namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    // Represents the coordinates affected by a single explosion event
    public class AffectedTileDto
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}