import React from 'react';                                     // Importerer React-biblioteket
import { createRoot } from 'react-dom/client';                // Henter funksjonen for å montere React i DOM
import './global.css';                                        // Importerer globale CSS-stiler
import App from './App.jsx';                                  // Importerer rotkomponenten for appen
import { GameProvider } from './context/GameContext';         // Importerer Context-provider for game state

// Henter <div id="root"> fra index.html og rigger React-root
const rootElement = document.getElementById('root');
const root = createRoot(rootElement);

// Render hele appen innenfor GameProvider (gir tilgang til game state overalt)
root.render(
  <GameProvider>
    <App />
  </GameProvider>
);

/*
Denne filen, main.jsx, er inngangspunktet for React-applikasjonen. 
Den importerer nødvendige biblioteker, globale stiler og rotkomponenten App. 
Ved å bruke createRoot monteres React-komponenttreet i <div id="root"> i index.html. 
GameProvider wrappes rundt App for å gi tilgang til delt state (game state) 
gjennom hele komponent-hierarkiet.
*/
