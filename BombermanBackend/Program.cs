using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; // Required for ILogger
using BombermanBackend.Logic;
using BombermanBackend.Models;
using BombermanBackend.Services;
using System; // Needed only for EventArgs if not moved

// Define a class name for the logger category if using top-level statements
// This is often implicitly 'Program' but making it explicit can be clearer.
public class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<GameSessionManager>();
                services.AddSingleton<MapPrinter>();
                services.AddSingleton<NatsService>();
                services.AddHostedService(sp => sp.GetRequiredService<NatsService>());
                services.AddLogging(configure => configure.AddConsole()); // Console logger configured here

                // Add the GameSession factory delegate separately for clarity
                services.AddSingleton<GameSession>(sp =>
                {
                    // TODO: Load map dimensions from configuration
                    const int mapWidth = 9;
                    const int mapHeight = 9;
                    var session = new GameSession(mapWidth, mapHeight);

                    // Placeholder: Create an empty map with borders
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
                    // Placeholder: Add some initial items
                    // session.Map[1, 3] = TileType.DestructibleWall;
                    // session.Map[1, 2] = TileType.PowerUpBlastRadius;
                    // session.Map[1, 4] = TileType.PowerUpBombCount;

                    return session;
                });

            })
            .Build();

        // Get the logger service AFTER building the host
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        // Use the logger for startup message
        logger.LogInformation("Bomberman Backend Service starting...");

        await host.RunAsync(); // Runs the application and blocks until shutdown

        // Use the logger for shutdown message (will run after Ctrl+C or shutdown signal)
        logger.LogInformation("Bomberman Backend Service stopped.");
    }
}