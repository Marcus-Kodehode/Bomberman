import React, {
  createContext, useContext,
  useState, useEffect, useCallback
} from 'react';
import {
  subscribeGameState,
  joinGame as apiJoin,
  movePlayer as apiMove,
  placeBomb as apiBomb
} from '../services/gameApi';

const GameContext = createContext();

export function GameProvider({ children }) {
  const [map, setMap] = useState(null);
  const [gameOver, setGameOver] = useState(false);
  const [bombTimer, setBombTimer] = useState(0);

  useEffect(() => {
    const unsub = subscribeGameState(({ map, gameOver }) => {
      setMap(map);
      if (gameOver) setGameOver(true);
    });
    return unsub;
  }, []);

  const joinGame = useCallback(id => apiJoin(id), []);
  const movePlayer = useCallback((id, dx, dy) => apiMove(id, dx, dy), []);
  const placeBomb = useCallback(id => {
    // Spill fuse-lyd umiddelbart
    const fuse = new Audio('/sounds/fuse.mp3');
    fuse.currentTime = 0;
    fuse.play().catch(() => {});

    apiBomb(id);

    // 1 sekund fuse
    setBombTimer(1);

    // planlegg eksplosjons-lyd etter 1s
    const boomTimeout = setTimeout(() => {
      fuse.pause();
      const boom = new Audio('/sounds/explosion.mp3');
      boom.currentTime = 0;
      boom.play().catch(() => {});
    }, 1000);

    // UI-nedtelling
    const iv = setInterval(() => {
      setBombTimer(bt => {
        if (bt <= 1) {
          clearInterval(iv);
          clearTimeout(boomTimeout);
          return 0;
        }
        return bt - 1;
      });
    }, 1000);
  }, []);

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
  const ctx = useContext(GameContext);
  if (!ctx) throw new Error('useGame must be used within GameProvider');
  return ctx;
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
  