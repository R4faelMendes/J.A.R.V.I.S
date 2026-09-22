// J.A.R.V.I.S. — Worker de Reconhecimento de Voz Local (STT offline)
//
// Roda como processo filho do Electron em modo Node:
//   ELECTRON_RUN_AS_NODE=1 electron.exe stt-worker.cjs <pastaDoCacheDoModelo>
//
// Protocolo (linhas JSON via stdin/stdout):
//   entrada : { id, samples: <base64 Float32Array>, sampleRate, model? }
//   saída   : { id, text, ms } | { id, error }
//
// Usa o Whisper local (onnxruntime-node) — sem internet, sem chave de API.

const MODEL_IDS = {
  tiny: 'onnx-community/whisper-tiny',
  base: 'onnx-community/whisper-base',
  small: 'onnx-community/whisper-small'
};

const cacheDir = process.argv[2] || null;
let asrPromise = null;
let busy = false;
const queue = [];

function log(msg) {
  process.stdout.write(`${JSON.stringify({ type: 'log', msg })}\n`);
}

function send(obj) {
  process.stdout.write(`${JSON.stringify(obj)}\n`);
}

async function getAsr(modelKey) {
  if (!asrPromise) {
    const key = MODEL_IDS[modelKey] ? modelKey : 'base';
    log(`carregando modelo Whisper (${key})...`);
    const t0 = Date.now();
    // eslint-disable-next-line import/no-extraneous-dependencies
    const { pipeline, env } = require('@huggingface/transformers');
    env.allowLocalModels = false;
    if (cacheDir) env.cacheDir = cacheDir;
    asrPromise = pipeline('automatic-speech-recognition', MODEL_IDS[key], { dtype: 'q8' })
      .then((fn) => {
        log(`modelo pronto em ${Date.now() - t0} ms`);
        send({ type: 'ready', ms: Date.now() - t0 });
        return fn;
      })
      .catch((err) => {
        asrPromise = null;
        throw err;
      });
  }
  return asrPromise;
}

function base64ToFloat32(b64) {
  const buf = Buffer.from(b64, 'base64');
  const f32 = new Float32Array(Math.floor(buf.length / 4));
  new Uint8Array(f32.buffer, 0, f32.length * 4).set(buf.subarray(0, f32.length * 4));
  return f32;
}

async function handle(msg) {
  const { id, samples, model } = msg;
  const started = Date.now();
  try {
    const audio = base64ToFloat32(samples);
    if (!audio.length) {
      send({ id, error: 'audio-vazio', ms: 0 });
      return;
    }
    const asr = await getAsr(model);
    const out = await asr(audio, { language: 'portuguese', task: 'transcribe' });
    const text = String(out?.text || '').trim();
    send({ id, text, ms: Date.now() - started });
  } catch (err) {
    send({ id, error: err?.message || String(err), ms: Date.now() - started });
  }
}

async function pump() {
  if (busy) return;
  const next = queue.shift();
  if (!next) return;
  busy = true;
  await handle(next);
  busy = false;
  pump();
}

let buffer = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (chunk) => {
  buffer += chunk;
  let idx;
  while ((idx = buffer.indexOf('\n')) >= 0) {
    const line = buffer.slice(0, idx).trim();
    buffer = buffer.slice(idx + 1);
    if (!line) continue;
    let msg;
    try { msg = JSON.parse(line); } catch { continue; }
    if (msg && msg.type === 'warmup') {
      getAsr(msg.model || 'base').catch((e) => log(`warmup falhou: ${e.message}`));
      continue;
    }
    if (msg && msg.id != null) {
      queue.push(msg);
      pump();
    }
  }
});
process.stdin.on('end', () => process.exit(0));

send({ type: 'started', pid: process.pid });
log('worker de voz pronto');
