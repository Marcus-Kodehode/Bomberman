// src/App.jsx
import React, { useState, useEffect } from 'react';                 // Importerer React og hooks
import Grid from './components/Grid/Grid';                          // Importerer Grid-komponenten
import { useGame } from './context/GameContext';                    // Importerer custom hook for game state

export default function App() {
  const {
    map,           // Spillkartet som 2D-array
    gameOver,      // Boolean som indikerer game over
    bombTimer,     // Nedtelling til eksplosjon
    joinGame,      // Funksjon for å bli med i spillet
    movePlayer,    // Funksjon for å flytte spilleren
    placeBomb      // Funksjon for å plassere en bombe
  } = useGame();

  const [started, setStarted] = useState(false);                    // State for om spillet har startet

  // Når brukeren starter spillet, kall joinGame
  useEffect(() => {
    if (started) {
      joinGame('player1');
    }
  }, [started, joinGame]);

  // Legger på tastatur‐hendelser (W/A/S/D, Space, P)
  useEffect(() => {
    if (!started || gameOver) return;
    const onKey = e => {
      switch (e.key) {
        case 'w': case 'W':
          movePlayer('player1', 0, -1); break;                      // Flytt opp
        case 's': case 'S':
          movePlayer('player1', 0,  1); break;                      // Flytt ned
        case 'a': case 'A':
          movePlayer('player1', -1, 0); break;                      // Flytt venstre
        case 'd': case 'D':
          movePlayer('player1', 1,  0); break;                      // Flytt høyre
        case ' ':
          placeBomb('player1'); break;                              // Plasser bombe
        case 'p': case 'P':
          window.location.reload();                                  // Restart
          break;
      }
    };
    window.addEventListener('keydown', onKey);                      // Legger til event listener
    return () => window.removeEventListener('keydown', onKey);      // Fjerner listener ved unmount
  }, [started, gameOver, movePlayer, placeBomb]);

  // Render start‐overlay om ikke startet
  if (!started) {
    return (
      <div className="overlay start-screen">
        <img
          src="/images/Logo.png"
          alt="Bomberman Logo"
          className="logo"                                      // Logo øverst
        />
        <button
          className="start-btn"
          onClick={() => setStarted(true)}                       // Starter spillet
        >
          Start Game
        </button>
      </div>
    );
  }

  // Render game over‐overlay
  if (gameOver) {
    return (
      <div className="overlay">
        <h2>Game Over!</h2>
        <button onClick={() => window.location.reload()}>     // Tilbakestill siden
          Restart
        </button>
      </div>
    );
  }

  // Hoved‐UI når spillet er aktivt
  return (
    <div className="app-container">
      <header className="header">
        <h1>Bomberman Frontend</h1>
      </header>

      <div className="main">
        <div className="grid-wrapper">
          {bombTimer > 0 && (                                  // Vis bombenedtelling
            <div className="bomb-timer">
              Bombe eksploderer om {bombTimer}s
            </div>
          )}

          {!map                                               // Vis loading om kartet ikke er lastet
            ? <p className="loading">Laster spilldata…</p>
            : <Grid tiles={map} />                           // Render Grid-komponenten
          }
        </div>

        <aside className="sidebar">
          <h2>Instruksjoner</h2>
          <ul>
            <li>W/A/S/D – flytt</li>
            <li>Mellomrom – legg bombe</li>
            <li>P – restart</li>
          </ul>

          <div className="btns">
            <button onClick={() => movePlayer('player1', 0, -1)}>Opp</button>
            <button onClick={() => movePlayer('player1', 0, 1)}>Ned</button>
            <button onClick={() => movePlayer('player1', -1, 0)}>Venstre</button>
            <button onClick={() => movePlayer('player1', 1, 0)}>Høyre</button>
            <button onClick={() => placeBomb('player1')}>Bomb</button>
          </div>

          <div className="legend">
            <h3>Farge-legende</h3>
            <ul>
              <li><span className="legend-box wall-fixed" /> Indestruktibel vegg</li>
              <li><span className="legend-box wall-dest" /> Sprengbar vegg</li>
              <li><span className="legend-box empty" /> Tomt felt</li>
              <li><span className="legend-box player" /> Spilleren</li>
              <li><span className="legend-box bomb" /> Bombe (eksploderer om {bombTimer}s)</li>
              <li><span className="legend-box explosion" /> Eksplosjon</li>
            </ul>
          </div>
        </aside>
      </div>
    </div>
  );
}

/*
Denne filen, App.jsx, definerer hovedkomponenten for frontend-spillet. 
Først setter den opp state og hooks for å håndtere spillstart, game over, 
tastaturkontroller og nedtelling for bombe-eksplosjoner. 
Den rendrer ulike overlays basert på om spillet er startet eller er over, 
og når spillet er aktivt viser den brettet med Grid-komponenten, bombenedtelling, 
instruksjoner, kontrollknapper og en farge-legende. 
*/
