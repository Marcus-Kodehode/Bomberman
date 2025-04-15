using System;

namespace BombermanBackend.Models // Or Models.Events
{
    public class PlayerEventArgs : EventArgs
    {
        public Player Player { get; }

        public PlayerEventArgs(Player player)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
        }
    }
}