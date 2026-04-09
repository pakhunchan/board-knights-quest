const fs = require('fs');
const path = require('path');

const template = fs.readFileSync(
  path.join(__dirname, 'public/svg/generated/robot-yellow-v2-4.svg'),
  'utf8'
);

// Original v2-4 colors to replace
const ORIG = {
  light: '#F0C858',   // antenna ball, belly panel, highlights, power symbol line
  main:  '#DCA830',   // stem, head, body, body-bottom, legs, power button inner
  dark:  '#A88020',   // ridges, eye socket, neck, power button outer, arch shadow
  pupil: '#3A2E00',   // eye pupils
};

// Color palettes — all slightly muted/warm to harmonize with soft pastel landscape
const palettes = {
  yellow: [
    { name: 'Golden Honey',    light: '#F0C858', main: '#DCA830', dark: '#A88020', pupil: '#3A2E00' },
    { name: 'Warm Marigold',   light: '#F5D068', main: '#E0B438', dark: '#B08A28', pupil: '#3A2800' },
    { name: 'Soft Buttercup',  light: '#F8E080', main: '#E4C44A', dark: '#B09A30', pupil: '#383200' },
    { name: 'Muted Saffron',   light: '#ECC060', main: '#D4A030', dark: '#A07820', pupil: '#3C2A00' },
    { name: 'Sunny Wheat',     light: '#F8D870', main: '#E8BC40', dark: '#B89228', pupil: '#382E00' },
  ],
  purple: [
    { name: 'Soft Lavender',   light: '#B898D8', main: '#8B6CB0', dark: '#6A4E90', pupil: '#1E1038' },
    { name: 'Muted Plum',      light: '#A888C8', main: '#7E5EA0', dark: '#5E4280', pupil: '#1C0E30' },
    { name: 'Dusty Violet',    light: '#C0A0D0', main: '#9878B8', dark: '#705898', pupil: '#201240' },
    { name: 'Berry Grape',     light: '#B088C8', main: '#8860A8', dark: '#684088', pupil: '#1A0E30' },
    { name: 'Periwinkle',      light: '#B0A8D8', main: '#8880C0', dark: '#6860A0', pupil: '#181838' },
  ],
  orange: [
    { name: 'Warm Apricot',    light: '#F0B870', main: '#E09048', dark: '#B07030', pupil: '#3A1E08' },
    { name: 'Soft Tangerine',  light: '#F8A878', main: '#E88050', dark: '#C06038', pupil: '#381808' },
    { name: 'Dusty Peach',     light: '#F0B888', main: '#D89460', dark: '#B07448', pupil: '#382008' },
    { name: 'Burnt Amber',     light: '#E8A878', main: '#D48050', dark: '#A86038', pupil: '#381A08' },
    { name: 'Coral Sunset',    light: '#F8B080', main: '#E88858', dark: '#C06840', pupil: '#381C08' },
  ],
  pink: [
    { name: 'Soft Rose',       light: '#F098B0', main: '#D07088', dark: '#A85068', pupil: '#300818' },
    { name: 'Dusty Blush',     light: '#E898B8', main: '#C87090', dark: '#A05070', pupil: '#2E0818' },
    { name: 'Warm Peony',      light: '#F0A0B8', main: '#D88090', dark: '#B06070', pupil: '#300A18' },
    { name: 'Berry Rose',      light: '#E888B0', main: '#C86088', dark: '#A04068', pupil: '#2C0818' },
    { name: 'Mauve Blossom',   light: '#E0A0C0', main: '#C080A0', dark: '#986080', pupil: '#280A18' },
  ],
};

const outDir = path.join(__dirname, 'public/svg/generated');

for (const [color, variants] of Object.entries(palettes)) {
  variants.forEach((p, i) => {
    let svg = template;
    svg = svg.replace(new RegExp(ORIG.light.replace('#', '#'), 'g'), p.light);
    svg = svg.replace(new RegExp(ORIG.main.replace('#', '#'), 'g'), p.main);
    svg = svg.replace(new RegExp(ORIG.dark.replace('#', '#'), 'g'), p.dark);
    svg = svg.replace(new RegExp(ORIG.pupil.replace('#', '#'), 'g'), p.pupil);

    const filename = `robot-${color}-v3-${i + 1}.svg`;
    fs.writeFileSync(path.join(outDir, filename), svg);
    console.log(`  ${filename} — ${p.name}`);
  });
}

console.log('\nDone! 20 color variants generated.');
