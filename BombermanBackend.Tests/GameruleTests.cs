using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;
using System; // For Math.Sign

namespace BombermanBackend.Tests
{
    public class GameRulesTests
    {
        // Helper to set up a basic game with two players
        private (GameSession, GameManager, Player, Player) SetupTwoPlayerGame()
        {
            var session = new GameSession(7, 7);
            // Add border walls
            for (int x = 0; x < 7; x++) { session.Map[x, 0] = TileType.Wall; session.Map[x, 6] = TileType.Wall; }
            for (int y = 1; y < 6; y++) { session.Map[0, y] = TileType.Wall; session.Map[6, y] = TileType.Wall; }

            var manager = new GameManager(session);
            var player1 = new Player { Id = "p1", X = 1, Y = 1 };
            var player2 = new Player { Id = "p2", X = 5, Y = 5 };
            session.AddPlayer(player1);
            session.AddPlayer(player2);

            return (session, manager, player1, player2);
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

        private void AdvanceTicks(GameManager manager, int ticks)
        {
            for (int i = 0; i < ticks; i++) { manager.Tick(); }
        }


        [Fact]
        public void Game_Ends_When_One_Player_Remains()
        {
            // Arrange
            var (session, manager, player1, player2) = SetupTwoPlayerGame();
            Assert.Equal(2, session.Players.Count); // Verify initial state

            // Act: Simulate player 2 being hit by an explosion
            // Place a bomb by player 1, move player 1 away, let it explode near player 2
            bool placed = manager.PlaceBomb(player1.Id, player1.X, player1.Y);
            Assert.True(placed, "Setup: P1 failed to place bomb");

            // Move P1 far away
            bool movedP1 = MovePlayerTo(manager, player1, 3, 3);
            Assert.True(movedP1, "Setup: Failed to move P1 away");

            // Move P2 next to the bomb spot (original P1 spot)
            bool movedP2 = MovePlayerTo(manager, player2, 1, 1); // Move P2 to where P1 was
            Assert.True(movedP2, "Setup: Failed to move P2");


            var bomb = session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, fuse); // Bomb detonates, hitting P2

            // Assert
            Assert.Single(session.Players); // Only one player should remain
            Assert.True(session.Players.ContainsKey(player1.Id)); // Player 1 should be the winner
            Assert.False(session.Players.ContainsKey(player2.Id)); // Player 2 should be removed

            // Future: Add a dedicated CheckWinCondition method to GameManager
            // and assert its result here.
            // Assert.Equal(player1.Id, manager.CheckWinCondition());
        }

        // Add more tests for other game rules:
        // - Initial player placement validation
        // - Draw conditions (if any)
        // - Specific interaction rules (e.g., bomb kicking if implemented)
    }
}