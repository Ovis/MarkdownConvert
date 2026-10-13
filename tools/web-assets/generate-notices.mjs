import fs from 'node:fs';
import path from 'node:path';

const metadata = JSON.parse(fs.readFileSync('build-meta.json', 'utf8'));
const packageRoots = new Set();
for (const input of Object.keys(metadata.inputs)) {
  if (!input.includes('node_modules/')) continue;
  let directory = path.dirname(path.resolve(input));
  while (directory !== path.dirname(directory)) {
    const descriptor = path.join(directory, 'package.json');
    if (fs.existsSync(descriptor)) {
      packageRoots.add(directory);
      break;
    }
    directory = path.dirname(directory);
  }
}

const entries = [...packageRoots].map(directory => {
  const descriptor = JSON.parse(fs.readFileSync(path.join(directory, 'package.json'), 'utf8'));
  const licenseFile = fs.readdirSync(directory).find(name => /^(license|licence|copying)(\.|$)/i.test(name));
  return {
    name: descriptor.name || path.basename(directory),
    version: descriptor.version || 'unversioned',
    license: descriptor.license || 'See package distribution',
    text: licenseFile ? fs.readFileSync(path.join(directory, licenseFile), 'utf8') : null
  };
}).sort((a, b) => a.name.localeCompare(b.name));

const lines = [
  'MarkdownExporter — bundled JavaScript third-party notices',
  '',
  'Generated from the esbuild input list and the pinned npm package tree.',
  'A package without a local license file is identified by package name, version, and declared license.',
  ''
];
for (const item of entries) {
  lines.push('='.repeat(72), `${item.name} ${item.version} — ${item.license}`, '');
  if (item.text) lines.push(item.text.trim(), '');
}
const target = path.resolve('../../Licence/THIRD-PARTY-NOTICES.txt');
fs.writeFileSync(target, lines.join('\n').trimEnd() + '\n', 'utf8');
console.log(`Wrote notices for ${entries.length} bundled packages`);
