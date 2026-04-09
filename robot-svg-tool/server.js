const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 3010;
const SVG_DIR = path.join(__dirname, 'public', 'svg');
const GEN_DIR = path.join(SVG_DIR, 'generated');

const ROBOT_COLORS = [
  { id: 1, name: 'Purple', hex: '#7B2FBE' },
  { id: 2, name: 'Orange', hex: '#F5921B' },
  { id: 3, name: 'Hot Pink', hex: '#E91E8C' },
  { id: 4, name: 'Yellow', hex: '#F5C518' },
  { id: 5, name: 'Blue', hex: '#2E86DE' },
  { id: 6, name: 'Green', hex: '#27AE60' },
  { id: 7, name: 'Red', hex: '#E74C3C' },
  { id: 8, name: 'Teal', hex: '#00BCD4' },
  { id: 9, name: 'Lime', hex: '#8BC34A' },
  { id: 10, name: 'Coral', hex: '#FF7043' },
];

function buildPreviewPage() {
  const cards = ROBOT_COLORS.map(r => `
    <div class="card">
      <div class="svg-frame">
        <img src="/svg/robot/${r.id}" alt="Robot ${r.id}" />
      </div>
      <div class="label">#${r.id} — ${r.name}</div>
      <a href="/svg/robot/${r.id}" target="_blank">Open SVG</a>
    </div>`).join('\n');

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <title>Robot Game Pieces — SVG Preview</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      background: #1a1a2e; color: #eee; padding: 2rem;
    }
    h1 { text-align: center; margin-bottom: .5rem; font-size: 1.8rem; }
    .subtitle { text-align: center; color: #888; margin-bottom: 2rem; font-size: .95rem; }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
      gap: 1.5rem; max-width: 1200px; margin: 0 auto;
    }
    .card {
      background: #16213e; border-radius: 12px; padding: 1rem;
      text-align: center; transition: transform .15s;
    }
    .card:hover { transform: translateY(-4px); }
    .svg-frame {
      background: #0f3460; border-radius: 8px; padding: 1rem;
      display: flex; align-items: center; justify-content: center;
      height: 200px;
    }
    .svg-frame img { max-height: 100%; max-width: 100%; }
    .label { margin-top: .75rem; font-weight: 600; font-size: .95rem; }
    a {
      display: inline-block; margin-top: .5rem; color: #54a0ff;
      text-decoration: none; font-size: .85rem;
    }
    a:hover { text-decoration: underline; }
  </style>
</head>
<body>
  <h1>Robot Game Pieces</h1>
  <p class="subtitle">10 SVG variants — only robots, no spaceships</p>
  <div class="grid">
    ${cards}
  </div>
</body>
</html>`;
}

const GEN_COLORS = ['purple', 'orange', 'pink', 'yellow'];
const GEN_COLOR_HEX = { purple: '#7B2FBE', orange: '#F5921B', pink: '#E91E8C', yellow: '#F5B800' };

function buildGeneratedPage() {
  const sections = GEN_COLORS.map(color => {
    const cards = Array.from({ length: 5 }, (_, i) => {
      const num = i + 1;
      const file = `robot-${color}-${num}`;
      return `
        <div class="card">
          <div class="svg-frame">
            <img src="/generated/${file}" alt="${file}" />
          </div>
          <div class="label">${file}</div>
          <a href="/generated/${file}" target="_blank">Open SVG</a>
        </div>`;
    }).join('\n');

    return `
      <h2 style="border-left:4px solid ${GEN_COLOR_HEX[color]};padding-left:10px;margin:2rem 0 1rem">
        ${color.charAt(0).toUpperCase() + color.slice(1)} Robots
      </h2>
      <div class="grid">${cards}</div>`;
  }).join('\n');

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <title>Generated Robot Pieces — SVG Preview</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      background: linear-gradient(180deg, #B8E6D0 0%, #8FD4A8 40%, #6AAF7B 100%);
      color: #2D5A3D; padding: 2rem; min-height: 100vh;
    }
    h1 { text-align: center; margin-bottom: .3rem; font-size: 1.8rem; }
    .subtitle { text-align: center; color: #4A7A5A; margin-bottom: 1rem; font-size: .95rem; }
    .nav { text-align: center; margin-bottom: 1.5rem; }
    .nav a { color: #2E86DE; margin: 0 .5rem; }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(170px, 1fr));
      gap: 1.2rem; max-width: 1100px; margin: 0 auto;
    }
    .card {
      background: rgba(255,255,255,0.85); border-radius: 14px; padding: 1rem;
      text-align: center; transition: transform .15s; box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }
    .card:hover { transform: translateY(-4px); }
    .svg-frame {
      background: rgba(255,255,255,0.5); border-radius: 8px; padding: .75rem;
      display: flex; align-items: center; justify-content: center; height: 200px;
    }
    .svg-frame img { max-height: 100%; max-width: 100%; }
    .label { margin-top: .6rem; font-weight: 600; font-size: .85rem; color: #333; }
    a { display: inline-block; margin-top: .4rem; color: #2E86DE; text-decoration: none; font-size: .8rem; }
    a:hover { text-decoration: underline; }
    h2 { max-width: 1100px; margin-left: auto; margin-right: auto; }
  </style>
</head>
<body>
  <h1>Generated Robot Pieces</h1>
  <p class="subtitle">20 SVG robot game pieces — purple, orange, pink, yellow</p>
  <div class="nav"><a href="/">Original Robots</a> | <a href="/generated">Generated Robots</a></div>
  ${sections}
</body>
</html>`;
}

const V3_PALETTES = {
  yellow: [
    { name: 'Golden Honey',   hex: '#DCA830' },
    { name: 'Warm Marigold',  hex: '#E0B438' },
    { name: 'Soft Buttercup', hex: '#E4C44A' },
    { name: 'Muted Saffron',  hex: '#D4A030' },
    { name: 'Sunny Wheat',    hex: '#E8BC40' },
  ],
  purple: [
    { name: 'Soft Lavender',  hex: '#8B6CB0' },
    { name: 'Muted Plum',     hex: '#7E5EA0' },
    { name: 'Dusty Violet',   hex: '#9878B8' },
    { name: 'Berry Grape',    hex: '#8860A8' },
    { name: 'Periwinkle',     hex: '#8880C0' },
  ],
  orange: [
    { name: 'Warm Apricot',   hex: '#E09048' },
    { name: 'Soft Tangerine', hex: '#E88050' },
    { name: 'Dusty Peach',    hex: '#D89460' },
    { name: 'Burnt Amber',    hex: '#D48050' },
    { name: 'Coral Sunset',   hex: '#E88858' },
  ],
  pink: [
    { name: 'Soft Rose',      hex: '#D07088' },
    { name: 'Dusty Blush',    hex: '#C87090' },
    { name: 'Warm Peony',     hex: '#D88090' },
    { name: 'Berry Rose',     hex: '#C86088' },
    { name: 'Mauve Blossom',  hex: '#C080A0' },
  ],
};

function buildV3Page() {
  const sections = ['yellow', 'purple', 'orange', 'pink'].map(color => {
    const cards = V3_PALETTES[color].map((p, i) => {
      const num = i + 1;
      const file = `robot-${color}-v3-${num}`;
      return `
        <div class="card">
          <div class="svg-frame">
            <img src="/v3/${file}" alt="${file}" />
          </div>
          <div class="label">${p.name}</div>
          <div class="hex">${p.hex}</div>
          <a href="/v3/${file}" target="_blank">Open SVG</a>
        </div>`;
    }).join('\n');

    return `
      <h2 style="border-left:4px solid ${V3_PALETTES[color][0].hex};padding-left:10px;margin:2rem 0 1rem">
        ${color.charAt(0).toUpperCase() + color.slice(1)}
      </h2>
      <div class="grid">${cards}</div>`;
  }).join('\n');

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <title>V3 Color Options — Robot Pieces</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      background: linear-gradient(180deg, #B8E6D0 0%, #8FD4A8 40%, #6AAF7B 100%);
      color: #2D5A3D; padding: 2rem; min-height: 100vh;
    }
    h1 { text-align: center; margin-bottom: .3rem; font-size: 1.8rem; }
    .subtitle { text-align: center; color: #4A7A5A; margin-bottom: 1rem; font-size: .95rem; }
    .nav { text-align: center; margin-bottom: 1.5rem; }
    .nav a { color: #2E86DE; margin: 0 .5rem; }
    .grid {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 1.2rem; max-width: 950px; margin: 0 auto;
    }
    .card {
      background: rgba(255,255,255,0.85); border-radius: 14px; padding: 1rem;
      text-align: center; transition: transform .15s; box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }
    .card:hover { transform: translateY(-4px); }
    .svg-frame {
      background: rgba(255,255,255,0.5); border-radius: 8px; padding: .75rem;
      display: flex; align-items: center; justify-content: center; height: 220px;
    }
    .svg-frame img { max-height: 100%; max-width: 100%; }
    .label { margin-top: .6rem; font-weight: 600; font-size: .82rem; color: #333; }
    .hex { font-size: .72rem; color: #777; font-family: monospace; }
    a { display: inline-block; margin-top: .3rem; color: #2E86DE; text-decoration: none; font-size: .75rem; }
    a:hover { text-decoration: underline; }
    h2 { max-width: 950px; margin-left: auto; margin-right: auto; }
  </style>
</head>
<body>
  <h1>Color Options — V3</h1>
  <p class="subtitle">Same robot shape (v2-4), 5 color options per family — tuned for fantasy landscape palette</p>
  <div class="nav"><a href="/">Original</a> | <a href="/generated">V1</a> | <a href="/v2">V2</a> | <a href="/v3">V3 Colors</a></div>
  ${sections}
</body>
</html>`;
}

function buildV2Page() {
  const cards = Array.from({ length: 5 }, (_, i) => {
    const num = i + 1;
    const file = `robot-yellow-v2-${num}`;
    return `
      <div class="card">
        <div class="svg-frame">
          <img src="/v2/${file}" alt="${file}" />
        </div>
        <div class="label">${file}</div>
        <a href="/v2/${file}" target="_blank">Open SVG</a>
      </div>`;
  }).join('\n');

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <title>V2 Robot Pieces — Accurate Proportions</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      background: linear-gradient(180deg, #B8E6D0 0%, #8FD4A8 40%, #6AAF7B 100%);
      color: #2D5A3D; padding: 2rem; min-height: 100vh;
    }
    h1 { text-align: center; margin-bottom: .3rem; font-size: 1.8rem; }
    .subtitle { text-align: center; color: #4A7A5A; margin-bottom: 1rem; font-size: .95rem; }
    .nav { text-align: center; margin-bottom: 1.5rem; }
    .nav a { color: #2E86DE; margin: 0 .5rem; }
    .grid {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 1.2rem; max-width: 900px; margin: 0 auto;
    }
    .card {
      background: rgba(255,255,255,0.85); border-radius: 14px; padding: 1rem;
      text-align: center; transition: transform .15s; box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }
    .card:hover { transform: translateY(-4px); }
    .svg-frame {
      background: rgba(255,255,255,0.5); border-radius: 8px; padding: .75rem;
      display: flex; align-items: center; justify-content: center; height: 240px;
    }
    .svg-frame img { max-height: 100%; max-width: 100%; }
    .label { margin-top: .6rem; font-weight: 600; font-size: .8rem; color: #333; }
    a { display: inline-block; margin-top: .4rem; color: #2E86DE; text-decoration: none; font-size: .8rem; }
    a:hover { text-decoration: underline; }
  </style>
</head>
<body>
  <h1>V2 Robot Pieces — Yellow</h1>
  <p class="subtitle">Accurate proportions from physical piece reference, flat cartoon style</p>
  <div class="nav"><a href="/">Original</a> | <a href="/generated">V1 Generated</a> | <a href="/v2">V2 Accurate</a></div>
  <div class="grid">${cards}</div>
</body>
</html>`;
}

const server = http.createServer((req, res) => {
  // Route: /v3/:name (serve v3 color variant SVGs)
  const v3Match = req.url.match(/^\/v3\/(robot-(?:purple|orange|pink|yellow)-v3-\d+)$/);
  if (v3Match) {
    const filePath = path.join(GEN_DIR, `${v3Match[1]}.svg`);
    try {
      const svg = fs.readFileSync(filePath, 'utf8');
      res.writeHead(200, { 'Content-Type': 'image/svg+xml' });
      return res.end(svg);
    } catch {
      res.writeHead(404, { 'Content-Type': 'text/plain' });
      return res.end(`${v3Match[1]}.svg not found`);
    }
  }

  // Route: /v3 (color options page)
  if (req.url === '/v3' || req.url === '/v3/') {
    res.writeHead(200, { 'Content-Type': 'text/html' });
    return res.end(buildV3Page());
  }

  // Route: /v2/:name (serve v2 SVGs)
  const v2Match = req.url.match(/^\/v2\/(robot-(?:purple|orange|pink|yellow)-v2-\d+)$/);
  if (v2Match) {
    const filePath = path.join(GEN_DIR, `${v2Match[1]}.svg`);
    try {
      const svg = fs.readFileSync(filePath, 'utf8');
      res.writeHead(200, { 'Content-Type': 'image/svg+xml' });
      return res.end(svg);
    } catch {
      res.writeHead(404, { 'Content-Type': 'text/plain' });
      return res.end(`${v2Match[1]}.svg not found`);
    }
  }

  // Route: /v2 (v2 preview page)
  if (req.url === '/v2' || req.url === '/v2/') {
    res.writeHead(200, { 'Content-Type': 'text/html' });
    return res.end(buildV2Page());
  }

  // Route: /generated/:name (serve generated SVGs)
  const genMatch = req.url.match(/^\/generated\/(robot-(?:purple|orange|pink|yellow)-\d+)$/);
  if (genMatch) {
    const filePath = path.join(GEN_DIR, `${genMatch[1]}.svg`);
    try {
      const svg = fs.readFileSync(filePath, 'utf8');
      res.writeHead(200, { 'Content-Type': 'image/svg+xml' });
      return res.end(svg);
    } catch {
      res.writeHead(404, { 'Content-Type': 'text/plain' });
      return res.end(`${genMatch[1]}.svg not found`);
    }
  }

  // Route: /generated (preview page for generated robots)
  if (req.url === '/generated' || req.url === '/generated/') {
    res.writeHead(200, { 'Content-Type': 'text/html' });
    return res.end(buildGeneratedPage());
  }

  // Route: /svg/robot/:id (original robots)
  const match = req.url.match(/^\/svg\/robot\/(\d+)$/);
  if (match) {
    const id = parseInt(match[1], 10);
    if (id < 1 || id > 10) {
      res.writeHead(404, { 'Content-Type': 'text/plain' });
      return res.end('Robot not found. Valid IDs: 1-10');
    }
    const filePath = path.join(SVG_DIR, `robot_${id}.svg`);
    try {
      const svg = fs.readFileSync(filePath, 'utf8');
      res.writeHead(200, { 'Content-Type': 'image/svg+xml' });
      return res.end(svg);
    } catch {
      res.writeHead(404, { 'Content-Type': 'text/plain' });
      return res.end(`robot_${id}.svg not found on disk`);
    }
  }

  // Route: / (preview page)
  if (req.url === '/' || req.url === '/index.html') {
    res.writeHead(200, { 'Content-Type': 'text/html' });
    return res.end(buildPreviewPage());
  }

  res.writeHead(404, { 'Content-Type': 'text/plain' });
  res.end('Not found');
});

server.listen(PORT, () => {
  console.log(`\n  🤖 Robot SVG Preview Server`);
  console.log(`  ──────────────────────────`);
  console.log(`  Preview all:  http://localhost:${PORT}/`);
  console.log(`  Individual:   http://localhost:${PORT}/svg/robot/{1..10}\n`);
});
