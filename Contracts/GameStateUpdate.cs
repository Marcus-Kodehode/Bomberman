using System.Collections.Generic;

namespace BombermanBackend.Contracts // Use the namespace you decided on
{
    // A comprehensive snapshot of the game state
    public class GameStateUpdate
    {
        // Represent the map efficiently, e.g., as a 1D array or string
        // using integers corresponding to TileType enum values
        public List<int> MapTiles { get; set; } = new();
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }

        public List<PlayerStateDto> Players { get; set; } = new();
        public List<BombStateDto> Bombs { get; set; } = new();
        public string GamePhase { get; set; } = "Waiting"; // e.g., "Waiting", "Running", "Ended"
        public int TickCount { get; set; } // Optional: useful for debugging/sync
    }
}