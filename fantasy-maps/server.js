const express = require('express');
const app = express();
const PORT = process.env.PORT || 3000;

// ─── Seeded random for deterministic maps ────────────────────────────────────
function seededRandom(seed) {
  let s = seed;
  return function () {
    s = (s * 16807 + 0) % 2147483647;
    return (s - 1) / 2147483646;
  };
}

// ─── Color palette (from landscape_bg_soft.png) ─────────────────────────────
const P = {
  skyTop: '#b8e6d0',
  skyBot: '#d4efe3',
  mountain: ['#9e9689', '#8a8279', '#b0a99e'],
  snow: '#f0ede8',
  tree: ['#6b9e4a', '#5a8c3e', '#4a7a32'],
  ground: ['#6a9e50', '#5c8e44', '#4e8038'],
  path: ['#c4a66a', '#a08050'],
  water: ['#7bc5d4', '#5ab0c0', '#4a9fb0'],
  wood: ['#8b7355', '#6b5b45'],
  stone: ['#7a7268', '#5e564e'],
  cloud: 'rgba(255,255,255,0.55)',
  fire: ['#e8a030', '#d45020', '#f0c848'],
  magic: ['#c0a0f0', '#a080d8', '#d8c0ff'],
  dark: ['#3a4a30', '#2e3e28', '#4a5a3e'],
  fog: 'rgba(200,210,200,0.35)',
};

const W = 960, H = 640;

// ─── SVG helper builders ─────────────────────────────────────────────────────

function svgOpen() {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" width="100%" height="100%">`;
}

function skyGradient(id, topColor = P.skyTop, botColor = P.skyBot) {
  return `<defs><linearGradient id="${id}" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0%" stop-color="${topColor}"/>
    <stop offset="100%" stop-color="${botColor}"/>
  </linearGradient></defs>
  <rect width="${W}" height="${H}" fill="url(#${id})"/>`;
}

function cloud(cx, cy, rx, ry) {
  return `<ellipse cx="${cx}" cy="${cy}" rx="${rx}" ry="${ry}" fill="${P.cloud}"/>`;
}

function clouds(rand, count = 5) {
  let s = '';
  for (let i = 0; i < count; i++) {
    const cx = rand() * W;
    const cy = 30 + rand() * 70;
    s += cloud(cx, cy, 40 + rand() * 50, 14 + rand() * 12);
    s += cloud(cx + 30, cy - 5, 30 + rand() * 30, 10 + rand() * 8);
  }
  return s;
}

function mountain(cx, baseY, w, h, color, snowCap = true) {
  const peak = baseY - h;
  let s = `<polygon points="${cx - w / 2},${baseY} ${cx},${peak} ${cx + w / 2},${baseY}" fill="${color}"/>`;
  if (snowCap) {
    const sh = h * 0.2;
    const sw = w * 0.15;
    s += `<polygon points="${cx - sw},${peak + sh} ${cx},${peak} ${cx + sw},${peak + sh}" fill="${P.snow}"/>`;
  }
  return s;
}

function mountainRange(rand, baseY, count, minH, maxH, colors, snow = true) {
  let s = '';
  for (let i = 0; i < count; i++) {
    const cx = (W / (count - 1 || 1)) * i + rand() * 60 - 30;
    const h = minH + rand() * (maxH - minH);
    const w = h * (1.2 + rand() * 0.8);
    s += mountain(cx, baseY, w, h, colors[i % colors.length], snow);
  }
  return s;
}

function tree(cx, cy, scale, color) {
  const r = 12 * scale;
  return `<circle cx="${cx}" cy="${cy}" r="${r}" fill="${color}"/>
    <circle cx="${cx - r * 0.5}" cy="${cy + r * 0.3}" r="${r * 0.8}" fill="${color}"/>
    <circle cx="${cx + r * 0.5}" cy="${cy + r * 0.3}" r="${r * 0.85}" fill="${color}"/>
    <rect x="${cx - 2 * scale}" y="${cy + r * 0.5}" width="${4 * scale}" height="${8 * scale}" fill="${P.wood[0]}" rx="1"/>`;
}

function treeCluster(rand, x, y, count, spread, scaleBase) {
  let s = '';
  for (let i = 0; i < count; i++) {
    const tx = x + (rand() - 0.5) * spread;
    const ty = y + (rand() - 0.5) * spread * 0.5;
    const sc = scaleBase + rand() * 0.5;
    s += tree(tx, ty, sc, P.tree[Math.floor(rand() * P.tree.length)]);
  }
  return s;
}

function hill(cx, cy, rx, ry, color) {
  return `<ellipse cx="${cx}" cy="${cy}" rx="${rx}" ry="${ry}" fill="${color}"/>`;
}

function hills(rand, baseY, count) {
  let s = '';
  for (let i = 0; i < count; i++) {
    const cx = (W / count) * i + rand() * 120;
    const rx = 120 + rand() * 160;
    const ry = 40 + rand() * 40;
    s += hill(cx, baseY, rx, ry, P.ground[i % P.ground.length]);
  }
  return s;
}

function pathCurve(points, color = P.path[0], width = 12) {
  if (points.length < 2) return '';
  let d = `M${points[0][0]},${points[0][1]}`;
  for (let i = 1; i < points.length; i++) {
    const prev = points[i - 1];
    const cur = points[i];
    const cpx1 = prev[0] + (cur[0] - prev[0]) * 0.4;
    const cpy1 = prev[1];
    const cpx2 = prev[0] + (cur[0] - prev[0]) * 0.6;
    const cpy2 = cur[1];
    d += ` C${cpx1},${cpy1} ${cpx2},${cpy2} ${cur[0]},${cur[1]}`;
  }
  return `<path d="${d}" fill="none" stroke="${color}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round" opacity="0.85"/>`;
}

function river(points, width = 20) {
  return pathCurve(points, P.water[0], width)
    + pathCurve(points.map(([x, y]) => [x, y]), P.water[1], width * 0.6);
}

function lake(cx, cy, rx, ry) {
  return `<ellipse cx="${cx}" cy="${cy}" rx="${rx}" ry="${ry}" fill="${P.water[0]}" opacity="0.8"/>
    <ellipse cx="${cx}" cy="${cy}" rx="${rx * 0.85}" ry="${ry * 0.8}" fill="${P.water[1]}" opacity="0.5"/>`;
}

function hut(cx, cy, scale = 1) {
  const w = 24 * scale, h = 18 * scale, rh = 12 * scale;
  return `<rect x="${cx - w / 2}" y="${cy - h}" width="${w}" height="${h}" fill="${P.wood[0]}" rx="2"/>
    <polygon points="${cx - w / 2 - 4 * scale},${cy - h} ${cx},${cy - h - rh} ${cx + w / 2 + 4 * scale},${cy - h}" fill="${P.wood[1]}"/>`;
}

function tower(cx, cy, scale = 1) {
  const w = 16 * scale, h = 50 * scale;
  return `<rect x="${cx - w / 2}" y="${cy - h}" width="${w}" height="${h}" fill="${P.stone[0]}" rx="2"/>
    <rect x="${cx - w / 2 - 3 * scale}" y="${cy - h}" width="${w + 6 * scale}" height="${6 * scale}" fill="${P.stone[1]}" rx="1"/>
    <polygon points="${cx - w / 2 - 2 * scale},${cy - h} ${cx},${cy - h - 14 * scale} ${cx + w / 2 + 2 * scale},${cy - h}" fill="${P.wood[1]}"/>`;
}

function campfire(cx, cy, scale = 1) {
  return `<ellipse cx="${cx}" cy="${cy}" rx="${6 * scale}" ry="${3 * scale}" fill="${P.stone[1]}"/>
    <ellipse cx="${cx}" cy="${cy - 4 * scale}" rx="${4 * scale}" ry="${7 * scale}" fill="${P.fire[0]}" opacity="0.8"/>
    <ellipse cx="${cx}" cy="${cy - 6 * scale}" rx="${2.5 * scale}" ry="${5 * scale}" fill="${P.fire[1]}" opacity="0.7"/>
    <ellipse cx="${cx}" cy="${cy - 8 * scale}" rx="${1.5 * scale}" ry="${3 * scale}" fill="${P.fire[2]}" opacity="0.6"/>`;
}

function ruins(cx, cy, rand, count = 4) {
  let s = '';
  for (let i = 0; i < count; i++) {
    const rx = cx + (rand() - 0.5) * 80;
    const ry = cy + (rand() - 0.5) * 30;
    const w = 8 + rand() * 12;
    const h = 20 + rand() * 30;
    s += `<rect x="${rx}" y="${ry - h}" width="${w}" height="${h}" fill="${P.stone[0]}" rx="1" opacity="0.8"
      transform="rotate(${(rand() - 0.5) * 15}, ${rx + w / 2}, ${ry})"/>`;
  }
  return s;
}

function cave(cx, cy, scale = 1) {
  const w = 50 * scale, h = 35 * scale;
  return `<ellipse cx="${cx}" cy="${cy}" rx="${w}" ry="${h}" fill="${P.stone[1]}"/>
    <ellipse cx="${cx}" cy="${cy + 2 * scale}" rx="${w * 0.75}" ry="${h * 0.7}" fill="#2a2420"/>
    <ellipse cx="${cx}" cy="${cy + 4 * scale}" rx="${w * 0.5}" ry="${h * 0.45}" fill="#1a1610"/>`;
}

function bridge(x1, y, x2, color = P.wood[0]) {
  const w = x2 - x1;
  return `<rect x="${x1}" y="${y - 4}" width="${w}" height="${8}" fill="${color}" rx="3"/>
    <rect x="${x1}" y="${y - 8}" width="${6}" height="${16}" fill="${P.wood[1]}" rx="1"/>
    <rect x="${x2 - 6}" y="${y - 8}" width="${6}" height="${16}" fill="${P.wood[1]}" rx="1"/>
    <line x1="${x1 + 3}" y1="${y - 8}" x2="${x2 - 3}" y2="${y - 8}" stroke="${P.wood[1]}" stroke-width="2"/>`;
}

function castle(cx, cy, scale = 1) {
  const bw = 60 * scale, bh = 40 * scale;
  let s = `<rect x="${cx - bw / 2}" y="${cy - bh}" width="${bw}" height="${bh}" fill="${P.stone[0]}" rx="2"/>`;
  // battlements
  for (let i = 0; i < 5; i++) {
    const bx = cx - bw / 2 + i * (bw / 5) + 2 * scale;
    s += `<rect x="${bx}" y="${cy - bh - 8 * scale}" width="${8 * scale}" height="${8 * scale}" fill="${P.stone[0]}" rx="1"/>`;
  }
  // central tower
  s += `<rect x="${cx - 8 * scale}" y="${cy - bh - 30 * scale}" width="${16 * scale}" height="${30 * scale}" fill="${P.stone[1]}" rx="2"/>`;
  s += `<polygon points="${cx - 10 * scale},${cy - bh - 30 * scale} ${cx},${cy - bh - 44 * scale} ${cx + 10 * scale},${cy - bh - 30 * scale}" fill="${P.wood[1]}"/>`;
  // flag
  s += `<line x1="${cx}" y1="${cy - bh - 44 * scale}" x2="${cx}" y2="${cy - bh - 54 * scale}" stroke="${P.wood[1]}" stroke-width="2"/>`;
  s += `<polygon points="${cx},${cy - bh - 54 * scale} ${cx + 14 * scale},${cy - bh - 50 * scale} ${cx},${cy - bh - 46 * scale}" fill="#c04040"/>`;
  // gate
  s += `<rect x="${cx - 7 * scale}" y="${cy - 16 * scale}" width="${14 * scale}" height="${16 * scale}" fill="#3a3228" rx="7"/>`;
  return s;
}

function dangerMarker(cx, cy, scale = 1) {
  const s = 10 * scale;
  return `<polygon points="${cx},${cy - s * 1.5} ${cx - s},${cy + s * 0.5} ${cx + s},${cy + s * 0.5}" fill="#d04040" opacity="0.85"/>
    <text x="${cx}" y="${cy + 2}" text-anchor="middle" font-size="${10 * scale}" fill="white" font-weight="bold">!</text>`;
}

function magicGlow(cx, cy, r, rand) {
  let s = '';
  for (let i = 0; i < 3; i++) {
    const ox = (rand() - 0.5) * r;
    const oy = (rand() - 0.5) * r;
    s += `<circle cx="${cx + ox}" cy="${cy + oy}" r="${r * (0.3 + rand() * 0.4)}" fill="${P.magic[i % P.magic.length]}" opacity="${0.2 + rand() * 0.3}"/>`;
  }
  return s;
}

function twistedTree(cx, cy, scale, color) {
  const r = 14 * scale;
  return `<path d="M${cx},${cy + r} Q${cx - 6 * scale},${cy} ${cx - 10 * scale},${cy - r * 0.5}
    Q${cx - 4 * scale},${cy - r} ${cx},${cy - r * 1.2}
    Q${cx + 5 * scale},${cy - r} ${cx + 10 * scale},${cy - r * 0.5}
    Q${cx + 6 * scale},${cy} ${cx},${cy + r}" fill="${color}" opacity="0.9"/>
    <rect x="${cx - 2.5 * scale}" y="${cy + r * 0.3}" width="${5 * scale}" height="${12 * scale}" fill="${P.wood[1]}" rx="1"/>`;
}

function fogPatches(rand, baseY, count) {
  let s = '';
  for (let i = 0; i < count; i++) {
    const cx = rand() * W;
    const cy = baseY + (rand() - 0.5) * 60;
    s += `<ellipse cx="${cx}" cy="${cy}" rx="${60 + rand() * 80}" ry="${15 + rand() * 15}" fill="${P.fog}"/>`;
  }
  return s;
}

function mapTitle(text) {
  return `<rect x="${W / 2 - 160}" y="12" width="320" height="44" rx="10" fill="rgba(255,255,255,0.65)"/>
    <text x="${W / 2}" y="42" text-anchor="middle" font-family="Georgia, serif" font-size="22" fill="#3a3228" font-weight="bold">${text}</text>`;
}

function groundPlane(y, color) {
  return `<rect x="0" y="${y}" width="${W}" height="${H - y}" fill="${color}"/>`;
}

// ─── Map generators ──────────────────────────────────────────────────────────

const maps = [
  // 1 — The Green Vale
  function (rand) {
    let s = skyGradient('sky1');
    s += clouds(rand, 6);
    s += mountainRange(rand, 220, 6, 80, 160, P.mountain);
    s += groundPlane(350, P.ground[0]);
    s += hills(rand, 360, 5);
    s += pathCurve([[80, 600], [200, 520], [400, 480], [550, 440], [700, 460], [880, 400]], P.path[0], 14);
    s += hut(200, 510, 1.1);
    s += hut(240, 515, 0.9);
    s += hut(170, 520, 0.8);
    s += campfire(210, 540);
    s += treeCluster(rand, 600, 380, 8, 120, 1);
    s += treeCluster(rand, 100, 400, 5, 80, 0.8);
    s += treeCluster(rand, 800, 350, 6, 100, 1.1);
    s += treeCluster(rand, 450, 560, 4, 60, 0.7);
    s += mapTitle('The Green Vale');
    return s;
  },
  // 2 — Whispering Woods
  function (rand) {
    let s = skyGradient('sky2');
    s += clouds(rand, 4);
    s += mountainRange(rand, 230, 4, 60, 120, P.mountain);
    s += groundPlane(320, P.ground[1]);
    s += hills(rand, 330, 4);
    // Dense forest
    for (let i = 0; i < 18; i++) {
      const tx = 50 + rand() * (W - 100);
      const ty = 300 + rand() * 250;
      s += tree(tx, ty, 0.9 + rand() * 0.8, P.tree[Math.floor(rand() * 3)]);
    }
    // Branching paths
    s += pathCurve([[0, 500], [150, 470], [300, 440], [400, 420]], P.path[0], 10);
    s += pathCurve([[400, 420], [550, 380], [700, 340], [850, 360]], P.path[0], 10);
    s += pathCurve([[400, 420], [500, 480], [600, 530], [750, 560]], P.path[1], 8);
    // Hidden clearing
    s += `<ellipse cx="730" cy="350" rx="50" ry="30" fill="${P.ground[0]}" opacity="0.6"/>`;
    s += campfire(730, 355, 0.8);
    s += treeCluster(rand, 730, 330, 6, 80, 0.6);
    s += mapTitle('Whispering Woods');
    return s;
  },
  // 3 — Stone Bridge Crossing
  function (rand) {
    let s = skyGradient('sky3');
    s += clouds(rand, 5);
    s += mountainRange(rand, 200, 5, 80, 150, P.mountain);
    s += groundPlane(340, P.ground[0]);
    s += hills(rand, 350, 4);
    // River
    s += river([[0, 300], [200, 350], [400, 380], [480, 400], [560, 380], [750, 340], [W, 310]], 24);
    // Bridge
    s += bridge(440, 390, 560, P.wood[0]);
    // Paths on either side
    s += pathCurve([[80, 580], [200, 500], [350, 440], [460, 395]], P.path[0], 12);
    s += pathCurve([[540, 385], [650, 430], [780, 480], [900, 560]], P.path[0], 12);
    s += treeCluster(rand, 150, 420, 5, 80, 1);
    s += treeCluster(rand, 800, 400, 5, 80, 1);
    s += hut(120, 560, 0.9);
    s += hut(850, 540, 0.9);
    s += mapTitle('Stone Bridge Crossing');
    return s;
  },
  // 4 — Ruins of the Old Keep
  function (rand) {
    let s = skyGradient('sky4', '#c0d8c4', '#d8e8da');
    s += clouds(rand, 3);
    s += mountainRange(rand, 220, 5, 70, 140, P.mountain);
    s += groundPlane(340, P.ground[1]);
    s += hills(rand, 350, 5);
    // Path to ruins
    s += pathCurve([[80, 600], [250, 520], [450, 430], [600, 400], [700, 380]], P.path[1], 12);
    // Ruins
    s += ruins(640, 390, rand, 6);
    // Crumbling tower
    s += tower(700, 385, 0.9);
    // Overgrown vines (green circles on ruins)
    for (let i = 0; i < 5; i++) {
      s += `<circle cx="${620 + rand() * 100}" cy="${360 + rand() * 30}" r="${4 + rand() * 6}" fill="${P.tree[1]}" opacity="0.6"/>`;
    }
    // Treasure marker
    s += `<circle cx="700" cy="360" r="8" fill="${P.fire[2]}" opacity="0.8"/>`;
    s += `<text x="700" y="364" text-anchor="middle" font-size="10" fill="#6b5b45" font-weight="bold">★</text>`;
    s += treeCluster(rand, 200, 450, 6, 100, 1);
    s += treeCluster(rand, 850, 420, 4, 60, 0.8);
    s += mapTitle('Ruins of the Old Keep');
    return s;
  },
  // 5 — The Mountain Pass
  function (rand) {
    let s = skyGradient('sky5', '#a8d8c0', '#c8e4d4');
    s += clouds(rand, 4);
    // Tall peaks
    s += mountain(200, 300, 350, 260, P.mountain[0]);
    s += mountain(750, 300, 380, 280, P.mountain[1]);
    s += mountain(480, 320, 200, 200, P.mountain[2], true);
    // Snow caps
    s += `<polygon points="120,50 200,40 280,50 200,110" fill="${P.snow}" opacity="0.6"/>`;
    s += `<polygon points="670,30 750,20 830,30 750,100" fill="${P.snow}" opacity="0.6"/>`;
    s += groundPlane(420, P.ground[2]);
    // Narrow pass between peaks
    s += `<rect x="380" y="300" width="200" height="120" fill="${P.ground[1]}"/>`;
    s += hills(rand, 430, 3);
    // Path through pass
    s += pathCurve([[80, 580], [200, 500], [350, 430], [480, 380], [580, 360], [700, 400], [880, 500]], P.path[0], 10);
    s += treeCluster(rand, 150, 460, 3, 50, 0.7);
    s += treeCluster(rand, 800, 460, 3, 50, 0.7);
    s += dangerMarker(480, 350, 1.2);
    s += mapTitle('The Mountain Pass');
    return s;
  },
  // 6 — Dragon's Hollow
  function (rand) {
    let s = skyGradient('sky6', '#c8c0a8', '#d8d4c0');
    s += clouds(rand, 3);
    s += mountainRange(rand, 220, 4, 100, 180, ['#7a7268', '#6a6258', '#8a827a']);
    s += groundPlane(360, '#8a7e60');
    s += hills(rand, 370, 4);
    // Scorched earth patches
    for (let i = 0; i < 6; i++) {
      const sx = 300 + rand() * 350;
      const sy = 380 + rand() * 100;
      s += `<ellipse cx="${sx}" cy="${sy}" rx="${20 + rand() * 25}" ry="${8 + rand() * 10}" fill="#5a4a30" opacity="0.5"/>`;
    }
    // Cave entrance
    s += cave(500, 380, 1.2);
    // Path to cave
    s += pathCurve([[80, 580], [200, 530], [350, 460], [450, 400], [490, 385]], P.path[1], 12);
    // Danger markers
    s += dangerMarker(380, 430, 1);
    s += dangerMarker(550, 410, 1);
    s += dangerMarker(460, 350, 1.3);
    // Scattered bones (small white shapes)
    for (let i = 0; i < 5; i++) {
      const bx = 420 + rand() * 160;
      const by = 420 + rand() * 60;
      s += `<line x1="${bx}" y1="${by}" x2="${bx + 8}" y2="${by + 4}" stroke="#e8e0d0" stroke-width="2" stroke-linecap="round"/>`;
    }
    s += treeCluster(rand, 100, 440, 4, 70, 0.8);
    s += treeCluster(rand, 850, 430, 3, 50, 0.7);
    s += campfire(300, 510, 1.2);
    s += mapTitle("Dragon's Hollow");
    return s;
  },
  // 7 — The Enchanted Lake
  function (rand) {
    let s = skyGradient('sky7', '#b0d8e0', '#d0e8e8');
    s += clouds(rand, 5);
    s += mountainRange(rand, 210, 5, 60, 130, P.mountain);
    s += groundPlane(340, P.ground[0]);
    s += hills(rand, 350, 5);
    // Large lake
    s += lake(480, 430, 160, 80);
    // Island in lake
    s += `<ellipse cx="480" cy="420" rx="35" ry="18" fill="${P.ground[0]}"/>`;
    s += tree(480, 410, 0.8, P.tree[0]);
    // Magic glow spots
    s += magicGlow(420, 440, 30, rand);
    s += magicGlow(540, 420, 25, rand);
    s += magicGlow(480, 460, 20, rand);
    // Glow on water surface
    for (let i = 0; i < 8; i++) {
      const gx = 380 + rand() * 200;
      const gy = 400 + rand() * 60;
      s += `<circle cx="${gx}" cy="${gy}" r="${2 + rand() * 3}" fill="${P.magic[2]}" opacity="${0.3 + rand() * 0.3}"/>`;
    }
    // Path around lake
    s += pathCurve([[80, 550], [200, 480], [300, 430], [350, 400]], P.path[0], 10);
    s += pathCurve([[610, 400], [700, 440], [800, 500], [900, 560]], P.path[0], 10);
    s += treeCluster(rand, 100, 420, 5, 80, 1);
    s += treeCluster(rand, 850, 400, 5, 80, 1);
    s += mapTitle('The Enchanted Lake');
    return s;
  },
  // 8 — Knight's Watchtower
  function (rand) {
    let s = skyGradient('sky8');
    s += clouds(rand, 5);
    s += mountainRange(rand, 220, 5, 70, 140, P.mountain);
    s += groundPlane(350, P.ground[0]);
    s += hills(rand, 360, 5);
    // Tall watchtower
    s += tower(500, 380, 1.5);
    // Patrol path loop
    s += pathCurve([[300, 520], [400, 460], [480, 400], [560, 400], [640, 460], [700, 520]], P.path[0], 10);
    s += pathCurve([[700, 520], [680, 560], [600, 580], [480, 580], [380, 560], [300, 520]], P.path[0], 10);
    // Entry path
    s += pathCurve([[80, 600], [200, 560], [300, 520]], P.path[0], 12);
    // Campfires at patrol points
    s += campfire(320, 525, 0.8);
    s += campfire(680, 525, 0.8);
    s += campfire(500, 585, 0.8);
    s += treeCluster(rand, 150, 440, 5, 90, 1);
    s += treeCluster(rand, 820, 430, 5, 80, 0.9);
    s += hut(160, 580, 0.8);
    s += mapTitle("Knight's Watchtower");
    return s;
  },
  // 9 — The Dark Thicket
  function (rand) {
    let s = skyGradient('sky9', '#7a9a78', '#9ab898');
    s += clouds(rand, 2);
    s += mountainRange(rand, 230, 4, 60, 120, ['#6a6660', '#585450', '#787470']);
    s += groundPlane(330, P.dark[0]);
    s += hills(rand, 340, 5);
    // Twisted trees
    for (let i = 0; i < 14; i++) {
      const tx = 50 + rand() * (W - 100);
      const ty = 310 + rand() * 240;
      s += twistedTree(tx, ty, 0.8 + rand() * 0.7, P.dark[Math.floor(rand() * 3)]);
    }
    // Fog patches
    s += fogPatches(rand, 420, 8);
    // Fork in road
    s += pathCurve([[80, 580], [200, 520], [350, 460], [480, 420]], P.path[1], 10);
    s += pathCurve([[480, 420], [600, 380], [750, 350], [900, 330]], P.path[1], 8);
    s += pathCurve([[480, 420], [550, 480], [650, 540], [800, 590]], P.path[1], 8);
    // Question mark at fork
    s += `<circle cx="480" cy="400" r="12" fill="rgba(255,255,255,0.6)"/>`;
    s += `<text x="480" y="405" text-anchor="middle" font-size="16" fill="#3a3228" font-weight="bold">?</text>`;
    s += fogPatches(rand, 380, 4);
    s += mapTitle('The Dark Thicket');
    return s;
  },
  // 10 — Summit of Crowns
  function (rand) {
    let s = skyGradient('sky10', '#a0c8d8', '#c8e0e8');
    s += clouds(rand, 6);
    // Grand mountain backdrop
    s += mountain(480, 280, 500, 260, P.mountain[0]);
    s += mountain(250, 300, 300, 200, P.mountain[1]);
    s += mountain(720, 300, 320, 210, P.mountain[2]);
    s += groundPlane(400, P.ground[0]);
    s += hills(rand, 410, 5);
    // Castle at the summit
    s += castle(480, 300, 1.3);
    // Converging paths
    s += pathCurve([[80, 600], [200, 540], [350, 460], [450, 380], [478, 320]], P.path[0], 12);
    s += pathCurve([[880, 600], [760, 530], [630, 450], [530, 370], [500, 310]], P.path[0], 12);
    s += pathCurve([[480, 640], [480, 580], [480, 500], [480, 400], [480, 320]], P.path[1], 10);
    s += treeCluster(rand, 120, 480, 6, 100, 1);
    s += treeCluster(rand, 840, 470, 6, 100, 1);
    // Celebratory flags along path
    for (let i = 0; i < 5; i++) {
      const fx = 350 + i * 60;
      const fy = 440 - i * 25;
      s += `<line x1="${fx}" y1="${fy}" x2="${fx}" y2="${fy - 18}" stroke="${P.wood[1]}" stroke-width="2"/>`;
      s += `<polygon points="${fx},${fy - 18} ${fx + 10},${fy - 14} ${fx},${fy - 10}" fill="${i % 2 === 0 ? '#c04040' : '#4060c0'}"/>`;
    }
    s += mapTitle('Summit of Crowns');
    return s;
  },
];

// ─── Map metadata ────────────────────────────────────────────────────────────
const mapNames = [
  'The Green Vale',
  'Whispering Woods',
  'Stone Bridge Crossing',
  'Ruins of the Old Keep',
  'The Mountain Pass',
  "Dragon's Hollow",
  'The Enchanted Lake',
  "Knight's Watchtower",
  'The Dark Thicket',
  'Summit of Crowns',
];

const mapDescriptions = [
  'A peaceful starting meadow with a gentle winding path and a small village camp.',
  'A dense forest with branching paths leading to a hidden clearing.',
  'A wide river with a sturdy stone bridge connecting split paths.',
  'Crumbling ruins of an ancient tower, overgrown and hiding treasure.',
  'A treacherous narrow path winding between towering peaks.',
  'A foreboding cave entrance surrounded by scorched earth and danger.',
  'A serene lake with a mysterious island and magical glow spots.',
  'A tall watchtower with a patrol loop and campfires at each post.',
  'Twisted trees shrouded in fog with a mysterious fork in the road.',
  'The grand summit with a majestic castle and converging paths.',
];

// ─── Shared CSS ──────────────────────────────────────────────────────────────
const CSS = `
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body {
    font-family: Georgia, 'Times New Roman', serif;
    background: #f4f0e8;
    color: #3a3228;
    min-height: 100vh;
  }
  .page { max-width: 1000px; margin: 0 auto; padding: 24px; }
  h1 {
    font-size: 2rem;
    margin-bottom: 8px;
    color: #3a3228;
  }
  .subtitle {
    color: #6b5b45;
    font-style: italic;
    margin-bottom: 32px;
  }
  .map-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 20px;
  }
  .map-card {
    background: white;
    border-radius: 12px;
    overflow: hidden;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    transition: transform 0.2s, box-shadow 0.2s;
    text-decoration: none;
    color: inherit;
    display: block;
  }
  .map-card:hover {
    transform: translateY(-3px);
    box-shadow: 0 6px 20px rgba(0,0,0,0.15);
  }
  .map-card .preview {
    width: 100%;
    aspect-ratio: 3/2;
    display: block;
  }
  .map-card .info {
    padding: 14px 16px;
  }
  .map-card .info h2 {
    font-size: 1rem;
    margin-bottom: 4px;
  }
  .map-card .info p {
    font-size: 0.85rem;
    color: #6b5b45;
    line-height: 1.4;
  }
  .map-card .info .num {
    font-size: 0.75rem;
    color: #a09080;
    text-transform: uppercase;
    letter-spacing: 1px;
    margin-bottom: 2px;
  }
  .map-full {
    background: white;
    border-radius: 12px;
    overflow: hidden;
    box-shadow: 0 2px 12px rgba(0,0,0,0.12);
  }
  .map-full svg { display: block; width: 100%; height: auto; }
  .back-link {
    display: inline-block;
    margin-bottom: 16px;
    color: #6b5b45;
    text-decoration: none;
    font-size: 0.95rem;
  }
  .back-link:hover { text-decoration: underline; }
  .map-desc {
    padding: 16px 20px;
    color: #6b5b45;
    font-style: italic;
    border-top: 1px solid #e8e4dc;
  }
  .nav-row {
    display: flex;
    justify-content: space-between;
    margin-top: 12px;
  }
  .nav-row a {
    color: #6b5b45;
    text-decoration: none;
    font-size: 0.9rem;
  }
  .nav-row a:hover { text-decoration: underline; }
`;

function htmlWrap(title, body) {
  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>${title}</title>
  <style>${CSS}</style>
</head>
<body>${body}</body>
</html>`;
}

function generateMapSvg(id) {
  const rand = seededRandom(id * 7919 + 42);
  const content = maps[id - 1](rand);
  return svgOpen() + content + '</svg>';
}

// ─── Routes ──────────────────────────────────────────────────────────────────

app.get('/', (req, res) => {
  let cards = '';
  for (let i = 1; i <= 10; i++) {
    const svg = generateMapSvg(i);
    cards += `<a class="map-card" href="/map/${i}">
      <div class="preview">${svg}</div>
      <div class="info">
        <div class="num">Map ${i} of 10</div>
        <h2>${mapNames[i - 1]}</h2>
        <p>${mapDescriptions[i - 1]}</p>
      </div>
    </a>`;
  }
  const body = `<div class="page">
    <h1>Fantasy Level Maps</h1>
    <p class="subtitle">10 procedurally-generated storybook maps</p>
    <div class="map-grid">${cards}</div>
  </div>`;
  res.send(htmlWrap('Fantasy Level Maps', body));
});

app.get('/map/:id', (req, res) => {
  const id = parseInt(req.params.id, 10);
  if (isNaN(id) || id < 1 || id > 10) {
    return res.status(404).send(htmlWrap('Not Found', '<div class="page"><h1>Map not found</h1><a class="back-link" href="/">← Back to all maps</a></div>'));
  }
  const svg = generateMapSvg(id);
  const prev = id > 1 ? `<a href="/map/${id - 1}">← ${mapNames[id - 2]}</a>` : '<span></span>';
  const next = id < 10 ? `<a href="/map/${id + 1}">${mapNames[id]} →</a>` : '<span></span>';
  const body = `<div class="page">
    <a class="back-link" href="/">← All Maps</a>
    <div class="map-full">${svg}
      <div class="map-desc">${mapDescriptions[id - 1]}</div>
    </div>
    <div class="nav-row">${prev}${next}</div>
  </div>`;
  res.send(htmlWrap(mapNames[id - 1] + ' — Fantasy Maps', body));
});

app.listen(PORT, () => {
  console.log(`Fantasy Maps running at http://localhost:${PORT}/`);
});
