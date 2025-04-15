using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BombermanBackend.Contracts; // DTOs
using BombermanBackend.Logic;    // Managers
using BombermanBackend.Models;   // Core Models & EventArgs

namespace BombermanBackend.Services
{
    public class NatsService : BackgroundService
    {
        private readonly ILogger<NatsService> _logger;
        private readonly NatsOpts _natsOptions;
        private readonly GameSessionManager _sessionManager;
        private INatsConnection? _natsConnection;

        // Store reference to the initial game's components for event handling (TEMP solution)
        private string? _initialGameId;
        private GameSession? _initialGameSession;
        private GameManager? _initialGameManager;


        public NatsService(ILogger<NatsService> logger, GameSessionManager sessionManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;

            var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://localhost:4222";
            _natsOptions = NatsOpts.Default with { Url = natsUrl, Name = "BombermanBackendService" };
            _logger.LogInformation("NATS Service configured for URL: {NatsUrl}", natsUrl);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NATS Service starting...");

            try
            {
                _natsConnection = new NatsConnection(_natsOptions);
                _natsConnection.ConnectionDisconnected += HandleNatsDisconnect;
                _natsConnection.ConnectionOpened += HandleNatsReconnect;

                await _natsConnection.ConnectAsync();
                _logger.LogInformation("Connected to NATS server at {Url}", _natsConnection.Opts.Url);

                // --- Create and Store Initial Game Session ---
                _initialGameId = _sessionManager.CreateNewSession();
                _initialGameSession = _sessionManager.GetSession(_initialGameId);
                _initialGameManager = _sessionManager.GetManagerForSession(_initialGameId);
                _logger.LogInformation("Created initial game session with ID: {GameId}", _initialGameId);
                // -------------------------------------------

                SubscribeToGameEvents(); // Subscribe to events from the initial session

                var commandTask = SubscribeToCommandsAsync(stoppingToken);
                var tickTask = RunGameTicksAsync(stoppingToken);

                await Task.WhenAll(commandTask, tickTask);
            }
            catch (OperationCanceledException) { _logger.LogInformation("NATS Service stopping gracefully (OperationCanceled)."); }
            catch (Exception ex) { _logger.LogError(ex, "NATS Service encountered an unhandled exception during execution."); }
            finally
            {
                UnsubscribeFromGameEvents(); // Unsubscribe from the initial session
                if (_natsConnection != null)
                {
                    _natsConnection.ConnectionDisconnected -= HandleNatsDisconnect;
                    _natsConnection.ConnectionOpened -= HandleNatsReconnect;
                    await _natsConnection.DisposeAsync();
                    _logger.LogInformation("NATS connection disposed.");
                }
            }
        }

        // --- NATS Connection Event Handlers ---
        private ValueTask HandleNatsDisconnect(object? sender, NatsEventArgs args)
        {
            _logger.LogWarning("NATS connection DISCONNECTED."); return ValueTask.CompletedTask;
        }
        private ValueTask HandleNatsReconnect(object? sender, NatsEventArgs args)
        {
            var connection = sender as INatsConnection;
            string serverHost = connection?.ServerInfo?.Host ?? "Unknown"; int serverPort = connection?.ServerInfo?.Port ?? 0;
            _logger.LogInformation("NATS connection RECONNECTED to {Host}:{Port}.", serverHost, serverPort); return ValueTask.CompletedTask;
        }

        // --- Subscribe/Unsubscribe to Game Events (for Initial Session) ---
        private void SubscribeToGameEvents()
        {
            if (_initialGameManager == null || _initialGameSession == null) return;
            _logger.LogInformation("Subscribing to internal game events for GameId: {GameId}", _initialGameId);

            _initialGameManager.PlayerDied += HandlePlayerDied;
            _initialGameManager.BombExploded += HandleBombExploded;
            _initialGameManager.GameOver += HandleGameOver;
            _initialGameSession.PlayerJoined += HandlePlayerJoined;
            _initialGameSession.PowerUpCollected += HandlePowerUpCollected;
        }

        private void UnsubscribeFromGameEvents()
        {
            if (_initialGameManager == null || _initialGameSession == null) return;
            _logger.LogInformation("Unsubscribing from internal game events for GameId: {GameId}", _initialGameId);

            _initialGameManager.PlayerDied -= HandlePlayerDied;
            _initialGameManager.BombExploded -= HandleBombExploded;
            _initialGameManager.GameOver -= HandleGameOver;
            _initialGameSession.PlayerJoined -= HandlePlayerJoined;
            _initialGameSession.PowerUpCollected -= HandlePowerUpCollected;
        }

        // --- NATS Command Subscription Logic ---
        private async Task SubscribeToCommandsAsync(CancellationToken cancellationToken)
        {
            if (_natsConnection == null || _natsConnection.ConnectionState != NatsConnectionState.Open)
            {
                _logger.LogError("Cannot subscribe to commands: NATS connection not open."); return;
            }
            _logger.LogInformation("Subscribing to NATS command subjects...");

            // These subscriptions currently only affect the _initialGameId
            var subscriptionTasks = new List<Task> {
                Task.Run(async () => { await foreach (var msg in _natsConnection.SubscribeAsync<PlayerMoveCommand>("bomberman.commands.player.move", cancellationToken: cancellationToken)) { if (msg.Data != null) HandleMoveCommand(msg.Data); } }, cancellationToken),
                Task.Run(async () => { await foreach (var msg in _natsConnection.SubscribeAsync<PlaceBombCommand>("bomberman.commands.player.placebomb", cancellationToken: cancellationToken)) { if (msg.Data != null) HandlePlaceBombCommand(msg.Data); } }, cancellationToken),
                Task.Run(async () => { await foreach (var msg in _natsConnection.SubscribeAsync<JoinGameCommand>("bomberman.commands.game.join", cancellationToken: cancellationToken)) { if (msg.Data != null) HandleJoinGameCommand(msg.Data); } }, cancellationToken)
            };
            try { await Task.WhenAll(subscriptionTasks); }
            catch (OperationCanceledException) { _logger.LogInformation("Command subscription tasks cancelled."); }
            catch (Exception ex) { _logger.LogError(ex, "Error during command subscription."); }
            _logger.LogInformation("Command subscription processing stopped.");
        }

        // --- Command Handling Logic (Using SessionManager - TEMP uses initial game) ---
        private void HandleMoveCommand(PlayerMoveCommand command)
        {
            string targetGameId = _initialGameId ?? "default"; // TEMP: Target initial game
            GameManager? manager = _sessionManager.GetManagerForSession(targetGameId);
            GameSession? session = _sessionManager.GetSession(targetGameId);

            if (manager != null && session != null && session.Players.TryGetValue(command.PlayerId, out var player))
            {
                try
                {
                    int targetX = player.X + command.Dx; int targetY = player.Y + command.Dy;
                    manager.MovePlayer(command.PlayerId, targetX, targetY); // MovePlayer raises PowerUpCollected event internally
                }
                catch (Exception ex) { _logger.LogError(ex, "Error handling PlayerMoveCommand for {Pl} in {Game}", command.PlayerId, targetGameId); }
            }
            else { _logger.LogWarning("Game/Player not found for MoveCommand: {Pl} in {Game}", command.PlayerId, targetGameId); }
        }
        private void HandlePlaceBombCommand(PlaceBombCommand command)
        {
            string targetGameId = _initialGameId ?? "default"; // TEMP: Target initial game
            GameManager? manager = _sessionManager.GetManagerForSession(targetGameId);
            GameSession? session = _sessionManager.GetSession(targetGameId);

            if (manager != null && session != null && session.Players.TryGetValue(command.PlayerId, out var player))
            {
                try { manager.PlaceBomb(command.PlayerId, player.X, player.Y); }
                catch (Exception ex) { _logger.LogError(ex, "Error handling PlaceBombCommand for {Pl} in {Game}", command.PlayerId, targetGameId); }
            }
            else { _logger.LogWarning("Game/Player not found for PlaceBombCommand: {Pl} in {Game}", command.PlayerId, targetGameId); }
        }
        private void HandleJoinGameCommand(JoinGameCommand command)
        {
            string targetGameId = _initialGameId ?? "default"; // TEMP: Target initial game
            GameSession? session = _sessionManager.GetSession(targetGameId);
            if (session == null)
            {
                _logger.LogWarning("No active game session found to join for player {PlayerId}", command.DesiredPlayerId);
                // Maybe create game if none exists? -> Needs NATS command to create game
                return;
            }
            try
            {
                if (!session.Players.ContainsKey(command.DesiredPlayerId))
                {
                    int startX = 1; int startY = 1; // TODO: Better placement logic
                    if (session.Players.Count % 2 != 0) { startX = session.Map.GetLength(0) - 2; }
                    if (session.Players.Count > 1) { startY = session.Map.GetLength(1) - 2; }
                    var newPlayer = new Player { Id = command.DesiredPlayerId, X = startX, Y = startY };
                    session.AddPlayer(newPlayer); // This now raises PlayerJoined event internally
                }
                else { _logger.LogWarning("Player {Pl} already exists in game {Game}.", command.DesiredPlayerId, targetGameId); }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error handling JoinGameCommand for {Pl} in {Game}", command.DesiredPlayerId, targetGameId); }
        }

        // --- C# Event Handlers (Publishing to NATS) ---
        private void HandlePlayerJoined(object? sender, PlayerEventArgs e)
        {
            string gameId = _initialGameId ?? "unknown"; // TEMP: Assume initial game
            var playerState = new PlayerStateDto
            { // Map from Player model to DTO
                Id = e.Player.Id,
                X = e.Player.X,
                Y = e.Player.Y,
                MaxBombs = e.Player.MaxBombs,
                ActiveBombsCount = e.Player.ActiveBombsCount,
                BlastRadius = e.Player.BlastRadius,
                IsAlive = true
            };
            var dto = new PlayerJoinedEvent { Player = playerState };
            _ = PublishPlayerJoinedAsync(gameId, dto); // Fire and forget publish task
        }

        private void HandlePowerUpCollected(object? sender, PowerUpCollectedEventArgs e)
        {
            string gameId = _initialGameId ?? "unknown"; // TEMP: Assume initial game
                                                         // Map from Models.TileType to Contracts.PowerUpTypeDto
            PowerUpTypeDto powerUpTypeDto = e.PowerUpType switch
            {
                TileType.PowerUpBombCount => PowerUpTypeDto.BombCount,
                TileType.PowerUpBlastRadius => PowerUpTypeDto.BlastRadius,
                _ => PowerUpTypeDto.Unknown
            };
            if (powerUpTypeDto != PowerUpTypeDto.Unknown)
            {
                var dto = new PowerUpCollectedEvent
                {
                    PlayerId = e.Player.Id,
                    PowerUpType = powerUpTypeDto,
                    X = e.X,
                    Y = e.Y,
                    NewValue = (powerUpTypeDto == PowerUpTypeDto.BombCount) ? e.Player.MaxBombs : e.Player.BlastRadius
                };
                _ = PublishPowerUpCollectedAsync(gameId, dto);
            }
        }

        private void HandlePlayerDied(object? sender, PlayerEventArgs e)
        {
            string gameId = _initialGameId ?? "unknown"; // TEMP: Assume initial game
            var dto = new PlayerDiedEvent { PlayerId = e.Player.Id, X = e.Player.X, Y = e.Player.Y };
            _ = PublishPlayerDiedAsync(gameId, dto);
        }

        private void HandleBombExploded(object? sender, BombExplodedEventArgs e)
        {
            string gameId = _initialGameId ?? "unknown"; // TEMP: Assume initial game
            var dto = new BombExplodedEvent
            {
                OriginX = e.OriginX,
                OriginY = e.OriginY,
                // Map List<(int,int)> to List<AffectedTileDto>
                AffectedTiles = e.AffectedTiles.Select(t => new AffectedTileDto { X = t.X, Y = t.Y }).ToList(),
                HitPlayerIds = e.HitPlayerIds
            };
            _ = PublishBombExplodedAsync(gameId, dto);
        }

        private void HandleGameOver(object? sender, GameOverEventArgs e)
        {
            string gameId = _initialGameId ?? "unknown"; // TEMP: Assume initial game
            var dto = new GameOverEvent { WinnerPlayerId = e.WinnerPlayerId };
            _ = PublishGameOverAsync(gameId, dto);
            // Should also likely remove the game session from the manager here or soon after
            // _sessionManager.RemoveSession(gameId); // Careful about timing
        }

        // --- Game Tick Loop ---
        private async Task RunGameTicksAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting game tick loop...");
            var tickInterval = TimeSpan.FromMilliseconds(200); // Slower tick for testing

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(tickInterval, stoppingToken);
                try
                {
                    _sessionManager.TickAllSessions(); // Calls Tick() in each active GameManager

                    // TODO: Publish GameStateUpdate after ticking
                    // For now, let's publish state for the initial game
                    if (!string.IsNullOrEmpty(_initialGameId) && _initialGameSession != null)
                    {
                        // Create GameStateUpdate DTO from current session state
                        var gameStateDto = CreateGameStateUpdateDto(_initialGameId, _initialGameSession);
                        await PublishGameStateAsync(_initialGameId, gameStateDto);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Error during game tick execution."); }
            }
            _logger.LogInformation("Game tick loop stopped.");
        }

        // Helper to create GameStateUpdate DTO
        private GameStateUpdate CreateGameStateUpdateDto(string gameId, GameSession session)
        {
            // Map TileType[,] to List<int>
            var mapTiles = new List<int>();
            int width = session.Map.GetLength(0);
            int height = session.Map.GetLength(1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    mapTiles.Add((int)session.Map[x, y]);
                }
            }

            return new GameStateUpdate
            {
                MapTiles = mapTiles,
                MapWidth = width,
                MapHeight = height,
                GamePhase = "Running", // TODO: Track actual phase
                TickCount = 0, // TODO: Track tick count in session/manager?
                Players = session.Players.Values.Select(p => new PlayerStateDto
                {
                    Id = p.Id,
                    X = p.X,
                    Y = p.Y,
                    BlastRadius = p.BlastRadius,
                    IsAlive = true, // Assume alive if in dict
                    MaxBombs = p.MaxBombs,
                    ActiveBombsCount = p.ActiveBombsCount
                }).ToList(),
                Bombs = session.Bombs.Select(b => new BombStateDto
                {
                    OwnerId = b.OwnerId,
                    X = b.X,
                    Y = b.Y,
                    RemainingFuseTicks = b.RemainingFuseTicks
                }).ToList()
            };
        }


        // --- Publishing Logic Methods (Updated subjects) ---
        // ... (Publish methods remain largely the same as before, using gameId in subject) ...
        public async Task PublishGameStateAsync(string gameId, GameStateUpdate stateUpdate) => await PublishAsync($"bomberman.game.{gameId}.state.update", stateUpdate);
        public async Task PublishPlayerJoinedAsync(string gameId, PlayerJoinedEvent joinedEvent) => await PublishAsync($"bomberman.game.{gameId}.events.player.joined", joinedEvent);
        public async Task PublishPlayerDiedAsync(string gameId, PlayerDiedEvent diedEvent) => await PublishAsync($"bomberman.game.{gameId}.events.player.died", diedEvent);
        public async Task PublishBombExplodedAsync(string gameId, BombExplodedEvent explodedEvent) => await PublishAsync($"bomberman.game.{gameId}.events.bomb.exploded", explodedEvent);
        public async Task PublishPowerUpCollectedAsync(string gameId, PowerUpCollectedEvent collectedEvent) => await PublishAsync($"bomberman.game.{gameId}.events.powerup.collected", collectedEvent);
        public async Task PublishGameOverAsync(string gameId, GameOverEvent gameOverEvent) => await PublishAsync($"bomberman.game.{gameId}.events.game.over", gameOverEvent);

        // Generic helper for publishing
        private async Task PublishAsync<T>(string subject, T data) where T : class { /* ... (Same as before) ... */ }

        // StopAsync remains the same
        public override async Task StopAsync(CancellationToken cancellationToken) { /* ... */ }
    }
}