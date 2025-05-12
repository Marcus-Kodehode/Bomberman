// src/context/GameContext.jsx
import React, {
    createContext,    // Lager en Context for game state
    useContext,       // Hook for å hente context-verdier
    useState,         // Hook for lokal state i provider
    useEffect,        // Hook for side-effekter (abonnement på game state)
    useCallback       // Hook for å memoize callback-funksjoner
  } from 'react';
  import {
    subscribeGameState,    // Abonnement på kart- og gameOver-oppdateringer
    joinGame as apiJoin,   // API-funksjon for å bli med i spillet
    movePlayer as apiMove, // API-funksjon for å flytte spiller
    placeBomb as apiBomb   // API-funksjon for å plassere bombe
  } from '../services/gameApi';
  
  const GameContext = createContext(); // Oppretter context-objektet
  
  export function GameProvider({ children }) {
    const [map, setMap] = useState(null);          // State for spillkartet (2D-array)
    const [gameOver, setGameOver] = useState(false); // State for om spillet er over
    const [bombTimer, setBombTimer] = useState(0);   // State for bombenedteller
  
    // Abonner på mock-APIets game state ved mount
    useEffect(() => {
      const unsub = subscribeGameState(({ map, gameOver }) => {
        setMap(map);                     // Oppdater kartet
        if (gameOver) setGameOver(true); // Sett gameOver hvis flagget er true
      });
      return unsub;                      // Avslutt abonnement ved unmount
    }, []);
  
    // Memoized funksjon for å kalle joinGame-API
    const joinGame = useCallback(id => apiJoin(id), []);
  
    // Memoized funksjon for å flytte spiller via API
    const movePlayer = useCallback(
      (id, dx, dy) => apiMove(id, dx, dy),
      []
    );
  
    // Memoized funksjon for å plassere bombe og starte nedtelling
    const placeBomb = useCallback(id => {
      apiBomb(id);             // Kall mock API
      setBombTimer(5);         // Start nedtelling fra 5 sekunder
      const iv = setInterval(() => {
        setBombTimer(bt => {
          if (bt <= 1) {       // Når teller når 0, stopp interval og sett til 0
            clearInterval(iv);
            return 0;
          }
          return bt - 1;       // Reduser teller med 1 hvert sekund
        });
      }, 1000);
    }, []);
  
    // Returnerer provider med alle verdier og funksjoner i context
    return (
      <GameContext.Provider value={{
        map,
        gameOver,
        bombTimer,
        joinGame,
        movePlayer,
        placeBomb
      }}>
        {children}
      </GameContext.Provider>
    );
  }
  
  export function useGame() {
    const ctx = useContext(GameContext);    // Henter context-verdier
    if (!ctx) throw new Error(
      'useGame must be used within GameProvider'
    );
    return ctx;                             // Returnerer map, gameOver, bombTimer og API-funksjoner
  }
  
  /*
  Denne filen, GameContext.jsx, definerer en React Context og en Provider for spillets state. 
  GameProvider:
  - Abonnerer på game state fra mock-API (kart og gameOver-flag) via subscribeGameState.
  - Eksponerer stateverdiene 'map' (spillkart), 'gameOver' (boolean) og 'bombTimer' (nedtelling).
  - Pakker API-kallene joinGame, movePlayer og placeBomb inn i memoized funksjoner for stabil referanse.
  - Gir nedtelling for eksplosjoner i context ved å starte et interval som teller ned fra 5 sekunder.
  
  useGame:
  - Custom hook som returnerer context-verdiene, og kaster en feil om den brukes utenfor GameProvider.
  */
  