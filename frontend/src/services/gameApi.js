// src/services/gameApi.js

const GRID_SIZE = 32;                                      // Antall rader/kolonner i brettet
let subscribers = [];                                      // Liste over callbacks som får state-oppdateringer

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
        return 'Wall';                                     // Indestruktibel vegg
      }
      // Faste innervegger i par‐koordinater
      if (x % 2 === 0 && y % 2 === 0) {
        return 'Wall';                                     // Flere indestruktible vegger
      }
      // Ellers tilfeldig sprengbar eller tom
      return Math.random() < 0.5 ? 'DestructibleWall' : 'Empty';
    })
  );

  // Åpne startområdet (unngå innesperring)
  map[1][1] = 'Empty';                                     // Startposisjon
  map[1][2] = 'Empty';                                     // Rute rett under
  map[2][1] = 'Empty';                                     // Rute til høyre

  return map;                                              // Returnerer det ferdige kartet
}

// Opprett kart og plasser spilleren
let map = generateMap();                                   // Initialiserer spillets brett
const player = { x: 1, y: 1 };                             // Spillerens posisjon
map[player.y][player.x] = 'Player';                        // Merk startposisjonen

/**
 * Varsler alle subscribers med snapshot av kart + gameOver-flag
 * gameOver = true hvis spilleren dør i siste eksplosjon
 */
function notify(gameOver = false) {
  const snapshot = map.map(row => [...row]);               // Klon kartet
  subscribers.forEach(cb => cb({ map: snapshot, gameOver })); // Kall hver callback
}

/** Abonner på spill‐state; sender umiddelbart en initial melding */
export function subscribeGameState(onMessage) {
  subscribers.push(onMessage);                             // Legg til ny subscriber
  onMessage({ map: map.map(r => [...r]), gameOver: false }); // Initial oppdatering
  return () => {
    subscribers = subscribers.filter(fn => fn !== onMessage); // Avslutt abonnement
  };
}

// Sjekk om en celle kan flyttes inn på
function canMove(cell) {
  return cell === 'Empty' || cell === 'PowerUp';           // Tom eller power-up
}

/** Flytter spilleren i kartet hvis neste celle er ledig */
export async function movePlayer(id, dx, dy) {
  const nx = player.x + dx;
  const ny = player.y + dy;
  if (
    nx < 0 || nx >= GRID_SIZE ||
    ny < 0 || ny >= GRID_SIZE ||
    !canMove(map[ny][nx])
  ) return;                                                // Avbryt om ulovlig flytt

  map[player.y][player.x] = 'Empty';                       // Tøm gammel posisjon
  player.x = nx;                                           // Oppdater x
  player.y = ny;                                           // Oppdater y
  map[ny][nx] = 'Player';                                  // Merk ny posisjon
  notify(false);                                           // Send oppdatering
}

/**
 * Plasserer en bombe og sprenger etter 1s i radius 5:
 * - stopper ved faste vegger
 * - ødelegger sprengbare vegger
 * - setter gameOver hvis spilleren treffes
 */
export async function placeBomb(id) {
  const bx = player.x, by = player.y;
  map[by][bx] = 'Bomb';                                    // Sett bombe
  notify(false);                                           // Oppdater UI

  setTimeout(() => {
    let death = false;
    if (bx === player.x && by === player.y) death = true;  // Spilleren står på bomba?
    map[by][bx] = 'Explosion';                             // Eksplosjonssenter

    // Eksplosjon i fire retninger
    [[1,0],[-1,0],[0,1],[0,-1]].forEach(([dx, dy]) => {
      for (let i = 1; i <= 5; i++) {
        const x = bx + dx * i, y = by + dy * i;
        const cell = map[y]?.[x];
        if (!cell || cell === 'Wall') break;               // Stopp ved indestruktibel vegg
        if (x === player.x && y === player.y) death = true; // Treffer spiller?
        map[y][x] = 'Explosion';                          // Sett eksplosjon
        if (cell === 'DestructibleWall') break;           // Ødelegg og stopp
      }
    });

    notify(death);                                         // Oppdater med eventuell død

    // Rydd eksplosjoner etter 300 ms
    setTimeout(() => {
      for (let y = 0; y < GRID_SIZE; y++) {
        for (let x = 0; x < GRID_SIZE; x++) {
          if (map[y][x] === 'Explosion') {
            map[y][x] = 'Empty';                          // Fjern eksplosjonsmarkering
          }
        }
      }
      notify(death);                                       // Endelig oppdatering
    }, 300);
  }, 1000);
}

/** Stub for å bli med i spill – returnerer suksess uten å gjøre noe */
export async function joinGame(playerId) {
  console.log('Mock joinGame:', playerId);
  return { success: true, playerId };
}

/*
Denne filen, gameApi.js, fungerer som en mock-backend for frontend-spillet. 
Den genererer et Bomberman-brett med faste og sprengbare vegger, 
holder oversikt over spillerens posisjon, og tilbyr tre hoved-APIer:
- subscribeGameState: abonner på kart-oppdateringer og gameOver-flag
- movePlayer: flytt spilleren om neste rute er ledig
- placeBomb: plasser en bombe og simuler eksplosjon med ødeleggelse og dødsutfall

Frontend-komponenter bruker disse funksjonene til å vise et fungerende spillmiljø 
uten en ekte backend-server.
*/
