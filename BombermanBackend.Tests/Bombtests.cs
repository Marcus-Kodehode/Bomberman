using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;
using System;

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

        // Tests creating their own state
        [Fact]
        public void PlaceBomb_Succeeds_When_Under_Limit()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3, MaxBombs = 1 }; session.AddPlayer(player1);
            bool placed = manager.PlaceBomb(player1.Id, player1.X, player1.Y);
            Assert.True(placed); Assert.Single(session.Bombs); Assert.Equal(1, player1.ActiveBombsCount); Assert.Equal(TileType.Bomb, session.Map[player1.X, player1.Y]);
        }

        [Fact]
        public void PlaceBomb_Fails_When_Over_Limit()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3, MaxBombs = 1 }; session.AddPlayer(player1);
            bool placed1 = manager.PlaceBomb(player1.Id, player1.X, player1.Y); Assert.True(placed1);
            bool moved = manager.MovePlayer(player1.Id, player1.X + 1, player1.Y); // Move one step R to (4,3)
            Assert.True(moved, "Setup: Failed to move player for over-limit test");
            bool placed2 = manager.PlaceBomb(player1.Id, player1.X, player1.Y); // Try place second at (4,3)
            Assert.False(placed2); Assert.Single(session.Bombs); Assert.Equal(1, player1.ActiveBombsCount);
        }

        [Fact]
        public void PlaceBomb_Fails_If_Player_Not_Found()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            bool placed = manager.PlaceBomb("nonexistent_player", 1, 1);
            Assert.False(placed); Assert.Empty(session.Bombs);
        }

        [Fact]
        public void Tick_Decrements_Bomb_Fuse()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3 }; session.AddPlayer(player1);
            manager.PlaceBomb(player1.Id, player1.X, player1.Y);
            var bomb = session.Bombs.First(); int initialTicks = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, 1);
            Assert.Single(session.Bombs); Assert.Equal(initialTicks - 1, bomb.RemainingFuseTicks);
        }

        [Fact]
        public void Bomb_Detonates_And_Is_Removed_And_Player_Count_Decremented()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3 }; session.AddPlayer(player1);
            int bombX = player1.X; int bombY = player1.Y;
            bool placed = manager.PlaceBomb(player1.Id, bombX, bombY); Assert.True(placed, "Setup: Failed to place bomb");
            bool moved = MovePlayerTo(manager, player1, 1, 1); // Use cardinal helper
            Assert.True(moved, "Setup: Failed to move player away from bomb"); // Check move succeeds
            Assert.Equal(1, player1.ActiveBombsCount);
            var bomb = session.Bombs.FirstOrDefault(); Assert.NotNull(bomb);
            int fuse = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, fuse); AdvanceTicks(manager, 1); // Cleanup tick
            Assert.Empty(session.Bombs); Assert.Equal(TileType.Empty, session.Map[bombX, bombY]); Assert.Equal(0, player1.ActiveBombsCount);
        }

        [Fact]
        public void Can_Place_New_Bomb_After_First_One_Detonates()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 3, Y = 3, MaxBombs = 1 }; session.AddPlayer(player1);
            int bombX = player1.X; int bombY = player1.Y;
            bool placed1 = manager.PlaceBomb(player1.Id, bombX, bombY); Assert.True(placed1, "Setup: Failed to place first bomb");
            bool moved1 = MovePlayerTo(manager, player1, 1, 1); // Use cardinal helper
            Assert.True(moved1, "Setup: Failed to move player away from first bomb"); // Check move succeeds
            Assert.Equal(1, player1.ActiveBombsCount);
            var bomb1 = session.Bombs.FirstOrDefault(); Assert.NotNull(bomb1);
            int fuse = bomb1.RemainingFuseTicks;
            AdvanceTicks(manager, fuse); AdvanceTicks(manager, 1); // Cleanup tick *before* moving again
            Assert.Empty(session.Bombs); Assert.Equal(0, player1.ActiveBombsCount);
            bool moved2 = manager.MovePlayer(player1.Id, 1, 2); // Cardinal move D from (1,1)
            Assert.True(moved2, "Move failed after explosion cleanup");
            bool placed2 = manager.PlaceBomb(player1.Id, player1.X, player1.Y); // Place second bomb at (1,2)
            Assert.True(placed2, "Placing second bomb failed"); Assert.Single(session.Bombs); Assert.Equal(1, player1.ActiveBombsCount);
        }
    }
}