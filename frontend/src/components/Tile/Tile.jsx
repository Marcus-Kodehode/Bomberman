// src/components/Tile/Tile.jsx
import React from 'react';                    // Importerer React
import styles from './Tile.module.css';       // Importerer CSS-modules for styling

// Fargekart for hver celletype, hentet fra CSS-variabler
const COLORS = {
  Empty: 'var(--empty)',                     // Tomt felt
  Wall: 'var(--wall-fixed)',                 // Indestruktibel vegg
  DestructibleWall: 'var(--wall-dest)',      // Sprengbar vegg
  Bomb: 'var(--bomb)',                       // Bombe
  Player: 'var(--player)',                   // Spilleren
  Explosion: 'var(--explosion)'              // Eksplosjon
};

export default function Tile({ type }) {
  return (
    <div
      className={styles.tile}                 // Legger til grunnleggende tile-stil
      style={{
        backgroundColor:                     // Setter bakgrunnsfarge basert på type
          COLORS[type] || COLORS.Empty       // Fallback til 'Empty' ved ukjent type
      }}
    />
  );
}

/*
Denne filen, Tile.jsx, definerer en enkel React-komponent som representerer én rute på Bomberman-brettet. 
Den bruker CSS-modules for grunnleggende styling (f.eks. størrelse og border) og inline styles for å sette 
riktig bakgrunnsfarge basert på den mottatte 'type'-prop'en. Fargeverdiene er hentet fra CSS-variabler definert 
i global.css, noe som gir lett gjenbruk og tema-kontroll.
*/
