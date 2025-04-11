using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;

namespace BombermanBackend.Tests
{
    public class MovementTests
    {
        private readonly GameSession _session; // Use readonly
        private readonly GameManager _manager; // Use readonly
        private readonly Player _player1;    // Use readonly

        // Constructor initializes fields by calling SetupTest
        public MovementTests()
        {
            // Default setup for most tests
            int width = 7;
            int height = 7;
            _session = new GameSession(width, height);
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
            _player1 = new Player { Id = "p1", X = 1, Y = 1 };
            _session.AddPlayer(_player1);
        }


        [Fact]
        public void Player_Can_Move_Into_Empty_Space()
        {
            // Setup already done in constructor
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
            // Setup already done, add specific wall for this test
            _session.Map[2, 1] = TileType.Wall;
            int originalX = _player1.X;
            int originalY = _player1.Y;

            bool moved = _manager.MovePlayer(_player1.Id, originalX + 1, originalY);

            Assert.False(moved);
            Assert.Equal(originalX, _player1.X);
            Assert.Equal(originalY, _player1.Y);
            Assert.Equal(TileType.Player, _session.Map[originalX, originalY]);
            Assert.Equal(TileType.Wall, _session.Map[2, 1]);
        }

        [Fact]
        public void Player_Cannot_Move_Out_Of_Bounds()
        {
            // Setup already done
            int originalX = _player1.X;
            int originalY = _player1.Y;

            bool moved = _manager.MovePlayer(_player1.Id, originalX - 1, originalY); // Try move left off map / into border wall

            Assert.False(moved);
            Assert.Equal(originalX, _player1.X);
            Assert.Equal(originalY, _player1.Y);
        }

        [Fact]
        public void Player_Can_Move_Onto_Bomb_Tile()
        {
            // Setup already done
            int bombX = 2;
            int bombY = 1;
            _session.AddBomb("otherPlayer", bombX, bombY);
            Assert.Equal(TileType.Bomb, _session.Map[bombX, bombY]);

            // This requires GameSession.MovePlayer logic change to pass
            bool moved = _manager.MovePlayer(_player1.Id, bombX, bombY);

            Assert.True(moved); // Should now pass after GameSession fix
            Assert.Equal(bombX, _player1.X);
            Assert.Equal(bombY, _player1.Y);
            Assert.Equal(TileType.Player, _session.Map[bombX, bombY]);
        }
    }
}