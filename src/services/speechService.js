// J.A.R.V.I.S. — Motor de Voz (Reconhecimento de Fala + Síntese de Voz)
//
// Saída de áudio (TTS), em ordem de prioridade:
//   1. Voz neural Microsoft Edge (pt-BR Antonio/Francisca) via Electron — fluída e natural
//   2. Voz neural local instalada no Windows ("Online (Natural)")
//   3. Voz SAPI local do sistema (fallback offline, falada em blocos para não cortar)
//
// Entrada de áudio (STT), em ordem de prioridade:
//   1. Whisper local offline (processo auxiliar do Electron) — funciona sem internet
//   2. Web Speech API (navegador comum / Chrome)

import { sounds } from './soundEffects';
import { memoryService } from './memoryService';

const SAMPLE_RATE = 16000;
const FRAME_SIZE = 2048; // 128 ms por quadro @ 16 kHz

const VAD = {
  threshold: 0.014,      // RMS mínimo inicial para considerar que alguém falou
  noiseFactor: 3.2,      // sensibilidade adaptativa: limiar = ruído de fundo * fator
  minThreshold: 0.008,   // piso absoluto do limiar
  maxThreshold: 0.030,   // teto absoluto do limiar
  minSpeechMs: 160,      // tempo mínimo de fala para começar a gravar
  silenceMs: 750,        // silêncio que encerra a frase (mais ágil)
  maxUtteranceMs: 30000, // limite por frase (frases longas não são cortadas)
  keepAliveMs: 500       // eco do J.A.R.V.I.S. a ignorar após falar
};

const ERROR_MESSAGES = {
  'mic-denied': 'Senhor, o sistema negou o acesso ao microfone. Verifique as permissões de áudio do computador.',
  'mic-missing': 'Senhor, nenhum microfone foi detectado neste computador.',
  'mic-unsupported': 'Este ambiente não permite captura de áudio.',
  'motor-de-voz-indisponivel': 'Senhor, o motor de reconhecimento de voz local não pôde ser iniciado.',
  'worker-encerrado': 'Senhor, o motor de voz local encerrou inesperadamente. Vou tentar reiniciá-lo.',
  'timeout': 'Senhor, o reconhecimento de voz demorou mais que o esperado. Tente novamente.',
  'audio-vazio': 'Senhor, não capturei áudio suficiente. Fale um pouco mais perto do microfone.',
  'web-speech-network': 'Senhor, o reconhecimento de voz online está indisponível aqui. Use o aplicativo desktop do J.A.R.V.I.S. para conversar por voz.',
  'offline-transcribe': 'Senhor, não consegui interpretar o áudio. Verifique sua conexão ou o modelo de voz local.'
};

function humanError(code) {
  return ERROR_MESSAGES[code] || `Senhor, falha no reconhecimento de voz (${code}).`;
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, Number(value) || min));
}

// Remove markdown/emojis e normaliza o texto antes de falar
function sanitizeForSpeech(text) {
  return String(text || '')
    .replace(/```[\s\S]*?```/g, ' código omitido ')
    .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
    .replace(/[*_#`>~|-]{2,}/g, ' ')
    .replace(/[\u{1F000}-\u{1FAFF}\u{2600}-\u{27BF}\u{FE0F}]/gu, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

// Quebra o texto em blocos curtos: evita o corte de síntese após ~15 s e
// deixa a fala com pausas naturais entre frases.
function chunkForSpeech(text, maxLen = 170) {
  const sentences = String(text)
    .split(/(?<=[.!?;:\n])\s+/)
    .map(s => s.trim())
    .filter(Boolean);

  const chunks = [];
  for (const sentence of sentences) {
    if (sentence.length <= maxLen) {
      chunks.push(sentence);
      continue;
    }
    let current = '';
    for (const word of sentence.split(' ')) {
      if ((current + ' ' + word).trim().length > maxLen) {
        if (current) chunks.push(current.trim());
        current = word;
      } else {
        current = `${current} ${word}`;
      }
    }
    if (current.trim()) chunks.push(current.trim());
  }
  return chunks.length ? chunks : [String(text)];
}

function base64FromFloat32(f32) {
  const bytes = new Uint8Array(f32.buffer, f32.byteOffset, f32.byteLength);
  let binary = '';
  const step = 0x8000;
  for (let i = 0; i < bytes.length; i += step) {
    binary += String.fromCharCode.apply(null, bytes.subarray(i, i + step));
  }
  return btoa(binary);
}

function bytesFromBase64(b64) {
  const bin = atob(b64);
  const bytes = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
  return bytes;
}

class SpeechEngine {
  constructor() {
    // STT
    this.recognition = null;
    this.isListening = false;
    this.isListeningDesired = false;
    this.onResultCallback = null;
    this.onStatusCallback = null;
    this.onVolumeCallback = null;
    this.sttEngine = 'unknown';       // 'local' | 'webspeech' | 'unsupported'
    this.sttState = 'off';            // status reportado pelo processo auxiliar
    this.webSpeechFailed = false;

    // Captura de áudio / VAD
    this.audioContext = null;
    this.microphoneStream = null;
    this.processorNode = null;
    this.sourceNode = null;
    this.vad = { state: 'armed', chunks: [], speechMs: 0, silenceMs: 0, startedAt: 0 };
    this.noiseFloor = 0.004; // ruído de fundo estimado (adapta a sensibilidade)
    this.transcribing = false;

    // TTS
    this.isSpeaking = false;
    this.speakToken = 0;
    this.playingAudio = null;
    this.localVoices = [];
    this.keepaliveTimer = null;
    this.muteUntil = 0;
    this.settings = {
      voiceEngine: 'neural',
      voiceId: 'pt-BR-AntonioNeural',
      localVoiceName: '',
      rate: 1.0,
      pitch: 1.0,
      model: 'base',
      micDeviceId: ''
    };

    this.loadPrefs();
    this.initVoices();
    this.initWebSpeech();
    this.bindSttStatus();
  }

  // -------------------------------------------------------------------------
  // Preferências / configuração
  // -------------------------------------------------------------------------

  loadPrefs() {
    try {
      const prefs = memoryService.getPreferences() || {};
      if (prefs.voiceEngine) this.settings.voiceEngine = prefs.voiceEngine;
      if (prefs.voiceId) this.settings.voiceId = prefs.voiceId;
      if (prefs.localVoiceName) this.settings.localVoiceName = prefs.localVoiceName;
      if (prefs.voiceSpeed) this.settings.rate = prefs.voiceSpeed;
      if (prefs.voicePitch) this.settings.pitch = prefs.voicePitch;
      if (prefs.sttModel) this.settings.model = prefs.sttModel;
    if (typeof prefs.micDeviceId === 'string') this.settings.micDeviceId = prefs.micDeviceId;
    } catch { /* ignora */ }
    if (this.settings.voiceEngine === 'neural') {
      this.settings.voiceId = this.settings.voiceId || 'pt-BR-AntonioNeural';
    }
  }

  configure(patch = {}) {
    const previousModel = this.settings.model;
    this.settings = { ...this.settings, ...patch };

    if (patch.voiceSpeed != null) this.settings.rate = patch.voiceSpeed;
    if (patch.voicePitch != null) this.settings.pitch = patch.voicePitch;
    if (patch.sttModel != null) this.settings.model = patch.sttModel;

    if (this.settings.model && this.settings.model !== previousModel && window.electronAPI?.sttWarmup) {
      window.electronAPI.sttWarmup(this.settings.model);
    }
    return this.settings;
  }

  getEngineInfo() {
    const hasElectronVoice = typeof window !== 'undefined' && !!window.electronAPI?.synthesizeSpeech;
    return {
      tts: this.settings.voiceEngine === 'neural' && hasElectronVoice ? 'neural' : 'local',
      stt: this.sttEngine,
      sttState: this.sttState,
      model: this.settings.model,
      voice: this.settings.voiceEngine === 'neural' ? this.settings.voiceId : (this.settings.localVoiceName || 'sistema')
    };
  }

  // -------------------------------------------------------------------------
  // Vozes disponíveis
  // -------------------------------------------------------------------------

  initVoices() {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;

    const loadVoices = () => {
      this.localVoices = window.speechSynthesis.getVoices() || [];
    };
    loadVoices();
    if (window.speechSynthesis.onvoiceschanged !== undefined) {
      window.speechSynthesis.onvoiceschanged = loadVoices;
    }
  }

  listLocalVoices() {
    return (this.localVoices || []).map(v => ({ name: v.name, lang: v.lang, default: v.default }));
  }

  scoreLocalVoice(voice) {
    const name = (voice.name || '').toLowerCase();
    const lang = (voice.lang || '').toLowerCase();
    let score = 0;
    if (lang.startsWith('pt-br')) score += 40;
    else if (lang.startsWith('pt')) score += 30;
    if (name.includes('natural') || name.includes('neural')) score += 30;
    if (name.includes('online')) score += 12;
    if (name.includes('antonio') || name.includes('daniel') || name.includes('mauricio')) score += 6;
    if (name.includes('maria') || name.includes('zira') || name.includes('helena')) score += 3;
    if (name.includes('google')) score += 4;
    return score;
  }

  pickLocalVoice() {
    if (!this.localVoices.length) {
      this.localVoices = (typeof window !== 'undefined' && 'speechSynthesis' in window)
        ? window.speechSynthesis.getVoices() || []
        : [];
    }
    if (this.settings.localVoiceName) {
      const chosen = this.localVoices.find(v => v.name === this.settings.localVoiceName);
      if (chosen) return chosen;
    }
    return [...this.localVoices]
      .sort((a, b) => this.scoreLocalVoice(b) - this.scoreLocalVoice(a))[0] || null;
  }

  // -------------------------------------------------------------------------
  // Síntese de voz (TTS)
  // -------------------------------------------------------------------------

  async speak(text, onStart, onEnd) {
    const clean = sanitizeForSpeech(text);
    if (!clean) {
      if (onEnd) onEnd();
      return;
    }

    this.stopSpeaking();
    const token = ++this.speakToken;
    this._onStart = onStart;
    this._onEnd = onEnd;
    this.isSpeaking = true;
    if (onStart) onStart();

    // 1ª opção: voz neural da Microsoft (fluída, sem "robô")
    const canUseNeural = this.settings.voiceEngine !== 'local' && typeof window !== 'undefined' && window.electronAPI?.synthesizeSpeech;
    if (canUseNeural) {
      try {
        const res = await window.electronAPI.synthesizeSpeech({
          text: clean,
          voice: this.settings.voiceId || 'pt-BR-AntonioNeural',
          rate: clamp(this.settings.rate, 0.6, 1.6),
          pitch: clamp(this.settings.pitch, 0.6, 1.4)
        });
        if (token !== this.speakToken) return;
        if (res && res.ok && res.data) {
          const finished = await this.playBase64Audio(res.data, token);
          if (token !== this.speakToken) return;
          if (finished) {
            this.finishSpeak(token);
            return;
          }
        }
      } catch (err) {
        console.warn('TTS neural indisponível, usando voz local:', err?.message || err);
      }
    }

    if (token !== this.speakToken) return;
    this.speakLocal(clean, token);
  }

  playBase64Audio(b64, token) {
    return new Promise((resolve) => {
      let url = null;
      try {
        const bytes = bytesFromBase64(b64);
        const blob = new Blob([bytes], { type: 'audio/mpeg' });
        url = URL.createObjectURL(blob);
        const audio = new Audio(url);
        this.playingAudio = audio;

        let settled = false;
        const done = (ok) => {
          if (settled) return;
          settled = true;
          if (url) URL.revokeObjectURL(url);
          if (this.playingAudio === audio) this.playingAudio = null;
          resolve(ok);
        };

        audio.onended = () => done(true);
        audio.onerror = () => done(false);

        const playPromise = audio.play();
        if (playPromise && typeof playPromise.catch === 'function') {
          playPromise.catch(() => done(false));
        }
        // Redeprotelação: se o áudio não terminar, não trava a interface
        setTimeout(() => done(true), 60000);
      } catch (err) {
        if (url) URL.revokeObjectURL(url);
        resolve(false);
      }
    });
  }

  // Fallback local: fala em blocos curtos com guarda anti-corte
  speakLocal(text, token) {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) {
      this.finishSpeak(token);
      return;
    }

    const synth = window.speechSynthesis;
    try { synth.cancel(); } catch { /* ignora */ }

    const chunks = chunkForSpeech(text);
    let index = 0;
    const rate = clamp(this.settings.rate, 0.6, 1.6);
    const pitch = clamp(this.settings.pitch, 0.6, 2);

    clearInterval(this.keepaliveTimer);
    this.keepaliveTimer = setInterval(() => {
      try { synth.resume(); } catch { /* ignora */ }
    }, 6000);

    const next = () => {
      if (token !== this.speakToken) return;
      if (index >= chunks.length) {
        this.finishSpeak(token);
        return;
      }

      const piece = chunks[index++];
      const utterance = new SpeechSynthesisUtterance(piece);
      const voice = this.pickLocalVoice();
      if (voice) utterance.voice = voice;
      utterance.lang = voice?.lang || 'pt-BR';
      utterance.rate = rate;
      utterance.pitch = pitch;
      utterance.volume = 1;

      let advanced = false;
      const advance = () => {
        if (advanced) return;
        advanced = true;
        clearTimeout(guard);
        next();
      };

      utterance.onend = advance;
      utterance.onerror = advance;

      // Alguns navegadores deixam a frase "pendurada": força a passagem adiante
      const estimatedMs = (piece.length * 70) / rate + 6000;
      const guard = setTimeout(() => {
        try { synth.cancel(); } catch { /* ignora */ }
        advance();
      }, estimatedMs);

      try {
        synth.speak(utterance);
      } catch {
        advance();
      }
    };

    next();
  }

  finishSpeak(token) {
    if (token !== this.speakToken) return;
    clearInterval(this.keepaliveTimer);
    this.keepaliveTimer = null;
    this.isSpeaking = false;
    // Pequena janela para engolir o eco do próprio microfone
    this.muteUntil = Date.now() + VAD.keepAliveMs;
    this.resetVad('armed');
    const onEnd = this._onEnd;
    this._onEnd = null;
    this._onStart = null;
    if (onEnd) onEnd();
  }

  stopSpeaking() {
    this.speakToken += 1;
    clearInterval(this.keepaliveTimer);
    this.keepaliveTimer = null;

    if (this.playingAudio) {
      try { this.playingAudio.pause(); } catch { /* ignora */ }
      this.playingAudio = null;
    }
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      try { window.speechSynthesis.cancel(); } catch { /* ignora */ }
    }

    const wasSpeaking = this.isSpeaking;
    this.isSpeaking = false;
    const onEnd = this._onEnd;
    this._onEnd = null;
    this._onStart = null;
    if (wasSpeaking && onEnd) onEnd();
  }

  // -------------------------------------------------------------------------
  // Entrada de áudio (STT)
  // -------------------------------------------------------------------------

  bindSttStatus() {
    if (typeof window === 'undefined' || !window.electronAPI?.onSttStatus) return;
    window.electronAPI.onSttStatus((status) => {
      this.sttState = status?.state || 'unknown';
      if (status?.state === 'ready' && this.sttEngine === 'unknown') this.sttEngine = 'local';
      this.emitStatus({});
    });
  }

  initWebSpeech() {
    if (typeof window === 'undefined') return;

    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) {
      if (!window.electronAPI?.sttTranscribe) this.sttEngine = 'unsupported';
      return;
    }

    // No Electron o Web Speech API sempre falha (erro "network"): usamos o Whisper local.
    if (window.electronAPI?.sttTranscribe) {
      this.sttEngine = 'local';
      return;
    }

    this.recognition = new SpeechRecognition();
    this.recognition.continuous = true;
    this.recognition.interimResults = true;
    this.recognition.lang = 'pt-BR';

    this.recognition.onstart = () => {
      this.isListening = true;
      this.emitStatus({ phase: 'listening' });
    };

    this.recognition.onend = () => {
      this.isListening = false;
      if (this.isListeningDesired && !this.webSpeechFailed) {
        clearTimeout(this.restartTimeout);
        this.restartTimeout = setTimeout(() => {
          if (this.isListeningDesired && !this.webSpeechFailed) {
            try {
              this.recognition.start();
              this.isListening = true;
              this.emitStatus({ phase: 'listening' });
            } catch { /* já iniciado */ }
          }
        }, 250);
      } else {
        this.emitStatus({ isListening: false });
      }
    };

    this.recognition.onerror = (event) => {
      const error = event?.error;
      if (error === 'no-speech' || error === 'aborted') return;

      console.warn('Web Speech error:', error);
      if (error === 'network' || error === 'service-not-allowed') {
        this.webSpeechFailed = true;
        this.isListeningDesired = false;
        this.isListening = false;
        this.emitStatus({ isListening: false, error: 'web-speech-network' });
        try { this.recognition.stop(); } catch { /* ignora */ }
        return;
      }
      if (error === 'not-allowed' || error === 'audio-capture') {
        this.isListeningDesired = false;
        this.isListening = false;
        this.emitStatus({ isListening: false, error: 'mic-denied' });
      }
    };

    this.recognition.onresult = (event) => {
      let interim = '';
      let final = '';
      for (let i = event.resultIndex; i < event.results.length; i++) {
        const text = event.results[i][0]?.transcript || '';
        if (event.results[i].isFinal) final += text;
        else interim += text;
      }
      if (this.onResultCallback) {
        this.onResultCallback({ final: final.trim(), interim: interim.trim() });
      }
    };
  }

  emitStatus(extra = {}) {
    if (!this.onStatusCallback) return;
    this.onStatusCallback({
      isListening: this.isListeningDesired && (this.isListening || this.sttEngine === 'local'),
      engine: this.sttEngine,
      sttState: this.sttState,
      ...extra
    });
  }

  async openMicrophone() {
    if (this.microphoneStream) return true;
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      throw new Error('mic-unsupported');
    }

    const stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        ...(this.settings.micDeviceId ? { deviceId: { ideal: this.settings.micDeviceId } } : {}),
        channelCount: 1,
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true
      }
    });
    this.microphoneStream = stream;

    const AudioCtx = window.AudioContext || window.webkitAudioContext;
    this.audioContext = new AudioCtx({ sampleRate: SAMPLE_RATE });
    if (this.audioContext.state === 'suspended') {
      try { await this.audioContext.resume(); } catch { /* ignora */ }
    }

    this.sourceNode = this.audioContext.createMediaStreamSource(stream);
    this.processorNode = this.audioContext.createScriptProcessor(FRAME_SIZE, 1, 1);
    this.processorNode.onaudioprocess = (event) => {
      const data = event.inputBuffer.getChannelData(0);
      this.onAudioFrame(data);
    };
    this.sourceNode.connect(this.processorNode);
    this.processorNode.connect(this.audioContext.destination); // silêncio: só mantém o ciclo ativo
    return true;
  }

  closeMicrophone() {
    if (this.processorNode) {
      try {
        this.processorNode.onaudioprocess = null;
        this.processorNode.disconnect();
      } catch { /* ignora */ }
      this.processorNode = null;
    }
    if (this.sourceNode) {
      try { this.sourceNode.disconnect(); } catch { /* ignora */ }
      this.sourceNode = null;
    }
    if (this.microphoneStream) {
      this.microphoneStream.getTracks().forEach(track => track.stop());
      this.microphoneStream = null;
    }
    if (this.audioContext) {
      try { this.audioContext.close(); } catch { /* ignora */ }
      this.audioContext = null;
    }
  }

  resetVad(state = 'armed') {
    this.vad = { state, chunks: [], speechMs: 0, silenceMs: 0, startedAt: 0 };
  }

  // Limiar adaptativo: acompanha o ruído do ambiente para não perder fala
  // fraca nem disparar com ruído de fundo.
  currentThreshold() {
    return clamp((this.noiseFloor || 0.004) * VAD.noiseFactor, VAD.minThreshold, VAD.maxThreshold);
  }

  onAudioFrame(frame) {
    if (!this.isListeningDesired) return;

    // Não grava enquanto o J.A.R.V.I.S. fala (evita autoescuta)
    if (this.isSpeaking || Date.now() < this.muteUntil) {
      if (this.vad.state === 'recording') this.resetVad('armed');
      return;
    }
    if (this.transcribing || this.vad.state === 'transcribing') return;

    const frameMs = (FRAME_SIZE / this.audioContext.sampleRate) * 1000;
    let sum = 0;
    for (let i = 0; i < frame.length; i++) sum += frame[i] * frame[i];
    const rms = Math.sqrt(sum / frame.length);
    if (this.onVolumeCallback) this.onVolumeCallback(Math.min(1, rms * 12) * 255, frame);

    const threshold = this.currentThreshold();

    if (this.vad.state === 'armed') {
      if (rms >= threshold) {
        this.vad.speechMs += frameMs;
        if (this.vad.speechMs >= VAD.minSpeechMs) {
          this.vad.state = 'recording';
          this.vad.chunks = [Float32Array.from(frame)];
          this.vad.silenceMs = 0;
          this.vad.startedAt = Date.now();
          this.emitStatus({ phase: 'recording' });
        }
      } else {
        this.vad.speechMs = 0;
        // Atualiza o ruído de fundo apenas quando ninguém está falando
        this.noiseFloor = this.noiseFloor * 0.97 + rms * 0.03;
      }
      return;
    }

    if (this.vad.state === 'recording') {
      this.vad.chunks.push(Float32Array.from(frame));
      this.vad.silenceMs = rms >= threshold ? 0 : this.vad.silenceMs + frameMs;

      const elapsed = Date.now() - this.vad.startedAt;
      if (elapsed >= VAD.maxUtteranceMs || this.vad.silenceMs >= VAD.silenceMs) {
        this.transcribe(this.vad.chunks);
      }
    }
  }

  concatFloat32(chunks) {
    const total = chunks.reduce((acc, c) => acc + c.length, 0);
    const out = new Float32Array(total);
    let offset = 0;
    for (const chunk of chunks) {
      out.set(chunk, offset);
      offset += chunk.length;
    }
    // corta o silêncio final
    return out;
  }

  async transcribe(chunks) {
    this.vad.state = 'transcribing';
    this.transcribing = true;
    this.emitStatus({ phase: 'transcribing' });

    const audio = this.concatFloat32(chunks);
    this.resetVad('armed');

    let text = '';
    let error = null;

    if (window.electronAPI?.sttTranscribe) {
      const res = await window.electronAPI.sttTranscribe({
        samples: base64FromFloat32(audio),
        sampleRate: SAMPLE_RATE,
        model: this.settings.model
      });
      if (res && !res.error) text = res.text || '';
      else error = res?.error || 'offline-transcribe';
    } else {
      error = 'unsupported';
    }

    this.transcribing = false;

    if (error) {
      this.emitStatus({ phase: 'listening', error, message: humanError(error) });
      return;
    }

    const trimmed = text.trim();
    if (trimmed && this.onResultCallback) {
      this.onResultCallback({ final: trimmed, interim: '' });
    }
    this.emitStatus({ phase: 'listening' });
  }

  async startListening(onResult, onStatus) {
    this.onResultCallback = onResult || this.onResultCallback;
    this.onStatusCallback = onStatus || this.onStatusCallback;
    this.isListeningDesired = true;
    this.webSpeechFailed = false;

    // Modo desktop: pipeline local com Whisper
    if (window.electronAPI?.sttTranscribe) {
      this.sttEngine = 'local';
      try {
        await this.openMicrophone();
        this.isListening = true;
        sounds.playClick();
        if (window.electronAPI.sttWarmup) window.electronAPI.sttWarmup(this.settings.model);
        this.emitStatus({ phase: 'listening', engine: 'local' });
      } catch (err) {
        this.isListeningDesired = false;
        this.isListening = false;
        const error = err?.name === 'NotFoundError' ? 'mic-missing'
          : (err?.name === 'NotAllowedError' || err?.name === 'SecurityError') ? 'mic-denied'
            : (err?.message || 'mic-unsupported');
        this.emitStatus({ isListening: false, error, message: humanError(error) });
      }
      return;
    }

    // Navegador comum: Web Speech API
    if (!this.recognition) {
      this.emitStatus({ isListening: false, error: 'unsupported', message: humanError('unsupported') });
      return;
    }
    try {
      this.recognition.start();
      this.isListening = true;
      sounds.playClick();
      this.emitStatus({ phase: 'listening', engine: 'webspeech' });
    } catch { /* já iniciado */ }
  }

  stopListening() {
    this.isListeningDesired = false;
    this.isListening = false;
    clearTimeout(this.restartTimeout);

    this.closeMicrophone();
    this.resetVad('armed');
    this.transcribing = false;

    if (this.recognition) {
      try { this.recognition.stop(); } catch { /* ignora */ }
    }

    this.emitStatus({ isListening: false });
    this.onVolumeCallback?.(0, null);
  }

  toggleListening(onResult, onStatus) {
    if (this.isListening || this.isListeningDesired) {
      this.stopListening();
    } else {
      this.startListening(onResult, onStatus);
    }
  }

  // Lista os microfones do sistema (rótulos aparecem após a 1ª permissão)
  async listInputDevices() {
    try {
      if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) return [];
      let stream = null;
      try {
        stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      } catch { /* sem permissão ainda: devices vêm sem rótulo */ }
      const devices = await navigator.mediaDevices.enumerateDevices();
      const inputs = (devices || [])
        .filter(d => d && d.kind === 'audioinput')
        .map(d => ({ deviceId: d.deviceId, label: d.label || 'Microfone (nome indisponível)' }));
      if (stream) stream.getTracks().forEach(t => t.stop());
      return inputs;
    } catch {
      return [];
    }
  }

  // Teste rápido do microfone: retorna o pico de áudio capturado (0..100)
  async testMicrophone() {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: this.settings.micDeviceId
          ? { deviceId: { ideal: this.settings.micDeviceId }, echoCancellation: true, noiseSuppression: true, autoGainControl: true }
          : { echoCancellation: true, noiseSuppression: true, autoGainControl: true }
      });
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      const ctx = new AudioCtx();
      const source = ctx.createMediaStreamSource(stream);
      const analyser = ctx.createAnalyser();
      analyser.fftSize = 1024;
      source.connect(analyser);
      const buffer = new Float32Array(analyser.fftSize);

      let peak = 0;
      const started = Date.now();
      await new Promise((resolve) => {
        const tick = () => {
          analyser.getFloatTimeDomainData(buffer);
          let sum = 0;
          for (let i = 0; i < buffer.length; i++) sum += buffer[i] * buffer[i];
          peak = Math.max(peak, Math.sqrt(sum / buffer.length));
          if (Date.now() - started < 1500) requestAnimationFrame(tick);
          else resolve();
        };
        tick();
      });

      stream.getTracks().forEach(t => t.stop());
      try { ctx.close(); } catch { /* ignora */ }

      const track = (stream.getAudioTracks && stream.getAudioTracks()[0]) || null;
      const raw = Number(peak.toFixed(4));
      const device = track?.label || 'desconhecido';
      const base = { raw, device, muted: !!track?.muted, state: track?.readyState || null };

      // Silêncio digital (~zero) = dispositivo errado/mudo: sinaliza como falha
      if (raw <= 0.0005) {
        return {
          ...base,
          ok: false,
          peak: 0,
          error: 'mic-silent',
          message: `O microfone "${device}" não está capturando áudio (nível ~0). Continue falando durante o teste; se persistir, escolha outro microfone na lista ou verifique se não está mudo no Windows.`
        };
      }

      return { ...base, ok: true, peak: Math.round(Math.min(1, peak * 4) * 100) };
    } catch (err) {
      return {
        ok: false,
        error: err?.name === 'NotFoundError' ? 'mic-missing' : 'mic-denied',
        message: humanError(err?.name === 'NotFoundError' ? 'mic-missing' : 'mic-denied')
      };
    }
  }
}

export const speechService = new SpeechEngine();
