import { execFileSync } from 'node:child_process';
import { copyFileSync, cpSync, mkdirSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { join } from 'node:path';

const root = fileURLToPath(new URL('../', import.meta.url));
const out = join(root, 'artifacts/site');
mkdirSync(out, { recursive: true });
cpSync(join(root, 'site'), out, { recursive: true });
for (const font of ['ChakraPetch-SemiBold.ttf', 'IBMPlexSans-Regular.ttf']) {
  copyFileSync(join(root, 'src/Jarvis.Mobile/Resources/Fonts', font), join(out, 'fonts', font));
}
copyFileSync(join(root, 'src/Jarvis.Desktop/Assets/jarvis.ico'), join(out, 'jarvis.ico'));

const log = execFileSync('git', ['log', '-5', '--format=%H%x09%cI%x09%s'], { cwd: root, encoding: 'utf8' });
const commits = log.trimEnd().split('\n').filter(Boolean).map(line => {
  const [sha, date, ...subject] = line.split('\t');
  return { sha, date, subject: subject.join(' ') };
});
writeFileSync(join(out, 'status.json'), JSON.stringify({ commits }));
writeFileSync(join(out, '.nojekyll'), '');
