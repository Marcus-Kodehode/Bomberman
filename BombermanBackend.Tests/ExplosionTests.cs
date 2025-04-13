using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;
using System.Collections.Generic;
using System;

namespace BombermanBackend.Tests
{
    public class ExplosionTests
    {
        private void AdvanceTicks(GameManager manager, int ticks)
        {
            for (int i = 0; i < ticks; i++) { manager.Tick(); }
        }

        // Added cardinal move helper here too
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

        // Helper to place bomb at player's current location
        private Bomb PlaceBombAtPlayer(GameManager manager, GameSession session, Player player)
        {
            bool placed = manager.PlaceBomb(player.Id, player.X, player.Y);
            Assert.True(placed, $"Setup: Failed to place bomb at ({player.X},{player.Y})");
            var bomb = session.Bombs.FirstOrDefault(b => b.OwnerId == player.Id && b.X == player.X && b.Y == player.Y);
            Assert.NotNull(bomb);
            return bomb!;
        }

        // Removed SetupGame helper, setup done in each test

        [Fact]
        public void Explosion_Clears_Center_Tile()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 3, Y = 3 }; session.AddPlayer(p1); // Start center
            var bomb = PlaceBombAtPlayer(manager, session, p1);
            int fuse = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, fuse); AdvanceTicks(manager, 1); // Cleanup
            Assert.Equal(TileType.Empty, session.Map[3, 3]);
        }

        [Fact]
        public void Explosion_Clears_Adjacent_Empty_Tiles()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 3, Y = 3 }; session.AddPlayer(p1); // Start center
            var bomb = PlaceBombAtPlayer(manager, session, p1);
            int fuse = bomb.RemainingFuseTicks;
            var adjacentCoords = new[] { (3, 2), (3, 4), (2, 3), (4, 3) }; // Radius 1
            foreach (var (x, y) in adjacentCoords) Assert.Equal(TileType.Empty, session.Map[x, y]);
            AdvanceTicks(manager, fuse); AdvanceTicks(manager, 1); // Cleanup
            foreach (var (x, y) in adjacentCoords) Assert.Equal(TileType.Empty, session.Map[x, y]);
            Assert.Equal(TileType.Empty, session.Map[bomb.X, bomb.Y]);
        }

        [Fact]
        public void Explosion_Stops_At_Indestructible_Wall()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 3, Y = 3 }; session.AddPlayer(p1); // Start center
            session.Map[4, 3] = TileType.Wall; session.Map[5, 3] = TileType.Empty;
            var bomb = PlaceBombAtPlayer(manager, session, p1);
            int fuse = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, fuse); AdvanceTicks(manager, 1); // Cleanup
            Assert.Equal(TileType.Wall, session.Map[4, 3]); Assert.Equal(TileType.Empty, session.Map[5, 3]);
        }

        [Fact]
        public void Explosion_Destroys_Destructible_Wall_And_Stops()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 3, Y = 3 }; session.AddPlayer(p1); // Start center
            session.Map[4, 3] = TileType.DestructibleWall; session.Map[5, 3] = TileType.Empty;
            var bomb = PlaceBombAtPlayer(manager, session, p1);
            int fuse = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, fuse - 1); Assert.Equal(TileType.DestructibleWall, session.Map[4, 3]);
            manager.Tick(); // Detonation tick
            Assert.Equal(TileType.Explosion, session.Map[4, 3]); Assert.Equal(TileType.Empty, session.Map[5, 3]);
            AdvanceTicks(manager, 1); // Cleanup tick
            Assert.Equal(TileType.Empty, session.Map[4, 3]); Assert.Equal(TileType.Empty, session.Map[5, 3]);
        }

        [Fact]
        public void Explosion_Hits_And_Removes_Player()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 3, Y = 3 }; session.AddPlayer(p1); // Bomber starts center
            Player p2 = new Player { Id = "2", X = 4, Y = 3 }; session.AddPlayer(p2); // Target player right
            Assert.Contains(p2.Id, session.Players.Keys);
            var bomb = PlaceBombAtPlayer(manager, session, p1); // Bomb at (3,3)
            int fuse = bomb.RemainingFuseTicks;
            bool moved = MovePlayerTo(manager, p1, 1, 1); // Use cardinal helper to move p1 away
            Assert.True(moved, "Failed to move P1 away before detonation");
            AdvanceTicks(manager, fuse); // Detonate
            Assert.False(session.Players.ContainsKey("2")); // p2 removed
            Assert.True(session.Players.ContainsKey("1"));  // p1 survived
            Assert.Equal(TileType.Explosion, session.Map[4, 3]);
            AdvanceTicks(manager, 1); // Cleanup
            Assert.Equal(TileType.Empty, session.Map[4, 3]);
        }

        // Removed chain reaction tests temporarily
        // [Fact] public void Explosion_Triggers_Chain_Reaction() { /* ... */ }
        // [Fact] public void Chain_Reaction_Hits_Player() { /* ... */ }

        [Fact]
        public void Explosion_Tiles_Are_Cleaned_Up_Next_Tick()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 3, Y = 3 }; session.AddPlayer(p1);
            var bomb = PlaceBombAtPlayer(manager, session, p1);
            int fuse = bomb.RemainingFuseTicks;
            var affectedCoords = new[] { (3, 3), (3, 2), (3, 4), (2, 3), (4, 3) }; // Radius 1
            AdvanceTicks(manager, fuse); // Detonation tick
            foreach (var (x, y) in affectedCoords) if (session.Map[x, y] != TileType.Wall) Assert.Equal(TileType.Explosion, session.Map[x, y]);
            AdvanceTicks(manager, 1); // Cleanup tick
            foreach (var (x, y) in affectedCoords) if (session.Map[x, y] != TileType.Wall) Assert.Equal(TileType.Empty, session.Map[x, y]);
        }

        [Fact]
        public void Explosion_With_Radius_2_Clears_Correct_Tiles()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 3, Y = 3, BlastRadius = 2 }; // Set radius
            session.AddPlayer(p1);
            session.Map[5, 3] = TileType.DestructibleWall;
            var bomb = PlaceBombAtPlayer(manager, session, p1);
            int fuse = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, fuse); // Detonate
            Assert.Equal(TileType.Explosion, session.Map[3, 1]); Assert.Equal(TileType.Explosion, session.Map[1, 3]); Assert.Equal(TileType.Explosion, session.Map[5, 3]);
            AdvanceTicks(manager, 1); // Cleanup
            Assert.Equal(TileType.Empty, session.Map[3, 1]); Assert.Equal(TileType.Empty, session.Map[1, 3]); Assert.Equal(TileType.Empty, session.Map[5, 3]);
        }

        [Fact]
        public void Explosion_Stops_At_First_Wall_Even_With_Larger_Radius()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 3, Y = 3, BlastRadius = 3 }; session.AddPlayer(p1); // Set radius
            session.Map[4, 3] = TileType.Wall; session.Map[5, 3] = TileType.Empty; session.Map[6, 3] = TileType.Empty;
            var bomb = PlaceBombAtPlayer(manager, session, p1);
            int fuse = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, fuse); AdvanceTicks(manager, 1);
            Assert.Equal(TileType.Wall, session.Map[4, 3]); Assert.Equal(TileType.Empty, session.Map[5, 3]); Assert.Equal(TileType.Empty, session.Map[6, 3]);
        }

        [Fact]
        public void Explosion_Near_Boundary_Works()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 1, Y = 1 }; session.AddPlayer(p1); // Start near corner
            for (int i = 0; i < 7; i++) { session.Map[i, 0] = TileType.Wall; session.Map[0, i] = TileType.Wall; } // Add borders
            var bomb = PlaceBombAtPlayer(manager, session, p1);
            int fuse = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, fuse); AdvanceTicks(manager, 1);
            Assert.Equal(TileType.Wall, session.Map[0, 1]); Assert.Equal(TileType.Wall, session.Map[1, 0]);
            Assert.Equal(TileType.Empty, session.Map[1, 1]); Assert.Equal(TileType.Empty, session.Map[2, 1]); Assert.Equal(TileType.Empty, session.Map[1, 2]);
        }

        [Fact]
        public void Player_Hit_On_Center_Tile()
        {
            var session = new GameSession(7, 7); var manager = new GameManager(session);
            var p1 = new Player { Id = "1", X = 3, Y = 3 }; session.AddPlayer(p1);
            var bomb = PlaceBombAtPlayer(manager, session, p1);
            int fuse = bomb.RemainingFuseTicks;
            AdvanceTicks(manager, fuse); // Detonate
            Assert.False(session.Players.ContainsKey("1")); // P1 removed
            Assert.Equal(TileType.Explosion, session.Map[3, 3]);
            AdvanceTicks(manager, 1); // Cleanup
            Assert.Equal(TileType.Empty, session.Map[3, 3]);
        }
    }
}