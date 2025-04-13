// --- File: BombermanBackend.Tests/ContractSerializationTests.cs ---
using Xunit;
using BombermanBackend.Contracts; // Your namespace for DTOs
using System.Text.Json; // Using System.Text.Json for example
using System.Collections.Generic;

namespace BombermanBackend.Tests
{
    public class ContractSerializationTests
    {
        [Fact]
        public void PlayerMoveCommand_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var originalCommand = new PlayerMoveCommand
            {
                PlayerId = "p1",
                Dx = 1,
                Dy = 0
            };

            // Act
            string json = JsonSerializer.Serialize(originalCommand);
            PlayerMoveCommand? deserializedCommand = JsonSerializer.Deserialize<PlayerMoveCommand>(json);

            // Assert
            Assert.NotNull(deserializedCommand);
            Assert.Equal(originalCommand.PlayerId, deserializedCommand.PlayerId);
            Assert.Equal(originalCommand.Dx, deserializedCommand.Dx);
            Assert.Equal(originalCommand.Dy, deserializedCommand.Dy);
        }

        [Fact]
        public void GameStateUpdate_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var originalState = new GameStateUpdate
            {
                MapWidth = 7,
                MapHeight = 7,
                MapTiles = new List<int> { 1, 0, 0, 1, 0, 0, 1, /* ... more tiles */ }, // Example map data
                GamePhase = "Running",
                TickCount = 100,
                Players = new List<PlayerStateDto>
                {
                    new PlayerStateDto { Id = "p1", X = 2, Y = 3, IsAlive = true, BlastRadius = 2, MaxBombs = 1, ActiveBombsCount = 0 }
                },
                Bombs = new List<BombStateDto>
                {
                    new BombStateDto { OwnerId = "p1", X = 2, Y = 3, RemainingFuseTicks = 4 }
                }
            };

            // Act
            // Use options for potential indentation for readability if needed during debug
            var options = new JsonSerializerOptions { WriteIndented = false };
            string json = JsonSerializer.Serialize(originalState, options);
            GameStateUpdate? deserializedState = JsonSerializer.Deserialize<GameStateUpdate>(json);

            // Assert
            Assert.NotNull(deserializedState);
            Assert.Equal(originalState.MapWidth, deserializedState.MapWidth);
            Assert.Equal(originalState.MapHeight, deserializedState.MapHeight);
            Assert.Equal(originalState.MapTiles.Count, deserializedState.MapTiles.Count);
            Assert.Equal(originalState.GamePhase, deserializedState.GamePhase);
            Assert.Equal(originalState.TickCount, deserializedState.TickCount);

            Assert.Single(deserializedState.Players);
            Assert.Equal("p1", deserializedState.Players[0].Id);
            Assert.Equal(2, deserializedState.Players[0].X);

            Assert.Single(deserializedState.Bombs);
            Assert.Equal("p1", deserializedState.Bombs[0].OwnerId);
            Assert.Equal(4, deserializedState.Bombs[0].RemainingFuseTicks);
        }

        // Add similar tests for all your other key DTOs
        // (JoinGameCommand, PlaceBombCommand, PlayerDiedEvent, BombExplodedEvent, etc.)
    }
}