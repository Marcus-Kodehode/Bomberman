using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BombermanBackend.Logic;
using BombermanBackend.Models;
using BombermanBackend.Services;
using System;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register GameSessionManager as a Singleton
        // It will be responsible for creating and managing individual GameSession/GameManager instances
        services.AddSingleton<GameSessionManager>();

        // GameSession and GameManager are no longer registered directly here as singletons.
        // Their lifecycle is managed *per game* by the GameSessionManager.
        // If GameManager had other dependencies, they would need to be registered here
        // so GameSessionManager could resolve them via IServiceProvider if needed.

        // Register MapPrinter - GameManager might need it, or SessionManager might pass it
        services.AddSingleton<MapPrinter>();

        // Register NatsService as a Singleton and Hosted Service
        // NOTE: NatsService constructor and logic will need to change to accept
        // GameSessionManager instead of GameSession/GameManager directly.
        services.AddSingleton<NatsService>();
        services.AddHostedService(sp => sp.GetRequiredService<NatsService>());

        services.AddLogging(configure => configure.AddConsole());
    })
    .Build();

// TODO: Consider replacing Console.WriteLine with injected ILogger
Console.WriteLine("Bomberman Backend Service starting...");
await host.RunAsync();
Console.WriteLine("Bomberman Backend Service stopped.");