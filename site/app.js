'use strict';
const repository = 'https://github.com/R4faelMendes/J.A.R.V.I.S';
async function loadHistory() {
  try {
    const response = await fetch('status.json', { signal: AbortSignal.timeout(8000) });
    if (!response.ok) return;
    const data = await response.json();
    if (!Array.isArray(data.commits) || data.commits.length === 0) return;
    const entries = data.commits.slice(0, 5).map(commit => {
      if (!/^[a-f0-9]{40}$/.test(commit.sha)) throw new Error('Commit inválido');
      const article = document.createElement('article');
      const time = document.createElement('time');
      const date = new Date(commit.date);
      time.dateTime = date.toISOString();
      time.textContent = date.toLocaleDateString('pt-BR', { timeZone: 'America/Sao_Paulo', day: '2-digit', month: 'short', year: 'numeric' });
      const link = document.createElement('a');
      link.href = `${repository}/commit/${commit.sha}`;
      link.textContent = commit.subject;
      article.append(time, link);
      return article;
    });
    document.querySelector('#timeline').replaceChildren(...entries);
  } catch { /* O histórico completo permanece acessível sem JavaScript ou rede. */ }
}
async function loadRelease() {
  try {
    const response = await fetch('https://api.github.com/repos/R4faelMendes/J.A.R.V.I.S/releases/latest', { signal: AbortSignal.timeout(8000) });
    if (!response.ok) return;
    const release = await response.json();
    if (!Array.isArray(release.assets)) return;
    document.querySelector('#release-status').textContent = `Última versão: ${release.tag_name}. Consulte as notas e verificações SHA-256 no GitHub.`;
    for (const link of document.querySelectorAll('[data-asset]')) {
      const asset = release.assets.find(item => item.name === link.dataset.asset);
      if (!asset) {
        link.href = `${repository}/releases`;
        link.textContent = 'Consultar disponibilidade ↗';
      }
    }
  } catch { /* Links estáveis de releases funcionam mesmo se a API atingir sua cota. */ }
}
loadHistory();
loadRelease();
