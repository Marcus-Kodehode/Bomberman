import React from 'react';
import { createRoot } from 'react-dom/client';
import './global.css';
import App from './App.jsx';
import { GameProvider } from './context/GameContext';

createRoot(document.getElementById('root')).render(
  <GameProvider>
    <App />
  </GameProvider>
);
