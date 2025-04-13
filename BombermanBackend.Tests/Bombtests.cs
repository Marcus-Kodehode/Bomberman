using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;

namespace BombermanBackend.Tests
{
    public class BombTests
    {
        private readonly GameSession _session;
        private readonly GameManager _manager;
        private readonly Player _player1;

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
        public void PlaceBomb_Succeeds_When_Under_Limit()
        {
            // Assumes player limit is default 1, active is 0
            bool placed = _manager.PlaceBomb(_player1.Id, _player1.X, _player1.Y);

            Assert.True(placed);
            Assert.Single(_session.Bombs);
            Assert.Equal(1, _player1.ActiveBombsCount); // Check player count updated
            Assert.Equal(TileType.Bomb, _session.Map[_player1.X, _player1.Y]);
        }

        [Fact]
        public void PlaceBomb_Fails_When_Over_Limit()
        {
            _player1.MaxBombs = 1; // Explicitly set limit
            // Place first bomb successfully
            bool placed1 = _manager.PlaceBomb(_player1.Id, _player1.X, _player1.Y);
            Assert.True(placed1);
            Assert.Single(_session.Bombs);
            Assert.Equal(1, _player1.ActiveBombsCount);

            // Try to place second bomb - should fail
            // Need to move player slightly to place at new coords, or assume placing at same coords is allowed if under limit
            _manager.MovePlayer(_player1.Id, _player1.X + 1, _player1.Y); // Move player first
            bool placed2 = _manager.PlaceBomb(_player1.Id, _player1.X, _player1.Y); // Try place second bomb

            Assert.False(placed2); // Should fail due to limit
            Assert.Single(_session.Bombs); // Still only one bomb
            Assert.Equal(1, _player1.ActiveBombsCount); // Count still 1
        }

        [Fact]
        public void PlaceBomb_Fails_If_Player_Not_Found()
        {
            bool placed = _manager.PlaceBomb("nonexistent_player", 1, 1);
            Assert.False(placed);
        }

        [Fact]
        public void Tick_Decrements_Bomb_Fuse()
        {
            _manager.PlaceBomb(_player1.Id, _player1.X, _player1.Y);
            var bomb = _session.Bombs.First();
            int initialTicks = bomb.RemainingFuseTicks;

            _manager.Tick();

            Assert.Single(_session.Bombs);
            Assert.Equal(initialTicks - 1, bomb.RemainingFuseTicks);
        }

        [Fact]
        public void Bomb_Detonates_And_Is_Removed_And_Player_Count_Decremented()
        {
            int bombX = _player1.X;
            int bombY = _player1.Y;
            _manager.PlaceBomb(_player1.Id, bombX, bombY); // Places bomb, ActiveBombsCount becomes 1
            Assert.Equal(1, _player1.ActiveBombsCount);
            var bomb = _session.Bombs.First();
            int fuse = bomb.RemainingFuseTicks;

            for (int i = 0; i < fuse; i++)
            { // Tick exactly fuse times
                _manager.Tick();
            }

            Assert.Empty(_session.Bombs); // Bomb removed
            Assert.Equal(TileType.Empty, _session.Map[bombX, bombY]); // Tile cleared
            Assert.Equal(0, _player1.ActiveBombsCount); // Player active count decremented
        }

        [Fact]
        public void Can_Place_New_Bomb_After_First_One_Detonates()
        {
            // Place first bomb
            bool placed1 = _manager.PlaceBomb(_player1.Id, _player1.X, _player1.Y);
            Assert.True(placed1);
            Assert.Equal(1, _player1.ActiveBombsCount);
            int fuse = _session.Bombs.First().RemainingFuseTicks;

            // Detonate first bomb
            for (int i = 0; i < fuse; i++)
            {
                _manager.Tick();
            }
            Assert.Empty(_session.Bombs);
            Assert.Equal(0, _player1.ActiveBombsCount); // Count is 0 again

            // Move player slightly (needed as PlaceBomb checks coords match player)
            _manager.MovePlayer(_player1.Id, _player1.X + 1, _player1.Y);

            // Place second bomb - should succeed now
            bool placed2 = _manager.PlaceBomb(_player1.Id, _player1.X, _player1.Y);
            Assert.True(placed2);
            Assert.Single(_session.Bombs);
            Assert.Equal(1, _player1.ActiveBombsCount);
        }
    }
}