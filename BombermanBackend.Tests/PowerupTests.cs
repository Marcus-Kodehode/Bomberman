using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System;

namespace BombermanBackend.Tests
{
    public class PowerupTests
    {
        // Helper to set up a basic game state for power-up tests
        private (GameSession, GameManager, Player) SetupGameWithPowerup(TileType powerUpType, int powerUpX, int powerUpY, int playerStartX = 1, int playerStartY = 1)
        {
            var session = new GameSession(7, 7);
            // Add border walls (optional, but good practice)
            for (int x = 0; x < 7; x++) { session.Map[x, 0] = TileType.Wall; session.Map[x, 6] = TileType.Wall; }
            for (int y = 1; y < 6; y++) { session.Map[0, y] = TileType.Wall; session.Map[6, y] = TileType.Wall; }

            // Place the specified power-up
            if (powerUpX >= 0 && powerUpX < 7 && powerUpY >= 0 && powerUpY < 7)
            {
                session.Map[powerUpX, powerUpY] = powerUpType;
            }

            var manager = new GameManager(session);
            var player = new Player { Id = "p1", X = playerStartX, Y = playerStartY }; // Default R=1, MaxB=1
            session.AddPlayer(player); // Adds player and sets initial tile

            return (session, manager, player);
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
        public void Player_Collects_BlastRadius_Powerup_IncreasesStat()
        {
            int powerUpX = 2, powerUpY = 1;
            var (session, manager, player) = SetupGameWithPowerup(TileType.PowerUpBlastRadius, powerUpX, powerUpY);
            int initialRadius = player.BlastRadius;

            bool moved = MovePlayerTo(manager, player, powerUpX, powerUpY); // Move onto powerup

            Assert.True(moved, "Player should be able to move onto the power-up tile.");
            Assert.Equal(initialRadius + 1, player.BlastRadius); // Verify stat increased
            Assert.Equal(powerUpX, player.X); // Verify player position
            Assert.Equal(powerUpY, player.Y);
        }

        [Fact]
        public void Player_Collects_BlastRadius_Powerup_TileBecomesEmpty()
        {
            int powerUpX = 2, powerUpY = 1;
            var (session, manager, player) = SetupGameWithPowerup(TileType.PowerUpBlastRadius, powerUpX, powerUpY);
            int playerStartX = player.X;
            int playerStartY = player.Y;


            bool moved = MovePlayerTo(manager, player, powerUpX, powerUpY); // Move onto powerup

            Assert.True(moved, "Player should be able to move onto the power-up tile.");
            // The player is now ON the tile, so it should be TileType.Player
            Assert.Equal(TileType.Player, session.Map[powerUpX, powerUpY]);
            // The original tile should now be empty
            Assert.Equal(TileType.Empty, session.Map[playerStartX, playerStartY]);
        }


        [Fact]
        public void Player_Collects_BombCount_Powerup_IncreasesStat()
        {
            int powerUpX = 1, powerUpY = 2;
            var (session, manager, player) = SetupGameWithPowerup(TileType.PowerUpBombCount, powerUpX, powerUpY);
            int initialMaxBombs = player.MaxBombs;

            bool moved = MovePlayerTo(manager, player, powerUpX, powerUpY); // Move onto powerup

            Assert.True(moved, "Player should be able to move onto the power-up tile.");
            Assert.Equal(initialMaxBombs + 1, player.MaxBombs); // Verify stat increased
            Assert.Equal(powerUpX, player.X); // Verify player position
            Assert.Equal(powerUpY, player.Y);
        }

        [Fact]
        public void Player_Collects_BombCount_Powerup_TileBecomesEmpty()
        {
            int powerUpX = 1, powerUpY = 2;
            var (session, manager, player) = SetupGameWithPowerup(TileType.PowerUpBombCount, powerUpX, powerUpY);
            int playerStartX = player.X;
            int playerStartY = player.Y;

            bool moved = MovePlayerTo(manager, player, powerUpX, powerUpY); // Move onto powerup

            Assert.True(moved, "Player should be able to move onto the power-up tile.");
            // The player is now ON the tile, so it should be TileType.Player
            Assert.Equal(TileType.Player, session.Map[powerUpX, powerUpY]);
            // The original tile should now be empty
            Assert.Equal(TileType.Empty, session.Map[playerStartX, playerStartY]);
        }
    }
}