// src/App.jsx
import React, { useState, useEffect } from 'react';
import Grid from './components/Grid/Grid';
import { useGame } from './context/GameContext';

export default function App() {
  const {
    map,
    gameOver,
    bombTimer,
    joinGame,
    movePlayer,
    placeBomb
  } = useGame();

  const [started, setStarted] = useState(false);

  // Start spillet
  useEffect(() => {
    if (started) {
      joinGame('player1');
    }
  }, [started, joinGame]);

  // Tastatur‐kontroll
  useEffect(() => {
    if (!started || gameOver) return;
    const onKey = e => {
      switch (e.key) {
        case 'w': case 'W': movePlayer('player1', 0, -1); break;
        case 's': case 'S': movePlayer('player1', 0,  1); break;
        case 'a': case 'A': movePlayer('player1', -1, 0); break;
        case 'd': case 'D': movePlayer('player1', 1,  0); break;
        case ' ':               placeBomb('player1'); break;
        case 'p': case 'P': window.location.reload();  break;
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [started, gameOver, movePlayer, placeBomb]);

  // Start‐overlay
  if (!started) {
    return (
      <div className="overlay start-screen">
        <img
          src="/images/Logo.png"
          alt="Bomberman Logo"
          className="logo"
        />
        <button
          className="start-btn"
          onClick={() => setStarted(true)}
        >
          Start Game
        </button>
      </div>
    );
  }

  // Game Over‐overlay
  if (gameOver) {
    return (
      <div className="overlay">
        <h2>Game Over!</h2>
        <button onClick={() => window.location.reload()}>
          Restart
        </button>
      </div>
    );
  }

  // Hoved‐UI
  return (
    <div className="app-container">
      <header className="header">
        <h1>Bomberman Frontend</h1>
      </header>

      <div className="main">
        <div className="grid-wrapper">
          {bombTimer > 0 && (
            <div className="bomb-timer">
              Bombe eksploderer om {bombTimer}s
            </div>
          )}

          {!map ? (
            <p className="loading">Laster spilldata…</p>
          ) : (
            <Grid tiles={map} />
          )}
        </div>

        <aside className="sidebar">
          <h2>Instruksjoner</h2>
          <ul>
            <li>W/A/S/D – flytt</li>
            <li>Mellomrom – legg bombe</li>
            <li>P – restart</li>
          </ul>

          <div className="btns">
            <button onClick={() => movePlayer('player1', 0, -1)}>
              Opp
            </button>
            <button onClick={() => movePlayer('player1', 0, 1)}>
              Ned
            </button>
            <button onClick={() => movePlayer('player1', -1, 0)}>
              Venstre
            </button>
            <button onClick={() => movePlayer('player1', 1, 0)}>
              Høyre
            </button>
            <button onClick={() => placeBomb('player1')}>
              Bomb
            </button>
          </div>

          <div className="legend">
            <h3>Farge-legende</h3>
            <ul>
              <li>
                <span className="legend-box wall-fixed" />
                Indestruktibel vegg
              </li>
              <li>
                <span className="legend-box wall-dest" />
                Sprengbar vegg
              </li>
              <li>
                <span className="legend-box empty" />
                Tomt felt
              </li>
              <li>
                <span className="legend-box player" />
                Spilleren
              </li>
              <li>
                <span className="legend-box bomb" />
                Bombe (eksploderer om {bombTimer}s)
              </li>
              <li>
                <span className="legend-box explosion" />
                Eksplosjon
              </li>
            </ul>
          </div>
        </aside>
      </div>
    </div>
  );
}
