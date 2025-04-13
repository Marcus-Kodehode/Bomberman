using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;

namespace BombermanBackend.Tests
{
    public class BombTests
    {
        // Removed class fields, setup done per test

        // Helper remains for convenience within tests
        private void AdvanceTicks(GameManager manager, int ticks)
        {
            for (int i = 0; i < ticks; i++) { manager.Tick(); }
        }

        [Fact]
        public void PlaceBomb_Succeeds_When_Under_Limit()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3, MaxBombs = 1 };
            session.AddPlayer(player1);

            // Act
            bool placed = manager.PlaceBomb(player1.Id, player1.X, player1.Y);

            // Assert
            Assert.True(placed);
            Assert.Single(session.Bombs);
            Assert.Equal(1, player1.ActiveBombsCount);
            Assert.Equal(TileType.Bomb, session.Map[player1.X, player1.Y]);
        }

        [Fact]
        public void PlaceBomb_Fails_When_Over_Limit()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3, MaxBombs = 1 };
            session.AddPlayer(player1);

            bool placed1 = manager.PlaceBomb(player1.Id, player1.X, player1.Y); // Place first
            Assert.True(placed1);
            manager.MovePlayer(player1.Id, player1.X + 1, player1.Y); // Move player

            // Act
            bool placed2 = manager.PlaceBomb(player1.Id, player1.X, player1.Y); // Try place second

            // Assert
            Assert.False(placed2);
            Assert.Single(session.Bombs);
            Assert.Equal(1, player1.ActiveBombsCount);
        }

        [Fact]
        public void PlaceBomb_Fails_If_Player_Not_Found()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);

            // Act
            bool placed = manager.PlaceBomb("nonexistent_player", 1, 1);

            // Assert
            Assert.False(placed);
        }

        [Fact]
        public void Tick_Decrements_Bomb_Fuse()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3 };
            session.AddPlayer(player1);
            manager.PlaceBomb(player1.Id, player1.X, player1.Y);
            var bomb = session.Bombs.First();
            int initialTicks = bomb.RemainingFuseTicks;

            // Act
            AdvanceTicks(manager, 1);

            // Assert
            Assert.Single(session.Bombs);
            Assert.Equal(initialTicks - 1, bomb.RemainingFuseTicks);
        }

        [Fact]
        public void Bomb_Detonates_And_Is_Removed_And_Player_Count_Decremented()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3 };
            session.AddPlayer(player1);
            int bombX = player1.X;
            int bombY = player1.Y;

            bool placed = manager.PlaceBomb(player1.Id, bombX, bombY);
            Assert.True(placed, "Setup: Failed to place bomb");
            // --- FIX: Move player to SAFE location ---
            bool moved = manager.MovePlayer(player1.Id, 1, 1); // Move away to (1,1)
            Assert.True(moved, "Setup: Failed to move player away from bomb");
            Assert.Equal(1, player1.ActiveBombsCount);
            // --- End FIX ---

            var bomb = session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks;

            // Act
            AdvanceTicks(manager, fuse); // Tick exactly fuse times to detonate
            AdvanceTicks(manager, 1); // Tick once more for CleanupExplosions

            // Assert
            Assert.Empty(session.Bombs);
            Assert.Equal(TileType.Empty, session.Map[bombX, bombY]);
            Assert.Equal(0, player1.ActiveBombsCount); // Player count should be 0 now
        }

        [Fact]
        public void Can_Place_New_Bomb_After_First_One_Detonates()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3, MaxBombs = 1 };
            session.AddPlayer(player1);
            int bombX = player1.X;
            int bombY = player1.Y;

            bool placed1 = manager.PlaceBomb(player1.Id, bombX, bombY);
            Assert.True(placed1, "Setup: Failed to place first bomb");
            // --- FIX: Move player to SAFE location ---
            bool moved1 = manager.MovePlayer(player1.Id, 1, 1); // Move away to safe (1,1)
            Assert.True(moved1, "Setup: Failed to move player away from first bomb");
            Assert.Equal(1, player1.ActiveBombsCount);
            // --- End FIX ---

            int fuse = session.Bombs.First().RemainingFuseTicks;

            AdvanceTicks(manager, fuse); // Detonate first bomb
            AdvanceTicks(manager, 1); // Tick once more for CleanupExplosions *before* moving again

            Assert.Empty(session.Bombs);
            Assert.Equal(0, player1.ActiveBombsCount); // Verify count is 0

            // Act
            // Player is now at (1,1)
            bool moved2 = manager.MovePlayer(player1.Id, player1.X, player1.Y + 1); // Move D to (1,2)
            bool placed2 = manager.PlaceBomb(player1.Id, player1.X, player1.Y); // Place second bomb at (1,2)

            // Assert
            Assert.True(moved2, "Move failed after explosion cleanup"); // This should pass now
            Assert.True(placed2, "Placing second bomb failed");
            Assert.Single(session.Bombs);
            Assert.Equal(1, player1.ActiveBombsCount); // Count back to 1
        }
    }
}