using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;
using System.Collections.Generic;

namespace BombermanBackend.Tests
{
    public class ExplosionTests
    {
        // Helper remains for convenience
        private void AdvanceTicks(GameManager manager, int ticks)
        {
            for (int i = 0; i < ticks; i++) { manager.Tick(); }
        }

        // Helper for multi-step cardinal move (Example: Can add error handling)
        private bool MovePlayerTo(GameManager manager, Player player, int targetX, int targetY)
        {
            // Simple pathing for testing - assumes clear path or tests wall collision
            while (player.X != targetX)
            {
                int dx = Math.Sign(targetX - player.X);
                if (!manager.MovePlayer(player.Id, player.X + dx, player.Y)) return false; // Failed move
            }
            while (player.Y != targetY)
            {
                int dy = Math.Sign(targetY - player.Y);
                if (!manager.MovePlayer(player.Id, player.X, player.Y + dy)) return false; // Failed move
            }
            return (player.X == targetX && player.Y == targetY);
        }

        // Removed PlaceBombAtCenter helper

        [Fact]
        public void Explosion_Clears_Center_Tile()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 3, Y = 3 }; // Start at center
            session.AddPlayer(p1);

            // Place bomb directly
            bool placed = manager.PlaceBomb(p1.Id, 3, 3);
            Assert.True(placed);
            var bomb = session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks;

            // Act
            AdvanceTicks(manager, fuse); // Detonate
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
            var p1 = new Player { Id = "p1", X = 3, Y = 3 };
            session.AddPlayer(p1);

            bool placed = manager.PlaceBomb(p1.Id, 3, 3);
            Assert.True(placed);
            var bomb = session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks;
            var adjacentCoords = new[] { (3, 2), (3, 4), (2, 3), (4, 3) };

            // Ensure adjacent are empty before
            foreach (var (x, y) in adjacentCoords) Assert.Equal(TileType.Empty, session.Map[x, y]);

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
            var p1 = new Player { Id = "p1", X = 3, Y = 3 }; // Start at center
            session.AddPlayer(p1);
            session.Map[4, 3] = TileType.Wall;    // Wall right of bomb
            session.Map[5, 3] = TileType.Empty;   // Empty beyond wall

            bool placed = manager.PlaceBomb(p1.Id, 3, 3);
            Assert.True(placed);
            var bomb = session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks;

            // Act
            AdvanceTicks(manager, fuse);
            AdvanceTicks(manager, 1); // Cleanup

            // Assert
            Assert.Equal(TileType.Wall, session.Map[4, 3]);
            Assert.Equal(TileType.Empty, session.Map[5, 3]);
            Assert.Equal(TileType.Empty, session.Map[2, 3]); // Left should be clear
        }

        [Fact]
        public void Explosion_Destroys_Destructible_Wall_And_Stops()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 3, Y = 3 }; // Start center
            session.AddPlayer(p1);
            session.Map[4, 3] = TileType.DestructibleWall; // Destructible right
            session.Map[5, 3] = TileType.Empty;            // Empty beyond

            bool placed = manager.PlaceBomb(p1.Id, 3, 3);
            Assert.True(placed);
            var bomb = session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks;

            // Act
            AdvanceTicks(manager, fuse - 1); // Before explosion
            Assert.Equal(TileType.DestructibleWall, session.Map[4, 3]);

            manager.Tick(); // Detonation tick

            // Assert intermediate state
            Assert.Equal(TileType.Explosion, session.Map[4, 3]);
            Assert.Equal(TileType.Empty, session.Map[5, 3]);

            AdvanceTicks(manager, 1); // Cleanup tick

            // Assert final state
            Assert.Equal(TileType.Empty, session.Map[4, 3]);
            Assert.Equal(TileType.Empty, session.Map[5, 3]);
        }

        [Fact]
        public void Explosion_Hits_And_Removes_Player()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 3, Y = 3 }; // Bomber starts at center
            session.AddPlayer(p1);
            Player p2 = new Player { Id = "p2", X = 4, Y = 3 }; // Target player right
            session.AddPlayer(p2);
            Assert.Contains(p2.Id, session.Players.Keys);

            bool placed = manager.PlaceBomb(p1.Id, 3, 3); // Bomb at (3,3)
            Assert.True(placed);
            var bomb = session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks;

            // Move bomber P1 out of the way using cardinal moves
            bool moved = MovePlayerTo(manager, p1, 1, 1);
            Assert.True(moved, "Failed to move P1 away before detonation");

            // Act
            AdvanceTicks(manager, fuse); // Detonate

            // Assert
            Assert.False(session.Players.ContainsKey("p2")); // p2 removed
            Assert.True(session.Players.ContainsKey("p1"));  // p1 survived
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
            var p1 = new Player { Id = "p1", X = 1, Y = 1, MaxBombs = 2 }; // Start safe
            session.AddPlayer(p1);

            // Manually place bombs to control fuse/position precisely
            int bombAX = 3, bombAY = 3;
            int bombBX = 4, bombBY = 3;
            Bomb bombA = new Bomb(p1.Id, bombAX, bombAY, 1, 3); // Radius 1, Fuse 3
            Bomb bombB = new Bomb(p1.Id, bombBX, bombBY, 1, 5); // Radius 1, Fuse 5
            session.Bombs.Add(bombA); session.Map[bombAX, bombAY] = TileType.Bomb;
            session.Bombs.Add(bombB); session.Map[bombBX, bombBY] = TileType.Bomb;
            p1.ActiveBombsCount = 2; // Manually set count

            // Act
            AdvanceTicks(manager, 3); // Detonates Bomb A, hits B, B detonates in same tick

            // Assert - Check explosion tiles immediately after detonation tick
            Assert.Empty(session.Bombs);
            Assert.Equal(TileType.Explosion, session.Map[2, 3]); // A left
            Assert.Equal(TileType.Explosion, session.Map[3, 3]); // A center
            Assert.Equal(TileType.Explosion, session.Map[4, 3]); // B center / A right
            Assert.Equal(TileType.Explosion, session.Map[5, 3]); // B right
            // Check other tiles affected by B
            Assert.Equal(TileType.Explosion, session.Map[4, 2]); // B up (assuming not wall)
            Assert.Equal(TileType.Explosion, session.Map[4, 4]); // B down (assuming not wall & clear)

            Assert.Equal(0, p1.ActiveBombsCount); // Count decremented twice

            AdvanceTicks(manager, 1); // Cleanup tick

            // Assert final empty state
            Assert.Equal(TileType.Empty, session.Map[2, 3]);
            Assert.Equal(TileType.Empty, session.Map[3, 3]);
            Assert.Equal(TileType.Empty, session.Map[4, 3]);
            Assert.Equal(TileType.Empty, session.Map[5, 3]);
            Assert.Equal(TileType.Empty, session.Map[4, 2]);
            Assert.Equal(TileType.Empty, session.Map[4, 4]);
        }

        [Fact]
        public void Explosion_Tiles_Are_Cleaned_Up_Next_Tick()
        {
            // Arrange
            var session = new GameSession(7, 7);
            var manager = new GameManager(session);
            var p1 = new Player { Id = "p1", X = 3, Y = 3 }; // Start center
            session.AddPlayer(p1);

            bool placed = manager.PlaceBomb(p1.Id, 3, 3);
            Assert.True(placed);
            var bomb = session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks;
            // Expected affected tiles (radius 1 from 3,3)
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