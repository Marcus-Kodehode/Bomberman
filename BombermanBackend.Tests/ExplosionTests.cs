using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;
using System.Collections.Generic;

namespace BombermanBackend.Tests
{
    public class ExplosionTests
    {
        // Removed class fields and constructor/SetupTest

        // Helper remains for convenience
        private void AdvanceTicks(GameManager manager, int ticks)
        {
            for (int i = 0; i < ticks; i++) { manager.Tick(); }
        }

        // Helper to place bomb at standard test location (3,3)
        private Bomb PlaceBombAtCenter(GameManager manager, GameSession session, Player player)
        {
            bool moved = manager.MovePlayer(player.Id, 3, 3); // Ensure player is at center
            Assert.True(moved, "Setup: Could not move player to center");
            bool placed = manager.PlaceBomb(player.Id, 3, 3);
            Assert.True(placed, "Setup: Failed to place bomb at center (3,3)");
            return session.Bombs.First(b => b.X == 3 && b.Y == 3);
        }

        [Fact]
        public void Explosion_Clears_Center_Tile()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 1, Y = 1 }; // Start anywhere, will move
            session.AddPlayer(p1);

            var bomb = PlaceBombAtCenter(manager, session, p1); // Place bomb at (3,3)
            int fuse = bomb.RemainingFuseTicks;

            // Act
            AdvanceTicks(manager, fuse);
            AdvanceTicks(manager, 1); // Cleanup

            // Assert
            Assert.Equal(TileType.Empty, session.Map[3, 3]);
        }

        [Fact]
        public void Explosion_Clears_Adjacent_Empty_Tiles()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 1, Y = 1 };
            session.AddPlayer(p1);

            var bomb = PlaceBombAtCenter(manager, session, p1); // Place bomb at (3,3)
            int fuse = bomb.RemainingFuseTicks;
            var adjacentCoords = new[] { (3, 2), (3, 4), (2, 3), (4, 3) };

            // Act
            AdvanceTicks(manager, fuse);
            AdvanceTicks(manager, 1); // Cleanup

            // Assert
            foreach (var (x, y) in adjacentCoords) Assert.Equal(TileType.Empty, session.Map[x, y]);
            Assert.Equal(TileType.Empty, session.Map[bomb.X, bomb.Y]);
        }

        [Fact]
        public void Explosion_Stops_At_Indestructible_Wall()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 1, Y = 1 };
            session.AddPlayer(p1);

            session.Map[4, 3] = TileType.Wall; // Wall to the right of (3,3)
            session.Map[5, 3] = TileType.Empty; // Empty tile beyond the wall

            var bomb = PlaceBombAtCenter(manager, session, p1); // Place bomb at (3,3)
            int fuse = bomb.RemainingFuseTicks;

            // Act
            AdvanceTicks(manager, fuse);
            AdvanceTicks(manager, 1); // Cleanup

            // Assert
            Assert.Equal(TileType.Wall, session.Map[4, 3]);
            Assert.Equal(TileType.Empty, session.Map[5, 3]);
        }

        [Fact]
        public void Explosion_Destroys_Destructible_Wall_And_Stops()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 1, Y = 1 };
            session.AddPlayer(p1);

            session.Map[4, 3] = TileType.DestructibleWall; // Destructible wall to the right
            session.Map[5, 3] = TileType.Empty; // Empty tile beyond

            var bomb = PlaceBombAtCenter(manager, session, p1); // Place bomb at (3,3)
            int fuse = bomb.RemainingFuseTicks;

            // Act
            AdvanceTicks(manager, fuse - 1); // Before explosion
            Assert.Equal(TileType.DestructibleWall, session.Map[4, 3]);

            manager.Tick(); // Detonation tick

            // Assert middle state
            Assert.Equal(TileType.Explosion, session.Map[4, 3]); // Is explosion tile now
            Assert.Equal(TileType.Empty, session.Map[5, 3]); // Beyond is untouched

            AdvanceTicks(manager, 1); // Cleanup tick

            // Assert final state
            Assert.Equal(TileType.Empty, session.Map[4, 3]); // Is now empty
            Assert.Equal(TileType.Empty, session.Map[5, 3]); // Beyond remains empty
        }

        [Fact]
        public void Explosion_Hits_And_Removes_Player()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 1, Y = 1 }; // Bomber
            session.AddPlayer(p1);
            Player p2 = new Player { Id = "p2", X = 4, Y = 3 }; // Target player
            session.AddPlayer(p2);
            Assert.Contains(p2.Id, session.Players.Keys);

            var bomb = PlaceBombAtCenter(manager, session, p1); // Bomb at (3,3)
            int fuse = bomb.RemainingFuseTicks;

            // Move bomber P1 out of the way before detonation
            bool moved = manager.MovePlayer(p1.Id, 1, 1);
            Assert.True(moved, "Failed to move P1 away before detonation");

            // Act
            AdvanceTicks(manager, fuse); // Detonate

            // Assert
            Assert.False(session.Players.ContainsKey("p2")); // p2 removed
            Assert.True(session.Players.ContainsKey("p1")); // p1 (bomber) should survive
            Assert.Equal(TileType.Explosion, session.Map[4, 3]); // Tile shows explosion

            AdvanceTicks(manager, 1); // Cleanup
            Assert.Equal(TileType.Empty, session.Map[4, 3]); // Tile cleared
        }

        [Fact]
        public void Explosion_Triggers_Chain_Reaction()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 1, Y = 1, MaxBombs = 2 }; // Start safe, allow 2 bombs
            session.AddPlayer(p1);

            // Manually place bombs to control fuse/position
            int bombAX = 3, bombAY = 3;
            int bombBX = 4, bombBY = 3;
            Bomb bombA = new Bomb(p1.Id, bombAX, bombAY, 3, 1); // Short fuse
            Bomb bombB = new Bomb(p1.Id, bombBX, bombBY, 5, 1); // Longer fuse
            session.Bombs.Add(bombA); session.Map[bombAX, bombAY] = TileType.Bomb;
            session.Bombs.Add(bombB); session.Map[bombBX, bombBY] = TileType.Bomb;
            p1.ActiveBombsCount = 2; // Manually set count

            // Act
            AdvanceTicks(manager, 3); // Detonates Bomb A, hits B, B detonates in same tick

            // Assert
            Assert.Empty(session.Bombs);
            Assert.Equal(TileType.Explosion, session.Map[2, 3]);
            Assert.Equal(TileType.Explosion, session.Map[3, 3]);
            Assert.Equal(TileType.Explosion, session.Map[4, 3]);
            Assert.Equal(TileType.Explosion, session.Map[5, 3]);
            Assert.Equal(0, p1.ActiveBombsCount); // Count decremented twice

            AdvanceTicks(manager, 1); // Cleanup tick

            Assert.Equal(TileType.Empty, session.Map[2, 3]);
            Assert.Equal(TileType.Empty, session.Map[3, 3]);
            Assert.Equal(TileType.Empty, session.Map[4, 3]);
            Assert.Equal(TileType.Empty, session.Map[5, 3]);
        }

        [Fact]
        public void Explosion_Tiles_Are_Cleaned_Up_Next_Tick()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 1, Y = 1 };
            session.AddPlayer(p1);

            var bomb = PlaceBombAtCenter(manager, session, p1); // Bomb at (3,3)
            int fuse = bomb.RemainingFuseTicks;
            var affectedCoords = new[] { (3, 3), (3, 2), (3, 4), (2, 3), (4, 3) };

            // Act
            AdvanceTicks(manager, fuse); // Detonation tick

            // Assert explosion state
            foreach (var (x, y) in affectedCoords)
                if (session.Map[x, y] != TileType.Wall)
                    Assert.Equal(TileType.Explosion, session.Map[x, y]);

            AdvanceTicks(manager, 1); // Cleanup tick

            // Assert cleaned up state
            foreach (var (x, y) in affectedCoords)
                if (session.Map[x, y] != TileType.Wall)
                    Assert.Equal(TileType.Empty, session.Map[x, y]);
        }
    }
}