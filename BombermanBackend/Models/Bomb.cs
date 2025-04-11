namespace BombermanBackend.Models
{
    public class Bomb
    {
        public string OwnerId { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Timer { get; set; } = 3;
    }
}
