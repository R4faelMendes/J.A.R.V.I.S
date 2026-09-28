// Autoteste ponta-a-ponta do J.A.R.V.I.S (ativo com ?smoke=1 ou JARVIS_SMOKE=1)
import { speechService } from './speechService';
import { memoryService } from './memoryService';
import { aiService } from './aiService';

const results = [];

const push = (name, ok, detail) => {
  results.push({ name, ok, detail: detail || '' });
  // eslint-disable-next-line no-console
  console.log(`[SMOKE] ${ok ? 'PASS' : 'FAIL'} ${name}${detail ? ` — ${detail}` : ''}`);
  return ok;
};

const errText = (e) => String(e?.message || e);

function float32ToBase64(samples) {
  const bytes = new Uint8Array(samples.buffer, samples.byteOffset, samples.byteLength);
  let bin = '';
  const chunk = 0x8000;
  for (let i = 0; i < bytes.length; i += chunk) {
    bin += String.fromCharCode.apply(null, bytes.subarray(i, i + chunk));
  }
  return btoa(bin);
}

async function testTts() {
  if (!window.electronAPI?.synthesizeSpeech) {
    push('tts-neural', false, 'api ausente');
    return;
  }
  try {
    const res = await window.electronAPI.synthesizeSpeech({
      text: 'Autoteste de voz do Jarvis.',
      voice: 'pt-BR-AntonioNeural',
      rate: 1,
      pitch: 1
    });
    const kb = res?.data ? Math.round((res.data.length * 3) / 4 / 1024) : 0;
    push('tts-neural', !!res?.ok && kb > 1, res?.ok ? `${kb} KB` : `erro=${res?.error}`);
  } catch (e) {
    push('tts-neural', false, errText(e));
  }
}

async function testVoices() {
  try {
    const voices = await window.electronAPI.listNeuralVoices();
    const ok = Array.isArray(voices) && voices.some((v) => v.shortName === 'pt-BR-AntonioNeural');
    push('tts-voices', ok, `total=${Array.isArray(voices) ? voices.length : 0}`);
  } catch (e) {
    push('tts-voices', false, errText(e));
  }
}

async function testStt() {
  if (!window.electronAPI?.sttTranscribe) {
    push('stt-whisper', false, 'api ausente');
    return;
  }
  try {
    const res = await fetch('smoke-sample.mp3');
    if (!res.ok) throw new Error(`amostra HTTP ${res.status}`);
    const ctx = new (window.AudioContext || window.webkitAudioContext)();
    const decoded = await ctx.decodeAudioData(await res.arrayBuffer());

    // Reamostra para 16 kHz (formato exigido pelo Whisper)
    const targetRate = 16000;
    const offline = new (window.OfflineAudioContext || window.webkitOfflineAudioContext)(
      1, Math.ceil(decoded.duration * targetRate), targetRate
    );
    const src = offline.createBufferSource();
    src.buffer = decoded;
    src.connect(offline.destination);
    src.start(0);
    const rendered = await offline.startRendering();
    const samples = rendered.getChannelData(0);
    ctx.close();

    const out = await window.electronAPI.sttTranscribe({
      samples: float32ToBase64(samples),
      sampleRate: targetRate,
      model: (memoryService.getPreferences() || {}).sttModel || 'base'
    });
    const text = String(out?.text || '');
    // eslint-disable-next-line no-console
    console.log(`[SMOKE] stt-text: "${text}" (${out?.ms || '?'}ms)`);

    // Valida o pipeline com similaridade de tokens (o Whisper erra 1–2 palavras)
    const norm = (s) => String(s).toLowerCase().normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9\s]/g, ' ').replace(/\s+/g, ' ').trim();
    const expected = norm('Abrir o navegador e procurar as configurações do sistema').split(' ');
    const got = new Set(norm(text).split(' ').filter(Boolean));
    const hits = expected.filter(w => got.has(w)).length;
    const ratio = hits / expected.length;
    push('stt-whisper', ratio >= 0.6, `${hits}/${expected.length} tokens (${Math.round(ratio * 100)}%)`);
  } catch (e) {
    push('stt-whisper', false, errText(e));
  }
}

async function testMicrophone() {
  try {
    let res = await speechService.testMicrophone();
    const detail = (r) => `pico=${r.peak}% raw=${r.raw} mudo=${r.muted ? 'sim' : 'não'} "${r.device}"`;

    // Microfone padrão mudo: varre os dispositivos procurando um que funcione
    if (!res?.ok && res?.error === 'mic-silent') {
      const devices = await speechService.listInputDevices();
      for (const d of devices.slice(0, 6)) {
        const prev = speechService.settings.micDeviceId;
        speechService.configure({ micDeviceId: d.deviceId });
        const alt = await speechService.testMicrophone();
        if (alt?.ok) {
          speechService.configure({ micDeviceId: prev || '' });
          push('microphone', false,
            `padrão silencioso — "${d.label}" FUNCIONA; escolha-o em Configurações > Microfone`);
          return;
        }
        speechService.configure({ micDeviceId: prev || '' });
      }
      push('microphone', false, `nenhum dos ${devices.length} microfones capturou áudio`);
      return;
    }

    push('microphone', !!res?.ok, res?.ok ? detail(res) : (res?.message || res?.error));
  } catch (e) {
    push('microphone', false, errText(e));
  }
}

async function testPcFind() {
  if (!window.electronAPI?.pcControl) {
    push('pc-find', false, 'api ausente');
    return;
  }
  try {
    const app = await window.electronAPI.pcControl('find', 'navegador');
    const proj = await window.electronAPI.pcControl('find', 'jarvis');
    const missing = await window.electronAPI.pcControl('find', 'arquivo inexistente 98765');
    const ok = !!app?.found && !!proj?.found && missing?.found === false;
    push('pc-find', ok, `app=${app?.found}(${app?.name || '-'}) proj=${proj?.found}(${proj?.kind || '-'}) inexistente=${missing?.found}`);
  } catch (e) {
    push('pc-find', false, errText(e));
  }
}

async function testBrave() {
  if (!window.electronAPI?.braveStatus) {
    push('brave', false, 'api ausente');
    return;
  }
  try {
    const s = await window.electronAPI.braveStatus();
    push('brave', !!s, s?.found ? `instalado=${s.path}` : 'não instalado (fallback embutido)');
  } catch (e) {
    push('brave', false, errText(e));
  }
}

async function testPrefs() {
  try {
    const prefs = memoryService.getPreferences() || {};
    push('prefs', !!prefs.voiceEngine, `voiceEngine=${prefs.voiceEngine} autoListen=${prefs.autoListen}`);
  } catch (e) {
    push('prefs', false, errText(e));
  }
}

async function testAi() {
  try {
    const prefs = memoryService.getPreferences() || {};
    const answer = String(await aiService.query('Responda de forma curta: o sistema está online?')).trim();
    // Resposta do cérebro local começa com "Processei a instrução" — sinal de que a nuvem falhou
    const cloud = !!answer && !/processei a instru/i.test(answer);
    push('ia-nuvem', cloud, `provedor=${prefs.aiProvider || 'free'} resposta="${answer.slice(0, 70)}"`);
  } catch (e) {
    push('ia-nuvem', false, errText(e));
  }
}

async function testLoginStealth() {
  // Sinais de "navegador normal" dentro do emulador (emulado como Brave) — o
  // Google exige tudo isto para permitir login: webdriver=false, window.chrome
  // completo, plugins>0 e a marca "Brave"/"Google Chrome" nos Client Hints.
  // Autocontida (não fecha sobre variáveis externas): roda direto na janela
  // atual e é serializada para as webviews do emulador.
  const collectSpSignals = () => {
    let cd = 'none';
    try {
      const d = Object.getOwnPropertyDescriptor(window, 'chrome');
      if (d) cd = `${d.get ? 'get' : 'data'}/${d.configurable ? 'conf' : 'NONCONF'}/${d.writable ? 'w' : 'nw'}`;
    } catch { cd = 'err'; }
    let crOwn = 'none';
    try {
      const c = window.chrome;
      const d2 = c ? Object.getOwnPropertyDescriptor(c, 'runtime') : null;
      if (d2) crOwn = d2.get ? 'get' : (d2.value ? 'val' : 'empty');
    } catch { crOwn = 'err'; }
    return {
      wd: navigator.webdriver,
      chromeRt: !!(window.chrome && window.chrome.runtime),
      chromeApp: !!(window.chrome && window.chrome.app),
      plugins: (navigator.plugins || []).length,
      brands: (navigator.userAgentData && navigator.userAgentData.brands
        ? navigator.userAgentData.brands : []).map((b) => b.brand).join('|'),
      sp: window.__sp || 0,
      spErr: window.__spErr || '',
      sp3: window.__sp3 || 0,
      cd,
      crOwn
    };
  };
  try {
    let parsed = null;
    let reloaded = false;
    for (let attempt = 0; attempt < 30 && !(parsed && Number(parsed.sp) >= 9); attempt += 1) {
      const wv = document.querySelector('webview');
      if (wv?.executeJavaScript) {
        try {
          const raw = await Promise.race([
            wv.executeJavaScript(`(${collectSpSignals.toString()})()`),
            new Promise((_, rej) => setTimeout(() => rej(new Error('probe lento')), 2500))
          ]);
          parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
        } catch { /* webview ainda inicializando */ }
        // A navegação inicial disputa com o envio via CDP: recarrega UMA vez para
        // provar que documentos novos (é assim que funciona o login — o Google
        // abre numa navegação nova) recebem o script anti-bot
        if (!reloaded && attempt >= 2 && parsed && Number(parsed.sp) === 0
          && typeof wv.reload === 'function') {
          reloaded = true;
          try { wv.reload(); } catch { /* ignora */ }
        }
      } else {
        // Sem webview na tela (home com sidebar): a JANELA PRINCIPAL recebe o
        // mesmo script anti-bot e os mesmos Client Hints Brave — sonda local
        // em vez de falhar por "webview indisponível"
        try { parsed = collectSpSignals(); } catch { /* ignora */ }
      }
      if (!(parsed && Number(parsed.sp) >= 9)) await new Promise((r) => setTimeout(r, 500));
    }
    if (!parsed) throw new Error('sonda indisponível');
    const ok = parsed.wd === false && !!parsed.chromeRt && parsed.plugins > 0
      && Number(parsed.sp) >= 9
      && /Google Chrome|Brave/.test(String(parsed.brands || ''));
    const alvo = document.querySelector('webview') ? 'webview' : 'janela';
    push('login-google', ok,
      `alvo=${alvo} wd=${parsed.wd} chromeRt=${parsed.chromeRt} plugins=${parsed.plugins} `
      + `sp=${parsed.sp}/${parsed.sp3} cd=${parsed.cd} crOwn=${parsed.crOwn} brands=${parsed.brands}`
      + `${parsed.spErr ? ` spErr=${parsed.spErr}` : ''}`);
  } catch (e) {
    push('login-google', false, errText(e));
  }
}

export async function runSmokeTest() {
  // eslint-disable-next-line no-console
  console.log('[SMOKE] iniciando autoteste ponta-a-ponta...');
  await testPrefs();
  await testTts();
  await testVoices();
  await testAi();
  await testStt();
  await testMicrophone();
  await testPcFind();
  await testBrave();
  await testLoginStealth();

  const failed = results.filter((r) => !r.ok);
  const summary = `${results.length - failed.length}/${results.length} testes OK`;
  // eslint-disable-next-line no-console
  console.log(`[SMOKE] RESULTADO ${failed.length === 0 ? 'APROVADO' : 'REPROVADO'} — ${summary}`);
  failed.forEach((f) => {
    // eslint-disable-next-line no-console
    console.log(`[SMOKE] FALHOU: ${f.name} ${f.detail}`);
  });

  // Captura a interface para conferência visual (Agenda/Configurações/música)
  try {
    if (window.electronAPI?.saveScreenshot) {
      // Se um painel estiver aberto, rola até o fim para registrar toda a seção
      const modal = document.querySelector('.fixed.inset-0.z-50'); // painel aberto (os overlays de fundo têm z-0)
      // eslint-disable-next-line no-console
      console.log(`[SMOKE] capturando: url=${window.location.search} modal="${String(modal?.textContent || 'nenhum').replace(/\s+/g, ' ').slice(0, 55)}"`);
      if (modal) {
        const cands = [];
        const scrollers = [];
        modal.querySelectorAll('*').forEach((el) => {
          const diff = el.scrollHeight - el.clientHeight;
          if (diff > 10) {
            const ov = getComputedStyle(el).overflowY;
            cands.push({ el, diff, ov, tag: el.tagName.toLowerCase(), cls: String(el.className).slice(0, 50) });
            // scrollTop também funciona em overflow:hidden (só não há barra visível)
            if (ov === 'auto' || ov === 'scroll' || ov === 'hidden') { el.scrollTop = el.scrollHeight; scrollers.push(el); }
          }
        });
        cands.sort((a, b) => b.diff - a.diff);
        // Prioriza a seção de presets de voz (fica no meio do painel)
        const label = Array.from(modal.querySelectorAll('label, h2, div'))
          .find((el) => el.childElementCount === 0 && String(el.textContent).includes('Predefinições de voz'));
        if (label) label.scrollIntoView({ block: 'start' });
        await new Promise((r) => setTimeout(r, 400));
        await new Promise((r) => requestAnimationFrame(() => r()));
        // eslint-disable-next-line no-console
        console.log(`[SMOKE] scroll: modal=${!!modal} cands=${cands.length} alvo=${!!label} ` +
          cands.slice(0, 3).map((c) => `<${c.tag} ov=${c.ov} +${c.diff}>`).join(' ') +
          ` | depois: ${scrollers.map((el) => `${el.tagName.toLowerCase()}.scrollTop=${el.scrollTop}`).join(' ')}`);
      }
      const shot = await window.electronAPI.saveScreenshot(`smoke-${new URLSearchParams(window.location.search).get('modal') || 'home'}`);
      if (shot?.ok) {
        // eslint-disable-next-line no-console
        console.log(`[SMOKE] screenshot: ${shot.path}`);
      }
      // Home: 2ª captura com o emulador (guia Música) em vista + 3ª com a guia Agenda
      if (!modal) {
        const wv = document.querySelector('[data-emulator]') || document.querySelector('webview');
        // eslint-disable-next-line no-console
        console.log(`[SMOKE] emulador: webviews=${document.querySelectorAll('webview').length} ` +
          `guias=${['Música', 'Agenda', 'Proc.'].filter((g) => (document.body.textContent || '').includes(g)).length} ` +
          `shieldsTxt=${(document.body.textContent || '').includes('SHIELDS')} ` +
          `btnAgendaGoogle=${(document.body.textContent || '').includes('Agenda do Google')}`);
        if (wv?.scrollIntoView) {
          wv.scrollIntoView({ block: 'center' });
          await new Promise((r) => setTimeout(r, 800));
          await new Promise((r) => requestAnimationFrame(() => r()));
          const shot2 = await window.electronAPI.saveScreenshot('home-player');
          if (shot2?.ok) {
            // eslint-disable-next-line no-console
            console.log(`[SMOKE] screenshot: ${shot2.path}`);
          }
          // 3ª captura: guia Agenda (Google Calendar) aberta no emulador
          window.dispatchEvent(new CustomEvent('jarvis-open-tab', { detail: 'agenda' }));
          await new Promise((r) => setTimeout(r, 1500));
          const shot3 = await window.electronAPI.saveScreenshot('home-agenda');
          if (shot3?.ok) {
            // eslint-disable-next-line no-console
            console.log(`[SMOKE] screenshot: ${shot3.path}`);
          }
          window.dispatchEvent(new CustomEvent('jarvis-open-tab', { detail: 'music' }));
        }
      }
    }
  } catch (e) {
    // eslint-disable-next-line no-console
    console.log(`[SMOKE] screenshot falhou: ${errText(e)}`);
  }

  return { ok: failed.length === 0, results };
}
