using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BombermanBackend.Models;

namespace BombermanBackend.Logic
{
    public class GameSessionManager
    {
        // Using ConcurrentDictionary for basic thread safety on add/remove/get
        private readonly ConcurrentDictionary<string, (GameSession Session, GameManager Manager)> _activeSessions = new();
        private readonly IServiceProvider _serviceProvider; // Needed if GameManager has other dependencies

        // TODO: Inject ILoggerFactory if GameManager needs logging
        public GameSessionManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider; // Or directly inject factories/dependencies needed by Session/Manager
        }

        public string CreateNewSession()
        {
            string gameId = Guid.NewGuid().ToString("N"); // Generate a unique ID

            // TODO: Implement proper map loading/generation here instead of placeholder
            const int mapWidth = 9;
            const int mapHeight = 9;
            var session = new GameSession(mapWidth, mapHeight);
            // Placeholder: empty map with borders
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    if (x == 0 || x == mapWidth - 1 || y == 0 || y == mapHeight - 1)
                    {
                        session.Map[x, y] = TileType.Wall;
                    }
                    else
                    {
                        session.Map[x, y] = TileType.Empty;
                    }
                }
            }

            // Create a GameManager SPECIFICALLY for this new session
            // If GameManager has dependencies injected via constructor, resolve them here
            // using _serviceProvider or pass them into the constructor.
            var manager = new GameManager(session);

            if (_activeSessions.TryAdd(gameId, (session, manager)))
            {
                // TODO: Log session creation
                return gameId;
            }
            else
            {
                // TODO: Log error, handle unlikely ID collision or add failure
                throw new Exception("Failed to create and add new game session.");
            }
        }

        public GameManager? GetManagerForSession(string gameId)
        {
            if (_activeSessions.TryGetValue(gameId, out var gameTuple))
            {
                return gameTuple.Manager;
            }
            return null;
        }

        public GameSession? GetSession(string gameId)
        {
            if (_activeSessions.TryGetValue(gameId, out var gameTuple))
            {
                return gameTuple.Session;
            }
            return null;
        }

        public bool RemoveSession(string gameId)
        {
            if (_activeSessions.TryRemove(gameId, out _))
            {
                // TODO: Log session removal
                return true;
            }
            return false;
        }

        public IEnumerable<string> GetActiveSessionIds()
        {
            return _activeSessions.Keys.ToList(); // Return a copy
        }

        // TODO: Add method to run Tick() for all active games, maybe in parallel?
        public void TickAllSessions()
        {
            // Be mindful of long-running ticks potentially blocking here if not careful
            // Parallel.ForEach(_activeSessions.Values, gameTuple => gameTuple.Manager.Tick());
            foreach (var gameTuple in _activeSessions.Values)
            {
                gameTuple.Manager.Tick();
            }
        }
    }
}