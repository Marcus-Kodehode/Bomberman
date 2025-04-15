using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;         // For INatsConnection, NatsConnectionState
using NATS.Net;               // For NatsConnection, NatsOpts
using System;
using System.Collections.Generic; // For List<>
using System.Linq;              // For .Any()
using System.Text.Json;         // For serialization (if needed manually)
using System.Threading;
using System.Threading.Tasks;
using BombermanBackend.Contracts; // Your DTO namespace
using BombermanBackend.Logic;    // For GameManager
using BombermanBackend.Models;   // For GameSession

namespace BombermanBackend.Services
{
    public class NatsService : BackgroundService
    {
        private readonly ILogger<NatsService> _logger;
        private readonly NatsOpts _natsOptions;
        private readonly GameManager _gameManager;
        private readonly GameSession _gameSession;
        private INatsConnection? _natsConnection;

        // Constructor remains the same
        public NatsService(ILogger<NatsService> logger, GameManager gameManager, GameSession gameSession)
        {
            _logger = logger;
            _gameManager = gameManager;
            _gameSession = gameSession; // Keep reference to subscribe to its events too

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

                // --- Subscribe to Connection Events (Optional but Recommended) ---
                _natsConnection.ConnectionDisconnected += HandleNatsDisconnect;
                _natsConnection.ConnectionOpened += HandleNatsReconnect;
                // ----------------------------------------------------------------

                await _natsConnection.ConnectAsync();
                _logger.LogInformation("Connected to NATS server at {Url}", _natsConnection.Opts.Url);

                // --- Subscribe to Game Logic Events ---
                SubscribeToGameEvents();
                // ------------------------------------

                // Start listening for commands from clients
                await SubscribeToCommandsAsync(stoppingToken);

                // Keep the service alive (ExecuteAsync should ideally run forever)
                // The command subscription loop might handle this, or use Task.Delay
                await Task.Delay(Timeout.Infinite, stoppingToken);

            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("NATS Service stopping gracefully (OperationCanceled).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NATS Service encountered an unhandled exception during execution.");
            }
            finally
            {
                // --- Unsubscribe from Game Logic Events ---
                UnsubscribeFromGameEvents();
                // ----------------------------------------

                if (_natsConnection != null)
                {
                    _natsConnection.ConnectionDisconnected -= HandleNatsDisconnect;
                    _natsConnection.ConnectionOpened -= HandleNatsReconnect;
                    await _natsConnection.DisposeAsync();
                    _logger.LogInformation("NATS connection disposed.");
                }
            }
        }

        private ValueTask HandleNatsDisconnect(object? sender, NatsEventArgs args)
        {
            // NatsEventArgs doesn't directly expose the Exception here.
            // Log the disconnect event happened. More detail might require Opts.DisconnectedEventHandler.
            _logger.LogWarning("NATS connection DISCONNECTED.");
            return ValueTask.CompletedTask;
        }
        private ValueTask HandleNatsReconnect(object? sender, NatsEventArgs args)
        {
            var connection = sender as INatsConnection;
            // Use Host and Port from ServerInfo
            string serverHost = connection?.ServerInfo?.Host ?? "Unknown";
            int serverPort = connection?.ServerInfo?.Port ?? 0;
            _logger.LogInformation("NATS connection RECONNECTED to {Host}:{Port}.", serverHost, serverPort);
            return ValueTask.CompletedTask;
        }

        // --- Subscribe to Game Manager/Session Events ---
        private void SubscribeToGameEvents()
        {
            _logger.LogInformation("Subscribing to internal game events...");
            // !! IMPORTANT: These events need to be added to GameManager and GameSession !!
            // _gameManager.PlayerDied += HandlePlayerDied;
            // _gameManager.BombExploded += HandleBombExploded;
            // _gameManager.GameOver += HandleGameOver; // Assuming GameManager determines game over
            // _gameSession.PlayerJoined += HandlePlayerJoined; // Assuming GameSession handles joins
            // _gameSession.PowerUpCollected += HandlePowerUpCollected; // Assuming GameSession handles powerups

            // You might also need an event for general state changes if not polling
            // _gameManager.GameStateChanged += HandleGameStateChanged;
        }

        private void UnsubscribeFromGameEvents()
        {
            _logger.LogInformation("Unsubscribing from internal game events...");
            // !! IMPORTANT: Unsubscribe from the events added above !!
            // _gameManager.PlayerDied -= HandlePlayerDied;
            // _gameManager.BombExploded -= HandleBombExploded;
            // etc...
        }

        // --- NATS Command Subscription Logic ---
        private async Task SubscribeToCommandsAsync(CancellationToken cancellationToken)
        {
            if (_natsConnection == null || _natsConnection.ConnectionState != NatsConnectionState.Open)
            {
                _logger.LogError("Cannot subscribe to commands: NATS connection not open.");
                return;
            }

            _logger.LogInformation("Subscribing to NATS command subjects...");

            // Use Task.WhenAll for concurrent subscriptions
            var subscriptionTasks = new List<Task>
            {
                // Subscribe to PlayerMoveCommand
                Task.Run(async () => {
                    await foreach (var msg in _natsConnection.SubscribeAsync<PlayerMoveCommand>("bomberman.commands.player.move", cancellationToken: cancellationToken))
                    { _logger.LogDebug("Received PlayerMoveCommand for {PlayerId}", msg.Data?.PlayerId); if (msg.Data != null) HandleMoveCommand(msg.Data); }
                }, cancellationToken),

                // Subscribe to PlaceBombCommand
                Task.Run(async () => {
                    await foreach (var msg in _natsConnection.SubscribeAsync<PlaceBombCommand>("bomberman.commands.player.placebomb", cancellationToken: cancellationToken))
                    { _logger.LogDebug("Received PlaceBombCommand for {PlayerId}", msg.Data?.PlayerId); if (msg.Data != null) HandlePlaceBombCommand(msg.Data); }
                }, cancellationToken),

                // Subscribe to JoinGameCommand
                 Task.Run(async () => {
                    await foreach (var msg in _natsConnection.SubscribeAsync<JoinGameCommand>("bomberman.commands.game.join", cancellationToken: cancellationToken))
                    { _logger.LogDebug("Received JoinGameCommand for {PlayerId}", msg.Data?.DesiredPlayerId); if (msg.Data != null) HandleJoinGameCommand(msg.Data); }
                }, cancellationToken)

                // TODO: Add subscriptions for other commands if needed (e.g., StartGameCommand)
            };

            try
            {
                await Task.WhenAll(subscriptionTasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Command subscription tasks cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during command subscription.");
                // May need more robust error handling / restart logic
            }

            _logger.LogInformation("Command subscription loops ended.");
        }

        // --- Command Handling Logic ---
        private void HandleMoveCommand(PlayerMoveCommand command)
        {
            try { /* ... (Logic from previous step - call _gameManager.MovePlayer) ... */ }
            catch (Exception ex) { _logger.LogError(ex, "Error handling PlayerMoveCommand"); }
            // If successful move, GameStateChanged/PlayerMoved event from GameManager should trigger publish
        }

        private void HandlePlaceBombCommand(PlaceBombCommand command)
        {
            try
            {
                if (_gameSession.Players.ContainsKey(command.PlayerId))
                {
                    bool success = _gameManager.PlaceBomb(command.PlayerId, _gameSession.Players[command.PlayerId].X, _gameSession.Players[command.PlayerId].Y);
                    if (success) { _logger.LogInformation("Processed PlaceBomb for {PlayerId} successfully.", command.PlayerId); }
                    else { _logger.LogWarning("Processing PlaceBomb for {PlayerId} failed.", command.PlayerId); }
                    // If successful, GameStateChanged/BombPlaced event should trigger publish
                }
                else { _logger.LogWarning("Player {PlayerId} not found for PlaceBombCommand.", command.PlayerId); }
            }
            catch (Exception ex) { _logger.LogError(ex, "Error handling PlaceBombCommand for {PlayerId}", command.PlayerId); }
        }

        private void HandleJoinGameCommand(JoinGameCommand command)
        {
            try
            {
                // Basic join logic - GameManager might be better suited for this
                if (!_gameSession.Players.ContainsKey(command.DesiredPlayerId))
                {
                    // Assign starting position (needs better logic)
                    int startX = 1; int startY = 1;
                    if (_gameSession.Players.Count > 0) { startX = _gameSession.Map.GetLength(0) - 2; startY = _gameSession.Map.GetLength(1) - 2; }

                    var newPlayer = new Player { Id = command.DesiredPlayerId, X = startX, Y = startY };
                    _gameSession.AddPlayer(newPlayer); // GameSession should ideally raise PlayerJoined event
                    _logger.LogInformation("Processed JoinGame for {PlayerId}.", command.DesiredPlayerId);
                    // PlayerJoined event should trigger publish
                }
                else
                {
                    _logger.LogWarning("Player {PlayerId} already exists, cannot join.", command.DesiredPlayerId);
                    // TODO: Maybe send a JoinFailed message back to requester?
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling JoinGameCommand for {PlayerId}", command.DesiredPlayerId);
            }
        }

        // --- Placeholder Event Handlers (Called by C# events from GameManager/GameSession) ---

        private void HandlePlayerDied(object? sender, /* PlayerDiedEventArgs */ EventArgs e)
        {
            // TODO: Extract data from e
            var eventArgs = e as /* PlayerDiedEventArgs */ object; // Replace with actual EventArgs type
            if (eventArgs != null)
            {
                var dto = new PlayerDiedEvent { /* Populate from eventArgs */ };
                // Use Task.Run or similar fire-and-forget pattern if needed, or make handler async
                _ = PublishPlayerDiedAsync(dto);
            }
        }

        private void HandleBombExploded(object? sender, /* BombExplodedEventArgs */ EventArgs e)
        {
            // TODO: Extract data from e
            var eventArgs = e as /* BombExplodedEventArgs */ object; // Replace with actual EventArgs type
            if (eventArgs != null)
            {
                var dto = new BombExplodedEvent { /* Populate from eventArgs */ };
                _ = PublishBombExplodedAsync(dto);
            }
        }
        private void HandlePlayerJoined(object? sender, /* PlayerJoinedEventArgs */ EventArgs e)
        {
            // TODO: Extract data from e
            var eventArgs = e as /* PlayerJoinedEventArgs */ object; // Replace with actual EventArgs type
            if (eventArgs != null)
            {
                var dto = new PlayerJoinedEvent { /* Populate from eventArgs */ };
                _ = PublishPlayerJoinedAsync(dto);
            }
        }

        private void HandlePowerUpCollected(object? sender, /* PowerUpCollectedEventArgs */ EventArgs e)
        {
            // TODO: Extract data from e
            var eventArgs = e as /* PowerUpCollectedEventArgs */ object; // Replace with actual EventArgs type
            if (eventArgs != null)
            {
                var dto = new PowerUpCollectedEvent { /* Populate from eventArgs */ };
                _ = PublishPowerUpCollectedAsync(dto);
            }
        }
        private void HandleGameOver(object? sender, /* GameOverEventArgs */ EventArgs e)
        {
            // TODO: Extract data from e
            var eventArgs = e as /* GameOverEventArgs */ object; // Replace with actual EventArgs type
            if (eventArgs != null)
            {
                var dto = new GameOverEvent { /* Populate from eventArgs */ };
                _ = PublishGameOverAsync(dto);
            }
        }

        private void HandleGameStateChanged(object? sender, /* GameStateChangedEventArgs */ EventArgs e)
        {
            // TODO: Extract data from e (or just get current state)
            // This is where you might generate and publish the full GameStateUpdate DTO
            // Be mindful of frequency - maybe only publish if state *actually* changed
            // or on a timer synchronized with Tick()
            // GameStateUpdate currentState = GenerateCurrentGameStateDto();
            // _ = PublishGameStateAsync(currentState);
        }


        // --- Publishing Logic Methods ---
        // These methods publish the specific DTOs to NATS subjects

        public async Task PublishGameStateAsync(GameStateUpdate stateUpdate)
        {
            await PublishAsync("bomberman.state.update", stateUpdate);
        }

        public async Task PublishPlayerJoinedAsync(PlayerJoinedEvent joinedEvent)
        {
            // Maybe publish to a general event stream AND a player-specific one?
            await PublishAsync("bomberman.events.player.joined", joinedEvent);
        }

        public async Task PublishPlayerDiedAsync(PlayerDiedEvent diedEvent)
        {
            _logger.LogInformation("Publishing PlayerDiedEvent for {PlayerId}", diedEvent.PlayerId);
            await PublishAsync("bomberman.events.player.died", diedEvent);
            // Might also publish to a general state update or trigger one
        }

        public async Task PublishBombExplodedAsync(BombExplodedEvent explodedEvent)
        {
            _logger.LogInformation("Publishing BombExplodedEvent at ({X},{Y})", explodedEvent.OriginX, explodedEvent.OriginY);
            await PublishAsync("bomberman.events.bomb.exploded", explodedEvent);
            // Consider if this should trigger a general GameStateUpdate publish too
        }

        public async Task PublishPowerUpCollectedAsync(PowerUpCollectedEvent collectedEvent)
        {
            _logger.LogInformation("Publishing PowerUpCollectedEvent for {PlayerId}, Type: {PowerUpType}", collectedEvent.PlayerId, collectedEvent.PowerUpType);
            await PublishAsync("bomberman.events.powerup.collected", collectedEvent);
        }

        public async Task PublishGameOverAsync(GameOverEvent gameOverEvent)
        {
            _logger.LogInformation("Publishing GameOverEvent. Winner: {Winner}", gameOverEvent.WinnerPlayerId ?? "None/Draw");
            await PublishAsync("bomberman.events.game.over", gameOverEvent);
        }


        // Helper method for publishing with error checking
        private async Task PublishAsync<T>(string subject, T data) where T : class
        {
            if (_natsConnection == null || _natsConnection.ConnectionState != NatsConnectionState.Open)
            {
                _logger.LogWarning("Cannot publish to subject {Subject}: NATS connection not open.", subject);
                return;
            }
            try
            {
                // Use the generic overload to leverage built-in serialization (likely JSON for complex types)
                await _natsConnection.PublishAsync<T>(subject, data);
                _logger.LogDebug("Published message to subject {Subject}", subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to subject {Subject}", subject);
            }
        }

        // --- StopAsync (No changes needed) ---
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("NATS Service stopping...");
            // Unsubscribe handled in finally block of ExecuteAsync
            await base.StopAsync(cancellationToken);
        }
    }
}