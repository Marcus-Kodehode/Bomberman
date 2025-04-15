using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BombermanBackend.Logic;
using BombermanBackend.Models;
using BombermanBackend.Services;
using System; // Kept for Console.WriteLine in placeholder

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<GameSession>(sp =>
        {
            // TODO: Load map dimensions from configuration
            const int mapWidth = 9;
            const int mapHeight = 9;
            var session = new GameSession(mapWidth, mapHeight);

            // --- Placeholder: Create an empty map with borders ---
            // Replace this with your actual map loading/generation
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
            // Consider adding some initial DestructibleWall or Powerups via generation here

            return session;
        });

        services.AddSingleton<GameManager>();
        services.AddSingleton<MapPrinter>();
        services.AddSingleton<NatsService>();
        services.AddHostedService(sp => sp.GetRequiredService<NatsService>());
        services.AddLogging(configure => configure.AddConsole());
    })
    .Build();

// Consider replacing Console.WriteLine with proper logging
Console.WriteLine("Bomberman Backend Service starting...");
await host.RunAsync();
Console.WriteLine("Bomberman Backend Service stopped.");