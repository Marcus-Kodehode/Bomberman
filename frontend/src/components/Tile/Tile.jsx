import React from 'react';
import styles from './Tile.module.css';

const COLORS = {
  Empty: 'var(--empty)',
  Wall: 'var(--wall-fixed)',
  DestructibleWall: 'var(--wall-dest)',
  Bomb: 'var(--bomb)',
  Player: 'var(--player)',
  Explosion: 'var(--explosion)',
};

export default function Tile({ type }) {
  return (
    <div
      className={styles.tile}
      style={{ backgroundColor: COLORS[type] || COLORS.Empty }}
    />
  );
}
