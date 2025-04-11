using Xunit;
using BombermanBackend.Models;
using BombermanBackend.Logic;

namespace BombermanBackend.Tests
{
    public class MovementTests
    {
        [Fact]
        public void Player_Cannot_Move_Into_Wall()
        {
            var session = new GameSession(11, 11);
            session.Map[2, 1] = TileType.Wall;
            session.AddPlayer("p1", 1, 1);

            var manager = new GameManager(session);
            var moved = manager.MovePlayer("p1", "right");

            Assert.False(moved);
            Assert.Equal(1, session.Players["p1"].X);
        }
    }
}
