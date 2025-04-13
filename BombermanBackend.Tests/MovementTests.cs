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

        // Constructor now calls SetupTest
        public MovementTests()
        {
            SetupTest(); // Initializes fields via helper
        }

        // Setup helper method
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
            _session.AddPlayer(_player1); // Adds player and sets initial tile to Player
        }

        [Fact]
        public void Player_Can_Move_Into_Empty_Space()
        {
            int startX = _player1.X;
            int startY = _player1.Y;
            int targetX = startX + 1;
            int targetY = startY;
            Assert.Equal(TileType.Empty, _session.Map[targetX, targetY]); // Ensure target is empty

            bool moved = _manager.MovePlayer(_player1.Id, targetX, targetY);

            Assert.True(moved);
            Assert.Equal(targetX, _player1.X);
            Assert.Equal(targetY, _player1.Y);
            Assert.Equal(TileType.Player, _session.Map[targetX, targetY]); // New tile is player
            Assert.Equal(TileType.Empty, _session.Map[startX, startY]);   // Old tile is empty
        }

        [Fact]
        public void Player_Cannot_Move_Into_Wall()
        {
            _session.Map[2, 1] = TileType.Wall; // Place wall to the right
            int originalX = _player1.X;
            int originalY = _player1.Y;

            bool moved = _manager.MovePlayer(_player1.Id, originalX + 1, originalY); // Try move right

            Assert.False(moved);
            Assert.Equal(originalX, _player1.X); // Position shouldn't change
            Assert.Equal(originalY, _player1.Y);
            Assert.Equal(TileType.Player, _session.Map[originalX, originalY]); // Original tile still player
        }

        [Fact]
        public void Player_Cannot_Move_Into_DestructibleWall()
        {
            _session.Map[2, 1] = TileType.DestructibleWall; // Place destructible wall to the right
            int originalX = _player1.X;
            int originalY = _player1.Y;

            bool moved = _manager.MovePlayer(_player1.Id, originalX + 1, originalY); // Try move right

            Assert.False(moved); // Player cannot move into destructible wall
            Assert.Equal(originalX, _player1.X); // Position shouldn't change
            Assert.Equal(originalY, _player1.Y);
            Assert.Equal(TileType.Player, _session.Map[originalX, originalY]); // Original tile still player
            Assert.Equal(TileType.DestructibleWall, _session.Map[2, 1]); // Wall should still be there
        }


        [Fact]
        public void Player_Cannot_Move_Out_Of_Bounds()
        {
            int originalX = _player1.X; // At (1,1) near top-left border
            int originalY = _player1.Y;

            // Try moving outside bounds in each direction
            bool movedLeft = _manager.MovePlayer(_player1.Id, originalX - 1, originalY); // To (0,1) -> Wall
            bool movedUp = _manager.MovePlayer(_player1.Id, originalX, originalY - 1);   // To (1,0) -> Wall

            // Also test far out of bounds directly (handled by coordinate checks)
            bool movedFar = _manager.MovePlayer(_player1.Id, -5, -5);


            Assert.False(movedLeft);
            Assert.False(movedUp);
            Assert.False(movedFar);
            Assert.Equal(originalX, _player1.X); // Position shouldn't change
            Assert.Equal(originalY, _player1.Y);
        }

        [Fact]
        public void Player_Cannot_Move_Diagonally()
        {
            int originalX = _player1.X;
            int originalY = _player1.Y;

            bool moved = _manager.MovePlayer(_player1.Id, originalX + 1, originalY + 1); // Try move diagonally

            Assert.False(moved);
            Assert.Equal(originalX, _player1.X); // Position shouldn't change
            Assert.Equal(originalY, _player1.Y);
        }

        [Fact]
        public void Player_Cannot_Move_More_Than_One_Tile()
        {
            int originalX = _player1.X;
            int originalY = _player1.Y;

            bool moved = _manager.MovePlayer(_player1.Id, originalX + 2, originalY); // Try move 2 tiles right

            Assert.False(moved);
            Assert.Equal(originalX, _player1.X); // Position shouldn't change
            Assert.Equal(originalY, _player1.Y);
        }


        [Fact]
        public void Player_Can_Move_Onto_Bomb_Tile()
        {
            int bombX = 2;
            int bombY = 1;
            // Need to use the player's actual blast radius when adding the bomb
            _session.AddBomb("otherPlayer", bombX, bombY, 1); // Assuming default blast radius of 1 for the other player
            Assert.Equal(TileType.Bomb, _session.Map[bombX, bombY]);

            bool moved = _manager.MovePlayer(_player1.Id, bombX, bombY); // Try move onto bomb

            Assert.True(moved); // Should succeed based on current logic
            Assert.Equal(bombX, _player1.X);
            Assert.Equal(bombY, _player1.Y);
            Assert.Equal(TileType.Player, _session.Map[bombX, bombY]); // Player visually overwrites bomb tile
            Assert.Equal(TileType.Empty, _session.Map[1, 1]); // Original tile empty
        }

        // --- NEW MULTIPLAYER TEST ---
        [Fact]
        public void Player_Cannot_Move_Into_Another_Player()
        {
            // Arrange: Add a second player next to player 1
            Player player2 = new Player { Id = "p2", X = 2, Y = 1 };
            _session.AddPlayer(player2); // Adds player 2 and sets map[2,1] to Player
            Assert.Equal(TileType.Player, _session.Map[2, 1]);

            int originalX_P1 = _player1.X;
            int originalY_P1 = _player1.Y;

            // Act: Try to move player 1 into player 2's spot
            bool moved = _manager.MovePlayer(_player1.Id, 2, 1);

            // Assert
            Assert.False(moved); // Movement should fail
            Assert.Equal(originalX_P1, _player1.X); // Player 1 position unchanged
            Assert.Equal(originalY_P1, _player1.Y);
            Assert.Equal(2, player2.X); // Player 2 position unchanged
            Assert.Equal(1, player2.Y);
            Assert.Equal(TileType.Player, _session.Map[originalX_P1, originalY_P1]); // P1 still on original tile
            Assert.Equal(TileType.Player, _session.Map[2, 1]); // P2 still on their tile
        }
        // --- END NEW TEST ---
    }
}