import React from 'react';
import Tile from '../Tile/Tile';
import styles from './Grid.module.css';

export default function Grid({ tiles }) {
  const rows = tiles.length;
  const cols = tiles[0]?.length || 0;

  return (
    <div
      className={styles.grid}
      style={{
        gridTemplateColumns: `repeat(${cols}, 1fr)`,
        gridTemplateRows: `repeat(${rows}, 1fr)`
      }}
    >
      {tiles.flat().map((type, idx) => (
        <Tile key={idx} type={type} />
      ))}
    </div>
  );
}
