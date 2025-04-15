using System;

namespace BombermanBackend.Models // Or Models.Events
{
    public class GameOverEventArgs : EventArgs
    {
        // WinnerPlayerId can be null if it's a draw
        public string? WinnerPlayerId { get; }

        public GameOverEventArgs(string? winnerPlayerId)
        {
            WinnerPlayerId = winnerPlayerId;
        }
    }
}