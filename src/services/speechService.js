// Speech Recognition and Text-To-Speech (TTS) Engine for J.A.R.V.I.S.
import { sounds } from './soundEffects';

class SpeechEngine {
  constructor() {
    this.recognition = null;
    this.isListening = false;
    this.isListeningDesired = false; // Flag para auto-reconnect incondicional
    this.isSpeaking = false;
    this.onResultCallback = null;
    this.onStatusCallback = null;
    this.onVolumeCallback = null;
    this.selectedVoice = null;
    this.audioContext = null;
    this.analyser = null;
    this.microphoneStream = null;
    this.animationFrameId = null;
    this.restartTimeout = null;

    this.initRecognition();
    this.initVoices();
  }

  initVoices() {
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      const loadVoices = () => {
        const voices = window.speechSynthesis.getVoices();
        // Priorizar vozes neurais ultra-realistas (Microsoft Online Natural, Google Natural, pt-BR)
        this.selectedVoice = 
          voices.find(v => (v.name.includes('Natural') || v.name.includes('Neural')) && v.lang.includes('pt-BR')) ||
          voices.find(v => v.name.includes('Antonio') && v.lang.includes('pt-BR')) ||
          voices.find(v => v.name.includes('Francisca') && v.lang.includes('pt-BR')) ||
          voices.find(v => v.name.includes('Google') && v.lang.includes('pt-BR')) ||
          voices.find(v => v.lang.includes('pt-BR')) ||
          voices.find(v => v.lang.includes('pt')) ||
          voices[0] || null;
      };

      loadVoices();
      if (window.speechSynthesis.onvoiceschanged !== undefined) {
        window.speechSynthesis.onvoiceschanged = loadVoices;
      }
    }
  }

  initRecognition() {
    if (typeof window === 'undefined') return;

    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (SpeechRecognition) {
      this.recognition = new SpeechRecognition();
      this.recognition.continuous = true;
      this.recognition.interimResults = true;
      this.recognition.lang = 'pt-BR';

      this.recognition.onstart = () => {
        this.isListening = true;
        sounds.playClick();
        if (this.onStatusCallback) this.onStatusCallback({ isListening: true });
      };

      this.recognition.onend = () => {
        this.isListening = false;
        // SE o usuário deseja que o microfone fique ouvindo, reconecta imediatamente!
        if (this.isListeningDesired) {
          clearTimeout(this.restartTimeout);
          this.restartTimeout = setTimeout(() => {
            if (this.isListeningDesired) {
              try {
                this.recognition.start();
                this.isListening = true;
                if (this.onStatusCallback) this.onStatusCallback({ isListening: true });
              } catch (err) {
                // Se já estiver iniciando, ignora
              }
            }
          }, 200);
        } else {
          if (this.onStatusCallback) this.onStatusCallback({ isListening: false });
        }
      };

      this.recognition.onerror = (event) => {
        // 'no-speech' e 'audio-capture' temporário são esperados quando o usuário fica em silêncio.
        // NUNCA desligue a escuta por causa de silêncio.
        if (event.error === 'no-speech') {
          return;
        }
        console.warn('Speech recognition warning:', event.error);
        if (event.error === 'not-allowed') {
          this.isListeningDesired = false;
          this.isListening = false;
          if (this.onStatusCallback) this.onStatusCallback({ isListening: false, error: event.error });
        }
      };

      this.recognition.onresult = (event) => {
        let interimTranscript = '';
        let finalTranscript = '';

        for (let i = event.resultIndex; i < event.results.length; ++i) {
          const text = event.results[i][0]?.transcript || '';
          if (event.results[i].isFinal) {
            finalTranscript += text;
          } else {
            interimTranscript += text;
          }
        }

        const trimmedFinal = finalTranscript.trim();
        const trimmedInterim = interimTranscript.trim();

        if (this.onResultCallback) {
          this.onResultCallback({
            final: trimmedFinal,
            interim: trimmedInterim
          });
        }
      };
    }
  }

  async startListening(onResult, onStatus) {
    this.onResultCallback = onResult;
    this.onStatusCallback = onStatus;
    this.isListeningDesired = true;

    if (!this.recognition) {
      if (onStatus) onStatus({ isListening: false, unsupported: true });
      return;
    }

    try {
      this.recognition.start();
      this.startAudioAnalyzer();
    } catch (e) {
      // Ignora erro se já estiver iniciado
    }
  }

  stopListening() {
    this.isListeningDesired = false;
    clearTimeout(this.restartTimeout);
    if (this.recognition) {
      try {
        this.recognition.stop();
      } catch (e) {}
    }
    this.isListening = false;
    if (this.onStatusCallback) this.onStatusCallback({ isListening: false });
    this.stopAudioAnalyzer();
  }

  toggleListening(onResult, onStatus) {
    if (this.isListening || this.isListeningDesired) {
      this.stopListening();
    } else {
      this.startListening(onResult, onStatus);
    }
  }

  async startAudioAnalyzer(onVolume) {
    this.onVolumeCallback = onVolume || this.onVolumeCallback;
    try {
      if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        this.microphoneStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        this.audioContext = new AudioCtx();
        const source = this.audioContext.createMediaStreamSource(this.microphoneStream);
        this.analyser = this.audioContext.createAnalyser();
        this.analyser.fftSize = 64;
        source.connect(this.analyser);

        const dataArray = new Uint8Array(this.analyser.frequencyBinCount);
        const update = () => {
          if (!this.analyser) return;
          this.analyser.getByteFrequencyData(dataArray);
          let sum = 0;
          for (let i = 0; i < dataArray.length; i++) {
            sum += dataArray[i];
          }
          const avg = sum / dataArray.length;
          if (this.onVolumeCallback) {
            this.onVolumeCallback(avg, dataArray);
          }
          this.animationFrameId = requestAnimationFrame(update);
        };
        update();
      }
    } catch (err) {
      console.warn('Microphone stream error:', err);
    }
  }

  stopAudioAnalyzer() {
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }
    if (this.microphoneStream) {
      this.microphoneStream.getTracks().forEach(track => track.stop());
      this.microphoneStream = null;
    }
    if (this.audioContext) {
      try {
        this.audioContext.close();
      } catch (e) {}
      this.audioContext = null;
    }
  }

  speak(text, onStart, onEnd) {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;

    window.speechSynthesis.cancel(); // Para qualquer fala pendente

    // Limpeza de marcações de markdown e quebra em sentenças naturais para respiração humana
    const cleanText = text.replace(/[*#_`]/g, '').trim();
    const utterance = new SpeechSynthesisUtterance(cleanText);
    
    if (this.selectedVoice) {
      utterance.voice = this.selectedVoice;
    }
    
    // Configurações inspiradas na voz calma, natural e dinâmica do ChatGPT / Jarvis
    utterance.rate = 1.02; // Ritmo natural e fluido
    utterance.pitch = 0.94; // Tom levemente mais encorpado e sofisticado
    utterance.volume = 1.0;

    utterance.onstart = () => {
      this.isSpeaking = true;
      if (onStart) onStart();
    };

    utterance.onend = () => {
      this.isSpeaking = false;
      if (onEnd) onEnd();
    };

    utterance.onerror = () => {
      this.isSpeaking = false;
      if (onEnd) onEnd();
    };

    window.speechSynthesis.speak(utterance);
  }

  stopSpeaking() {
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      window.speechSynthesis.cancel();
      this.isSpeaking = false;
    }
  }
}

export const speechService = new SpeechEngine();