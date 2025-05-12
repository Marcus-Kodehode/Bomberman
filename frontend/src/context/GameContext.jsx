import React, {
    createContext,
    useContext,
    useState,
    useEffect,
    useCallback
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
      apiBomb(id);
      setBombTimer(5);
      const iv = setInterval(() => {
        setBombTimer(bt => {
          if (bt <= 1) { clearInterval(iv); return 0; }
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
  