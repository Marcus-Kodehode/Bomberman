using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;
using System; // For Console.WriteLine

namespace BombermanBackend.Tests
{
    public class BombTests
    {
        private void AdvanceTicks(GameManager manager, int ticks)
        {
            for (int i = 0; i < ticks; i++) { manager.Tick(); }
        }

        // Helper for multi-step cardinal move
        private bool MovePlayerTo(GameManager manager, Player player, int targetX, int targetY)
        {
            while (player.X != targetX)
            {
                int dx = Math.Sign(targetX - player.X);
                if (!manager.MovePlayer(player.Id, player.X + dx, player.Y)) return false;
            }
            while (player.Y != targetY)
            {
                int dy = Math.Sign(targetY - player.Y);
                if (!manager.MovePlayer(player.Id, player.X, player.Y + dy)) return false;
            }
            return (player.X == targetX && player.Y == targetY);
        }


        [Fact]
        public void PlaceBomb_Succeeds_When_Under_Limit() { /* ... No change ... */ }

        [Fact]
        public void PlaceBomb_Fails_When_Over_Limit() { /* ... No change ... */ }

        [Fact]
        public void PlaceBomb_Fails_If_Player_Not_Found() { /* ... No change ... */ }

        [Fact]
        public void Tick_Decrements_Bomb_Fuse() { /* ... No change ... */ }

        [Fact]
        public void Bomb_Detonates_And_Is_Removed_And_Player_Count_Decremented() { /* ... No change ... */ }

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
            bool moved1 = MovePlayerTo(manager, player1, 1, 1); // Move away to safe (1,1)
            Assert.True(moved1, "Setup: Failed to move player away from first bomb");
            Assert.Equal(1, player1.ActiveBombsCount);

            var bomb1 = session.Bombs.FirstOrDefault();
            Assert.NotNull(bomb1);
            int fuse = bomb1.RemainingFuseTicks;

            AdvanceTicks(manager, fuse); // Detonate first bomb
            AdvanceTicks(manager, 1); // Tick once more for CleanupExplosions

            Assert.Empty(session.Bombs);
            Assert.Equal(0, player1.ActiveBombsCount);

            // Act
            // --- ADDED DEBUG LOGGING ---
            int targetX = player1.X;
            int targetY = player1.Y + 1; // Target is (1,2)
            Console.WriteLine($"TEST_DEBUG: Before Move2: Player '{player1.Id}' exists in session? {session.Players.ContainsKey(player1.Id)}. Current Pos: ({player1.X},{player1.Y}). Target Pos: ({targetX},{targetY}). Target TileType: {session.Map[targetX, targetY]}");
            // --- END DEBUG LOGGING ---
            bool moved2 = manager.MovePlayer(player1.Id, targetX, targetY); // Move D to (1,2)

            // Assert Move Success
            Assert.True(moved2, "Move failed after explosion cleanup"); // <--- Failing Assertion

            // Continue if move succeeded
            bool placed2 = manager.PlaceBomb(player1.Id, player1.X, player1.Y); // Place second bomb at (1,2)
            Assert.True(placed2, "Placing second bomb failed");
            Assert.Single(session.Bombs);
            Assert.Equal(1, player1.ActiveBombsCount);
        }
    }
}