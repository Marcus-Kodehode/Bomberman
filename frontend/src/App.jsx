// src/App.jsx
import React, { useState, useEffect } from 'react';                 // Import React and hooks
import Grid from './components/Grid/Grid';                          // Import the grid component
import { useGame } from './context/GameContext';                    // Import custom hook for game state

export default function App() {
  const {
    map,           // The game map as a 2D array
    gameOver,      // Boolean flag indicating game over
    bombTimer,     // Countdown until bomb explosion
    joinGame,      // Function to join the game
    movePlayer,    // Function to move the player
    placeBomb      // Function to place a bomb
  } = useGame();

  const [started, setStarted] = useState(false);                    // State: has the game started?

  // When user clicks "Start Game", call joinGame
  useEffect(() => {
    if (started) {
      joinGame('player1');
    }
  }, [started, joinGame]);

  // Keyboard controls: W/A/S/D, Space, P
  useEffect(() => {
    if (!started || gameOver) return;                              // Only active during gameplay
    const onKey = e => {
      switch (e.key) {
        case 'w': case 'W':
          movePlayer('player1', 0, -1); break;                    // Move up
        case 's': case 'S':
          movePlayer('player1', 0,  1); break;                    // Move down
        case 'a': case 'A':
          movePlayer('player1', -1, 0); break;                    // Move left
        case 'd': case 'D':
          movePlayer('player1', 1,  0); break;                    // Move right
        case ' ':
          placeBomb('player1'); break;                            // Place bomb
        case 'p': case 'P':
          window.location.reload();                                // Restart game
          break;
      }
    };
    window.addEventListener('keydown', onKey);                      // Add listener
    return () => window.removeEventListener('keydown', onKey);      // Cleanup on unmount
  }, [started, gameOver, movePlayer, placeBomb]);

  // Render start screen if not started
  if (!started) {
    return (
      <div className="overlay start-screen">
        <img
          src="/images/Logo.png"
          alt="Bomberman Logo"
          className="logo"                                         // Display game logo
        />
        <button
          className="start-btn"
          onClick={() => setStarted(true)}                         // Start game on click
        >
          Start Game
        </button>
      </div>
    );
  }

  // Render game over overlay if gameOver
  if (gameOver) {
    return (
      <div className="overlay">
        <h2>Game Over!</h2>
        <button onClick={() => window.location.reload()}>         {/* Restart page */}
          Restart
        </button>
      </div>
    );
  }

  // Main UI during gameplay
  return (
    <div className="app-container">
      <header className="header">
        <h1>The Mad Bomberman</h1>
      </header>

      <div className="main">
        <div className="grid-wrapper">                             {/* Game board container */}
          {bombTimer > 0 && (                                      // Show bomb countdown
            <div className="bomb-timer">
              Bomb explodes in {bombTimer}s
            </div>
          )}

          {!map                                                    // Show loading if map not ready
            ? <p className="loading">Loading gamedata...</p>
            : <Grid tiles={map} />                                 // Render the grid
          }
        </div>

        <aside className="sidebar">
          <h2>Instructions</h2>
          <ul>
            <li>W/A/S/D – move</li>
            <li>Space – drop bomb</li>
            <li>P – restart</li>
          </ul>

          <div className="btns">
            <button onClick={() => movePlayer('player1', 0, -1)}>Up</button>
            <button onClick={() => movePlayer('player1', 0, 1)}>Down</button>
            <button onClick={() => movePlayer('player1', -1, 0)}>Left</button>
            <button onClick={() => movePlayer('player1', 1, 0)}>Right</button>
            <button onClick={() => placeBomb('player1')}>Bomb</button>
          </div>

          <div className="legend">                                {/* Color legend */}
            <h3>Color Legend</h3>
            <ul>
              <li><span className="legend-box wall-fixed" /> Indestructible Wall</li>
              <li><span className="legend-box wall-dest" /> Destructible Wall</li>
              <li><span className="legend-box empty" /> Empty Space</li>
              <li><span className="legend-box player" /> Player</li>
              <li><span className="legend-box bomb" /> Bomb (explodes in {bombTimer}s)</li>
              <li><span className="legend-box explosion" /> Explosion</li>
            </ul>
          </div>

          {/* Logo column */}
          <div className="footer-logos">
            <img
              src="/images/Logo.png"
              alt="The Mad Bomberman Logo"
              className="footer-logo"
            />
            <img
              src="/images/MBlogo.png"
              alt="MB Logo"
              className="footer-logo"
            />
            <p className="credit">_design by Børresen Utvikling_</p>
          </div>
        </aside>
      </div>
    </div>
  );
}

/*
App.jsx is the main React component for the Bomberman frontend. It handles:
- Game start via a start screen with logo.
- Game over overlay with a restart button.
- Subscriptions to game state, including map rendering, bomb countdown, and game over.
- Keyboard controls (W/A/S/D, space, P) and sidebar button controls.
- Layout of header, game board, sidebar with instructions, buttons, legend, and footer logo.
*/
