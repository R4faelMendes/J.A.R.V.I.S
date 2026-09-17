// Speech Recognition and Text-To-Speech (TTS) Engine for J.A.R.V.I.S.
import { sounds } from './soundEffects';

class SpeechEngine {
  constructor() {
    this.recognition = null;
    this.isListening = false;
    this.isSpeaking = false;
    this.onResultCallback = null;
    this.onStatusCallback = null;
    this.onVolumeCallback = null;
    this.selectedVoice = null;
    this.audioContext = null;
    this.analyser = null;
    this.microphoneStream = null;
    this.animationFrameId = null;

    this.initRecognition();
    this.initVoices();
  }

  initVoices() {
    if (typeof window !== 'undefined' && 'speechSynthesis' in window) {
      const loadVoices = () => {
        const voices = window.speechSynthesis.getVoices();
        // Prefer Portuguese voices (pt-BR, pt-PT) or English (en-GB / en-US)
        this.selectedVoice = 
          voices.find(v => v.lang.includes('pt-BR') && v.name.includes('Google')) ||
          voices.find(v => v.lang.includes('pt-BR')) ||
          voices.find(v => v.lang.includes('pt-PT')) ||
          voices.find(v => v.lang.includes('en-GB')) ||
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
        if (this.onStatusCallback) this.onStatusCallback({ isListening: false });
      };

      this.recognition.onerror = (event) => {
        console.warn('Speech recognition error:', event.error);
        if (this.onStatusCallback) this.onStatusCallback({ isListening: false, error: event.error });
      };

      this.recognition.onresult = (event) => {
        let interimTranscript = '';
        let finalTranscript = '';

        for (let i = event.resultIndex; i < event.results.length; ++i) {
          if (event.results[i].isFinal) {
            finalTranscript += event.results[i][0].transcript;
          } else {
            interimTranscript += event.results[i][0].transcript;
          }
        }

        if (this.onResultCallback) {
          this.onResultCallback({
            final: finalTranscript.trim(),
            interim: interimTranscript.trim()
          });
        }
      };
    }
  }

  async startListening(onResult, onStatus) {
    this.onResultCallback = onResult;
    this.onStatusCallback = onStatus;

    if (!this.recognition) {
      if (onStatus) onStatus({ isListening: false, unsupported: true });
      return;
    }

    try {
      this.recognition.start();
      this.startAudioAnalyzer();
    } catch (e) {
      console.warn('Recognition start caught error:', e);
    }
  }

  stopListening() {
    if (this.recognition && this.isListening) {
      this.recognition.stop();
    }
    this.stopAudioAnalyzer();
  }

  toggleListening(onResult, onStatus) {
    if (this.isListening) {
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
      this.audioContext.close();
      this.audioContext = null;
    }
  }

  speak(text, onStart, onEnd) {
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;

    window.speechSynthesis.cancel(); // Stop any pending speech

    const cleanText = text.replace(/[*#_`]/g, '').slice(0, 350); // limit speech chunk
    const utterance = new SpeechSynthesisUtterance(cleanText);
    
    if (this.selectedVoice) {
      utterance.voice = this.selectedVoice;
    }
    utterance.rate = 1.05; // slightly faster, tactical pace
    utterance.pitch = 0.95; // slightly deeper, composed Jarvis persona

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