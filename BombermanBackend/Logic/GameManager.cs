using System;
using System.Linq;
using System.Collections.Generic;
using BombermanBackend.Models;

namespace BombermanBackend.Logic
{
    public class GameManager
    {
        public GameSession Session;

        public GameManager(GameSession session)
        {
            Session = session;
        }

        public bool MovePlayer(string playerId, string direction)
        {
            if (!Session.Players.ContainsKey(playerId)) return false;
            var player = Session.Players[playerId];
            if (!player.IsAlive) return false;

            int newX = player.X, newY = player.Y;

            switch (direction.ToLower())
            {
                case "up": newY--; break;
                case "down": newY++; break;
                case "left": newX--; break;
                case "right": newX++; break;
            }

            if (IsWalkable(newX, newY))
            {
                player.X = newX;
                player.Y = newY;
                return true;
            }

            return false;
        }

        private bool IsWalkable(int x, int y)
        {
            return x >= 0 && y >= 0 &&
                   x < Session.Map.GetLength(0) &&
                   y < Session.Map.GetLength(1) &&
                   Session.Map[x, y] == TileType.Empty;
        }

        public bool PlaceBomb(string playerId)
        {
            if (!Session.Players.ContainsKey(playerId)) return false;

            var player = Session.Players[playerId];
            return Session.AddBomb(playerId, player.X, player.Y);
        }
    }
}
