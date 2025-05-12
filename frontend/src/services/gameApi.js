// src/services/gameApi.js

const GRID_SIZE = 32;
let subscribers = [];

/**
 * Genererer et nytt Bomberman‐kart:
 * - Ytterkant = indestruktible vegger
 * - Faste indestruktible vegger i alle par‐koordinater
 * - Resten tilfeldig sprengbare eller tomme
 * - Sørger for at start‐sonen i øvre venstre er fri
 */
function generateMap() {
  const map = Array.from({ length: GRID_SIZE }, (_, y) =>
    Array.from({ length: GRID_SIZE }, (_, x) => {
      // Ytterkant
      if (x === 0 || y === 0 || x === GRID_SIZE - 1 || y === GRID_SIZE - 1) {
        return 'Wall';
      }
      // Faste innervegger i par‐koordinater
      if (x % 2 === 0 && y % 2 === 0) {
        return 'Wall';
      }
      // Ellers tilfeldig sprengbar
      return Math.random() < 0.5 ? 'DestructibleWall' : 'Empty';
    })
  );

  // Sørg for at start‐området i øvre venstre er åpent:
  // (1,1) hvor spilleren står, pluss (1,2) og (2,1)
  map[1][1] = 'Empty';
  map[1][2] = 'Empty';
  map[2][1] = 'Empty';

  return map;
}

// Lag kart + plasser spiller
let map = generateMap();
const player = { x: 1, y: 1 };
map[player.y][player.x] = 'Player';

// Varsle alle subscribers med snapshot + gameOver‐flag
function notify(gameOver = false) {
  const snapshot = map.map(row => [...row]);
  subscribers.forEach(cb => cb({ map: snapshot, gameOver }));
}

/** Abonner på spill‐state */
export function subscribeGameState(onMessage) {
  subscribers.push(onMessage);
  onMessage({ map: map.map(r => [...r]), gameOver: false });
  return () => {
    subscribers = subscribers.filter(fn => fn !== onMessage);
  };
}

function canMove(cell) {
  return cell === 'Empty' || cell === 'PowerUp';
}

/** Flytt spilleren hvis ledig */
export async function movePlayer(id, dx, dy) {
  const nx = player.x + dx;
  const ny = player.y + dy;
  if (
    nx < 0 || nx >= GRID_SIZE ||
    ny < 0 || ny >= GRID_SIZE ||
    !canMove(map[ny][nx])
  ) return;

  map[player.y][player.x] = 'Empty';
  player.x = nx; 
  player.y = ny;
  map[ny][nx] = 'Player';
  notify(false);
}

/** Plasser bombe og spreng i radius 5 etter 1 s */
export async function placeBomb(id) {
  const bx = player.x, by = player.y;
  map[by][bx] = 'Bomb';
  notify(false);

  setTimeout(() => {
    let death = false;
    // Sjekk om spilleren står på bomben
    if (bx === player.x && by === player.y) death = true;
    map[by][bx] = 'Explosion';

    // Fire retninger
    [[1,0],[-1,0],[0,1],[0,-1]].forEach(([dx, dy]) => {
      for (let i = 1; i <= 5; i++) {
        const x = bx + dx * i;
        const y = by + dy * i;
        const cell = map[y]?.[x];
        if (!cell || cell === 'Wall') break;
        if (x === player.x && y === player.y) death = true;
        map[y][x] = 'Explosion';
        if (cell === 'DestructibleWall') break;
      }
    });

    notify(death);

    // Rydd eksplosjoner etter 300 ms
    setTimeout(() => {
      for (let y = 0; y < GRID_SIZE; y++) {
        for (let x = 0; x < GRID_SIZE; x++) {
          if (map[y][x] === 'Explosion') {
            map[y][x] = 'Empty';
          }
        }
      }
      notify(death);
    }, 300);
  }, 1000);
}

/** Stub for å bli med i spill */
export async function joinGame(playerId) {
  console.log('Mock joinGame:', playerId);
  return { success: true, playerId };
}
