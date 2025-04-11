using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;

namespace BombermanBackend.Tests
{
    public class BombTests
    {
        private readonly GameSession _session; // Use readonly
        private readonly GameManager _manager; // Use readonly
        private readonly Player _player1;    // Use readonly

        // Constructor initializes fields
        public BombTests()
        {
            int width = 7;
            int height = 7;
            _session = new GameSession(width, height);
            _manager = new GameManager(_session);
            _player1 = new Player { Id = "p1", X = 3, Y = 3 };
            _session.AddPlayer(_player1);
        }

        [Fact]
        public void Bomb_Is_Placed_At_Player_Position_With_Correct_Defaults()
        {
            int initialX = _player1.X;
            int initialY = _player1.Y;

            _manager.PlaceBomb(_player1.Id, initialX, initialY);

            Assert.Single(_session.Bombs);
            var bomb = _session.Bombs.First();
            Assert.Equal(_player1.Id, bomb.OwnerId);
            Assert.Equal(initialX, bomb.X);
            Assert.Equal(initialY, bomb.Y);
            Assert.Equal(5, bomb.RemainingFuseTicks);
            Assert.Equal(1, bomb.BlastRadius);
            Assert.Equal(TileType.Bomb, _session.Map[initialX, initialY]);
        }

        [Fact]
        public void Placing_Bomb_On_Existing_Bomb_Tile_Adds_Second_Bomb_Object()
        {
            int targetX = _player1.X;
            int targetY = _player1.Y;

            _manager.PlaceBomb(_player1.Id, targetX, targetY); // First bomb
            _manager.PlaceBomb(_player1.Id, targetX, targetY); // Second bomb

            Assert.Equal(2, _session.Bombs.Count);
            Assert.Equal(TileType.Bomb, _session.Map[targetX, targetY]);
            Assert.All(_session.Bombs, b => Assert.Equal(targetX, b.X));
            Assert.All(_session.Bombs, b => Assert.Equal(targetY, b.Y));
        }

        [Fact]
        public void Tick_Decrements_Bomb_Fuse()
        {
            _manager.PlaceBomb(_player1.Id, _player1.X, _player1.Y);
            var bomb = _session.Bombs.First();
            int initialTicks = bomb.RemainingFuseTicks;

            _manager.Tick(); // Tick 1

            Assert.Single(_session.Bombs);
            Assert.Equal(initialTicks - 1, bomb.RemainingFuseTicks);

            _manager.Tick(); // Tick 2
            Assert.Equal(initialTicks - 2, bomb.RemainingFuseTicks);
        }

        [Fact]
        public void Bomb_Detonates_And_Is_Removed_After_Correct_Ticks()
        {
            int bombX = _player1.X;
            int bombY = _player1.Y;
            _manager.PlaceBomb(_player1.Id, bombX, bombY);
            var bomb = _session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks; // Should be 5

            for (int i = 0; i < fuse - 1; i++)
            {
                _manager.Tick();
                Assert.Single(_session.Bombs);
                Assert.Equal(fuse - 1 - i, bomb.RemainingFuseTicks);
            }

            Assert.Equal(1, bomb.RemainingFuseTicks);
            Assert.Equal(TileType.Bomb, _session.Map[bombX, bombY]);

            _manager.Tick(); // Final tick

            Assert.Empty(_session.Bombs);
            Assert.Equal(TileType.Empty, _session.Map[bombX, bombY]);
        }
    }
}