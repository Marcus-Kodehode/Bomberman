// src/App.jsx
import React, { useState, useEffect } from 'react';                 // Import React + hooks
import Grid from './components/Grid/Grid';                          // Spillebrett-komponenten
import { useGame } from './context/GameContext';                    // Custom hook for spill-state

export default function App() {
  const {
    map,           // 2D-array: kartet
    gameOver,      // true når spilleren har tapt
    bombTimer,     // nedtelling til eksplosjon
    joinGame,      // starter spillet
    movePlayer,    // flytt‐funksjon
    placeBomb      // plassér bombe‐funksjon
  } = useGame();

  const [started, setStarted] = useState(false);                    // om spillet har startet

  // Når brukeren trykker «Start Game», kalles joinGame
  useEffect(() => {
    if (started) joinGame('player1');
  }, [started, joinGame]);

  // Tastaturstyring: W/A/S/D, Space, P
  useEffect(() => {
    if (!started || gameOver) return;
    const onKey = e => {
      switch (e.key) {
        case 'w': case 'W':
          movePlayer('player1', 0, -1); break;                    // opp
        case 's': case 'S':
          movePlayer('player1', 0,  1); break;                    // ned
        case 'a': case 'A':
          movePlayer('player1', -1, 0); break;                    // venstre
        case 'd': case 'D':
          movePlayer('player1', 1,  0); break;                    // høyre
        case ' ':
          placeBomb('player1'); break;                            // legg bombe
        case 'p': case 'P':
          window.location.reload();                                // restart
          break;
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [started, gameOver, movePlayer, placeBomb]);

  // ** Start‐skjerm **
  if (!started) {
    return (
      <div className="overlay start-screen">
        <img
          src="/images/Logo.png"
          alt="Bomberman Logo"
          className="logo"                                      // Spillelogo øverst
        />
        <button
          className="start-btn"
          onClick={() => setStarted(true)}                       // Start
        >
          Start Game
        </button>
      </div>
    );
  }

  // ** Game Over‐skjerm **
  if (gameOver) {
    return (
      <div className="overlay">
        <h2>Game Over!</h2>
        <button onClick={() => window.location.reload()}>      {/* Restart */}
          Restart
        </button>
      </div>
    );
  }

  // ** Hoved‐UI under spilling **
  return (
    <div className="app-container">
      <header className="header">
        <h1>The Mad Bomberman</h1>
      </header>

      <div className="main">
        <div className="grid-wrapper">
          {bombTimer > 0 && (                                     // vis nedtelling
            <div className="bomb-timer">
              Bomb explodes in {bombTimer}s
            </div>
          )}

          {!map
            ? <p className="loading">Loading gamedata…</p>
            : <Grid tiles={map} />                              // rendrer brettet
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

          <div className="legend">
            <h3>Color Legend</h3>
            <ul>
              <li><span className="legend-box wall-fixed" />Indestructible Wall</li>
              <li><span className="legend-box wall-dest" />Destructible Wall</li>
              <li><span className="legend-box empty" />Empty Space</li>
              <li><span className="legend-box player" />Player</li>
              <li><span className="legend-box bomb" />Bomb (explodes in {bombTimer}s)</li>
              <li><span className="legend-box explosion" />Explosion</li>
            </ul>
          </div>

          {/* Klikkbar logo + egen logo + credit */}
          <div className="footer-logos">
            <img
              src="/images/Logo.png"
              alt="Bomberman Logo"
              className="footer-logo"
            />

            <a
              href="https://github.com/Marcus-Kodehode"
              target="_blank"
              rel="noopener noreferrer"
            >
              <img
                src="/images/MBlogo.png"
                alt="The Mad Bomberman GitHub"
                className="footer-logo"
              />
            </a>

            <div className="credit">design by Børresen Utvikling</div>
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
