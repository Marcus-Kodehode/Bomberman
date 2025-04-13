using System.Collections.Generic;

namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    public class BombExplodedEvent
    {
        public int OriginX { get; set; } // Where the bomb was
        public int OriginY { get; set; }
        // Send only the coordinates that turned into 'Explosion' state
        public List<AffectedTileDto> AffectedTiles { get; set; } = new();
        // Include IDs of players hit by this specific explosion
        public List<string> HitPlayerIds { get; set; } = new();
    }
}