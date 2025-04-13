namespace BombermanBackend.Models
{
    public enum TileType
    {
        Empty,
        Wall,             // Indestructible Wall
        Bomb,
        Player,
        DestructibleWall,
        Explosion         // Represents the blast effect temporarily
    }
}