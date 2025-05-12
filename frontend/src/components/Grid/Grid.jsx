// src/components/Grid/Grid.jsx
import React from 'react';                         // Importerer React-biblioteket
import Tile from '../Tile/Tile';                   // Importerer Tile-komponenten for hver celle
import styles from './Grid.module.css';            // Importerer CSS-modules for grid-stil

export default function Grid({ tiles }) {
  const rows = tiles.length;                       // Antall rader i brettet
  const cols = tiles[0]?.length || 0;              // Antall kolonner (sikrer at første rad finnes)

  return (
    <div
      className={styles.grid}                      // Legger til grunnleggende grid-stil
      style={{
        gridTemplateColumns: `repeat(${cols}, 1fr)`, // Setter like brede kolonner
        gridTemplateRows:    `repeat(${rows}, 1fr)` // Setter like høye rader
      }}
    >
      {tiles.flat().map((type, idx) => (           // Flater ut 2D-array til 1D-liste
        <Tile key={idx} type={type} />             // Render en Tile for hver celle
      ))}
    </div>
  );
}

/*
Denne filen, Grid.jsx, definerer en React-komponent som tar imot en 2D-array 'tiles' og
renderer hele spillbrettet ved hjelp av CSS Grid. Den regner ut antall rader og kolonner
fra arrayens dimensjoner, setter opp 'gridTemplateColumns' og 'gridTemplateRows' dynamisk,
og flater deretter ut arrayen for å rendre én Tile-komponent per celle. 
På denne måten skapes en responsiv, jevnt fordelt rutenettvisning av spillkartet.
*/
