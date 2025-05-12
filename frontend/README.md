# Bomberman

A Bomberman-style game split into two parts:

1. **Backend** (C#)  
2. **Frontend** (React + Vite + CSS-Modules)

---

## 🚀 Frontend

### Technologies
- **React 18**  
- **Vite** (dev server & build)  
- **CSS-Modules** + CSS custom properties  
- **Mock API** (can be swapped out for real message-bus integration)

### Features
- **Dynamic 32×32 grid** rendered via CSS Grid  
- **Tile component** with distinct colors for:
  - Empty space  
  - Indestructible wall  
  - Destructible wall  
  - Player  
  - Bomb  
  - Explosion  
- **Keyboard controls**:  
  - W/A/S/D to move  
  - Space to place a bomb  
  - P to restart  
- **Button controls** in the sidebar (Up, Down, Left, Right, Bomb)  
- **Start screen** with transparent logo and “Start Game” button  
- **Game Over overlay** with “Restart” button  
- **Bomb countdown** displayed above the grid  
- **Color legend** in the sidebar explaining each tile type  
- **Mock API** in `src/services/gameApi.js` that:
  - Generates a map with open start zones  
  - Handles `movePlayer` and `placeBomb` logic (radius-5 explosions, wall destruction, player death)  
  - Publishes `map` + `gameOver` via a simple subscription interface  

### Setup & Run

1. **Clone the repository**  
   ```bash
   git clone https://github.com/Erikg-kodehode/Bomberman.git
   cd Bomberman


```cd frontend
npm install
npm run dev

```npm run build

Output will be in frontend/dist.

Deploy

Vercel: set Root Directory to frontend, Build Command to npm run build, Output Directory to dist.

GitHub Pages: use gh-pages to publish frontend/dist.

frontend/
├─ public/
│  └─ images/
│     └─ Logo.png            # Transparent logo for the start screen
├─ src/
│  ├─ components/
│  │   ├─ Grid/              # Grid component + CSS-Module
│  │   │   ├─ Grid.jsx
│  │   │   └─ Grid.module.css
│  │   └─ Tile/              # Tile component + CSS-Module
│  │       ├─ Tile.jsx
│  │       └─ Tile.module.css
│  ├─ context/
│  │    └─ GameContext.jsx   # React Context + useGame hook
│  ├─ services/
│  │    └─ gameApi.js        # Mock API for frontend development
│  ├─ App.jsx                # Main React component
│  ├─ main.jsx               # React entrypoint
│  └─ global.css             # Reset, layout, theme variables
└─ package.json
