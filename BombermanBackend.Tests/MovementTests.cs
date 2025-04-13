using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;

namespace BombermanBackend.Tests
{
    public class MovementTests
    {
        // Removed readonly, kept null!
        private GameSession _session = null!;
        private GameManager _manager = null!;
        private Player _player1 = null!;

        public MovementTests()
        {
            SetupTest(); // Initializes fields via helper
        }

        private void SetupTest(int width = 7, int height = 7)
        {
            _session = new GameSession(width, height);
            // Add border walls
            for (int x = 0; x < width; x++)
            {
                _session.Map[x, 0] = TileType.Wall;
                _session.Map[x, height - 1] = TileType.Wall;
            }
            for (int y = 1; y < height - 1; y++)
            {
                _session.Map[0, y] = TileType.Wall;
                _session.Map[width - 1, y] = TileType.Wall;
            }
            _manager = new GameManager(_session);
            _player1 = new Player { Id = "p1", X = 1, Y = 1 }; // Default R=1, MaxB=1
            _session.AddPlayer(_player1);
        }

        [Fact]
        public void Player_Can_Move_Into_Empty_Space()
        {
            int targetX = _player1.X + 1;
            int targetY = _player1.Y;
            bool moved = _manager.MovePlayer(_player1.Id, targetX, targetY);
            Assert.True(moved);
            Assert.Equal(targetX, _player1.X);
            Assert.Equal(targetY, _player1.Y);
            Assert.Equal(TileType.Player, _session.Map[targetX, targetY]);
            Assert.Equal(TileType.Empty, _session.Map[1, 1]);
        }

        [Fact]
        public void Player_Cannot_Move_Into_Wall()
        {
            _session.Map[2, 1] = TileType.Wall;
            int originalX = _player1.X;
            int originalY = _player1.Y;
            bool moved = _manager.MovePlayer(_player1.Id, originalX + 1, originalY);
            Assert.False(moved);
            Assert.Equal(originalX, _player1.X);
            Assert.Equal(originalY, _player1.Y);
        }

        [Fact]
        public void Player_Cannot_Move_Out_Of_Bounds()
        {
            int originalX = _player1.X;
            int originalY = _player1.Y;
            bool moved = _manager.MovePlayer(_player1.Id, originalX - 1, originalY); // Try move left
            Assert.False(moved);
            Assert.Equal(originalX, _player1.X);
            Assert.Equal(originalY, _player1.Y);
        }

        [Fact]
        public void Player_Can_Move_Onto_Bomb_Tile()
        {
            int bombX = 2;
            int bombY = 1;
            // --- FIX: Added missing blastRadius argument (using default 1) ---
            _session.AddBomb("otherPlayer", bombX, bombY, 1);
            // --- END FIX ---
            Assert.Equal(TileType.Bomb, _session.Map[bombX, bombY]);

            bool moved = _manager.MovePlayer(_player1.Id, bombX, bombY); // Try move onto bomb

            Assert.True(moved); // Should succeed based on current logic
            Assert.Equal(bombX, _player1.X);
            Assert.Equal(bombY, _player1.Y);
            Assert.Equal(TileType.Player, _session.Map[bombX, bombY]); // Player visually overwrites bomb tile
        }
    }
}