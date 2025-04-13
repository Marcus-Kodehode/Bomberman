namespace BombermanBackend.Models
{
    public enum TileType
    {
        Empty,
        Wall, // Indestructible Wall
        Bomb,
        Player,
        DestructibleWall // New type for walls that can be destroyed
        // Future: Explosion, Powerup etc.
    }
}