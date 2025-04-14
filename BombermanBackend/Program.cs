using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BombermanBackend.Logic;
using BombermanBackend.Models;
using BombermanBackend.Services; // Your new service namespace

// --- Application Entry Point ---

// Use the Generic Host for dependency injection, logging, configuration
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register GameSession as a Singleton - only one game state
        services.AddSingleton<GameSession>(sp =>
        {
            // Initialize GameSession (e.g., fixed size or load from config)
            const int mapWidth = 7; // Or get from configuration
            const int mapHeight = 7;
            var session = new GameSession(mapWidth, mapHeight);

            // TODO: Initialize the map layout here instead of hardcoding
            // This could load from a file or use a generation algorithm
            // For now, just a basic empty map with borders
            for (int x = 0; x < mapWidth; x++) for (int y = 0; y < mapHeight; y++) session.Map[x, y] = TileType.Empty;
            for (int x = 0; x < mapWidth; x++) { session.Map[x, 0] = TileType.Wall; session.Map[x, mapHeight - 1] = TileType.Wall; }
            for (int y = 1; y < mapHeight - 1; y++) { session.Map[0, y] = TileType.Wall; session.Map[mapWidth - 1, y] = TileType.Wall; }
            // Add some obstacles or powerups if needed for initial state
            session.Map[3, 3] = TileType.Wall;
            session.Map[1, 3] = TileType.DestructibleWall;
            session.Map[1, 2] = TileType.PowerUpBlastRadius;
            session.Map[1, 4] = TileType.PowerUpBombCount;


            Console.WriteLine("Initialized GameSession Map."); // Use logging instead later
            return session;
        });

        // Register GameManager as a Singleton - manages the single game session
        services.AddSingleton<GameManager>();

        // Register MapPrinter if needed elsewhere (or just instantiate in GameManager)
        services.AddSingleton<MapPrinter>();

        // Register NatsService as a Singleton and Hosted Service
        // It will connect and start listening automatically
        services.AddSingleton<NatsService>();
        services.AddHostedService(sp => sp.GetRequiredService<NatsService>());

        // Add Logging
        services.AddLogging(configure => configure.AddConsole());


    })
    .Build();

Console.WriteLine("Bomberman Backend Service starting...");
// Run the host - this starts all IHostedServices (like NatsService)
await host.RunAsync();

Console.WriteLine("Bomberman Backend Service stopped.");