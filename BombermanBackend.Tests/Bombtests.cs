using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;
using System.Linq;

namespace BombermanBackend.Tests
{
    public class BombTests
    {
        [Fact]
        public void Bomb_Is_Placed_At_Player_Position()
        {
            var session = new GameSession(11, 11);
            session.AddPlayer("p1", 3, 3);
            var manager = new GameManager(session);

            var placed = manager.PlaceBomb("p1");

            Assert.True(placed);
            Assert.Single(session.Bombs);

            var bomb = session.Bombs.First();
            Assert.Equal(3, bomb.X);
            Assert.Equal(3, bomb.Y);
            Assert.Equal("p1", bomb.OwnerId);
            Assert.Equal(TileType.Bomb, session.Map[3, 3]);
        }

        [Fact]
        public void Cannot_Place_Bomb_On_Existing_Bomb()
        {
            var session = new GameSession(11, 11);
            session.AddPlayer("p1", 2, 2);
            var manager = new GameManager(session);

            var first = manager.PlaceBomb("p1");
            var second = manager.PlaceBomb("p1");

            Assert.True(first);
            Assert.False(second);
            Assert.Single(session.Bombs);
        }

        [Fact]
        public void PlaceBomb_Fails_When_Player_Not_Found()
        {
            var session = new GameSession(11, 11);
            var manager = new GameManager(session);

            var result = manager.PlaceBomb("ghost");

            Assert.False(result);
            Assert.Empty(session.Bombs);
        }
    }
}
