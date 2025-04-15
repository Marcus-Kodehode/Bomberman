namespace BombermanBackend.Models
{
    public enum TileType
    {
        Empty,
        Wall,             // Indestructible Wall
        Bomb,
        Player,
        DestructibleWall,
        Explosion,        // Temporary explosion effect
        PowerUpBombCount, // Powerup: Increases MaxBombs
        PowerUpBlastRadius // Powerup: Increases BlastRadius
        // Future: PowerUpSpeed etc.
    }
}