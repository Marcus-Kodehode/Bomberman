const GRID_SIZE = 32;
let subscribers = [];

function generateMap() {
  return Array.from({ length: GRID_SIZE }, (_, y) =>
    Array.from({ length: GRID_SIZE }, (_, x) => {
      if (x === 0 || y === 0 || x === GRID_SIZE-1 || y === GRID_SIZE-1) {
        return 'Wall';
      }
      if (x % 2 === 0 && y % 2 === 0) {
        return 'Wall';
      }
      return Math.random() < 0.5 ? 'DestructibleWall' : 'Empty';
    })
  );
}

let map = generateMap();
const player = { x: 1, y: 1 };
map[player.y][player.x] = 'Player';

function notify(gameOver = false) {
  const snapshot = map.map(row => [...row]);
  subscribers.forEach(cb => cb({ map: snapshot, gameOver }));
}

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

export async function movePlayer(id, dx, dy) {
  const nx = player.x + dx;
  const ny = player.y + dy;
  if (ny < 0 || ny >= GRID_SIZE || nx < 0 || nx >= GRID_SIZE) return;
  if (!canMove(map[ny][nx])) return;

  map[player.y][player.x] = 'Empty';
  player.x = nx; player.y = ny;
  map[ny][nx] = 'Player';
  notify(false);
}

export async function placeBomb(id) {
  const bx = player.x, by = player.y;
  map[by][bx] = 'Bomb';
  notify(false);

  setTimeout(() => {
    let death = false;
    if (bx === player.x && by === player.y) death = true;
    map[by][bx] = 'Explosion';

    [[1,0],[-1,0],[0,1],[0,-1]].forEach(([dx,dy]) => {
      for (let i = 1; i <= 5; i++) {
        const x = bx + dx*i, y = by + dy*i;
        const cell = map[y]?.[x];
        if (!cell || cell === 'Wall') break;
        if (x === player.x && y === player.y) death = true;
        map[y][x] = 'Explosion';
        if (cell === 'DestructibleWall') break;
      }
    });

    notify(death);

    setTimeout(() => {
      for (let y = 0; y < GRID_SIZE; y++) {
        for (let x = 0; x < GRID_SIZE; x++) {
          if (map[y][x] === 'Explosion') map[y][x] = 'Empty';
        }
      }
      notify(death);
    }, 300);
  }, 1000);
}

export async function joinGame(playerId) {
  console.log('Mock joinGame:', playerId);
  return { success: true, playerId };
}
