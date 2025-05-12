// src/components/Tile/Tile.jsx
import React from 'react';
import styles from './Tile.module.css';
// import BombSVG from '/images/bomb.svg';
// import ExplosionSVG from '/images/explosion.svg';

// Fallback-farger for typene som ikke bruker SVG
const COLORS = {
  Empty:            'var(--empty)',
  Wall:             'var(--wall-fixed)',
  DestructibleWall: 'var(--wall-dest)',
  Player:           'var(--player)',
};

export default function Tile({ type }) {
  return (
    <div className={styles.tile}>
      {type === 'Bomb' && (
        <img
          src="/images/bomb.svg"
          alt="Bomb"
          className={styles.bombIcon}
        />
      )}
      {type === 'Explosion' && (
        <img
          src="/images/explosion.svg"
          alt="Explosion"
          className={styles.explosionIcon}
        />
      )}
      {!['Bomb','Explosion'].includes(type) && (
        <div
          className={styles.defaultFill}
          style={{ backgroundColor: COLORS[type] || COLORS.Empty }}
        />
      )}
    </div>
  );
}



/*
Denne filen, Tile.jsx, definerer en enkel React-komponent som representerer én rute på Bomberman-brettet. 
Den bruker CSS-modules for grunnleggende styling (f.eks. størrelse og border) og inline styles for å sette 
riktig bakgrunnsfarge basert på den mottatte 'type'-prop'en. Fargeverdiene er hentet fra CSS-variabler definert 
i global.css, noe som gir lett gjenbruk og tema-kontroll.
*/
