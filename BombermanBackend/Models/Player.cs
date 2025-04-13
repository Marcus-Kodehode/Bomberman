namespace BombermanBackend.Models
{
    public class Player
    {
        public string Id { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int MaxBombs { get; set; } = 1; // Player starts with a limit of 1 bomb
        public int ActiveBombsCount { get; set; } = 0; // How many bombs they currently have placed
    }
}