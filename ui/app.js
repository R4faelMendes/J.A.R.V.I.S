/* ==========================================================================
   JARVIS 2.0 — HUD HOLOGRÁFICO CINEMATOGRÁFICO
   WebGL Three.js (Qualidade) + Canvas 2D (Economia) + Web Audio FX
   100% Local, Sem Frameworks Externos, Offline.
   ========================================================================== */

(() => {
  "use strict";

  const $ = (id) => document.getElementById(id);

  // Elementos do DOM
  const canvas3D = $("orbe-3d");
  const canvas2D = $("orbe-2d");
  const player = $("player");
  const elEstado = $("estado");
  const elHeaderStatus = $("header-status-txt");
  const elTranscricao = $("transcricao");
  const elResposta = $("resposta");
  const elFerramenta = $("ferramenta");
  const elLog = $("log");
  const elRelogio = $("relogio");
  const elData = $("data-extenso");
  const elCpuBar = $("cpu-bar");
  const elCpuTxt = $("cpu-txt");
  const elRamBar = $("ram-bar");
  const elRamTxt = $("ram-txt");
  const elAudioBar = $("audio-in-bar");
  const elAudioTxt = $("audio-in-txt");
  const modalConfirma = $("modal-confirmacao");
  const modalDetalhes = $("modal-acao-detalhes");
  const bootScreen = $("boot-screen");
  const bootFill = $("boot-fill");
  const bootLog = $("boot-log");

  // Estado da Aplicação
  let estadoAtual = "dormindo"; // "dormindo", "ouvindo", "pensando", "falando", "erro"
  let modoEconomia = false;
  let audioFxAtivo = true;
  let audioLevel = 0.05;
  let targetAudioLevel = 0.05;
  let fpsHistory = [];
  let lastFrameTime = performance.now();
  let actionPending = false;

  // Web Audio FX Sintetizador Local
  let sfxCtx = null;
  function getAudioContext() {
    if (!sfxCtx) {
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      sfxCtx = new AudioCtx();
    }
    if (sfxCtx.state === "suspended") {
      sfxCtx.resume();
    }
    return sfxCtx;
  }

  function playSFX(tipo) {
    if (!audioFxAtivo) return;
    try {
      const ctx = getAudioContext();
      const now = ctx.currentTime;
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.connect(gain);
      gain.connect(ctx.destination);

      if (tipo === "boot") {
        osc.type = "sine";
        osc.frequency.setValueAtTime(220, now);
        osc.frequency.exponentialRampToValueAtTime(880, now + 0.3);
        gain.gain.setValueAtTime(0.15, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.35);
        osc.start(now);
        osc.stop(now + 0.35);
      } else if (tipo === "wake") {
        osc.type = "triangle";
        osc.frequency.setValueAtTime(440, now);
        osc.frequency.setValueAtTime(660, now + 0.08);
        gain.gain.setValueAtTime(0.2, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.2);
        osc.start(now);
        osc.stop(now + 0.2);
      } else if (tipo === "confirm") {
        osc.type = "sine";
        osc.frequency.setValueAtTime(587.33, now);
        osc.frequency.setValueAtTime(880, now + 0.07);
        gain.gain.setValueAtTime(0.25, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.25);
        osc.start(now);
        osc.stop(now + 0.25);
      } else if (tipo === "error") {
        osc.type = "sawtooth";
        osc.frequency.setValueAtTime(300, now);
        osc.frequency.linearRampToValueAtTime(150, now + 0.25);
        gain.gain.setValueAtTime(0.2, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.3);
        osc.start(now);
        osc.stop(now + 0.3);
      } else if (tipo === "click") {
        osc.type = "sine";
        osc.frequency.setValueAtTime(800, now);
        gain.gain.setValueAtTime(0.08, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.05);
        osc.start(now);
        osc.stop(now + 0.05);
      }
    } catch (e) {
      // Ignora erro de áudio se navegador bloquear autoplay
    }
  }

  // Relógio e Data do HUD
  function atualizarRelogio() {
    const agora = new Date();
    elRelogio.textContent = agora.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit", second: "2-digit" });
    const dias = ["DOM", "SEG", "TER", "QUA", "QUI", "SEX", "SÁB"];
    const meses = ["JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT", "NOV", "DEZ"];
    elData.textContent = `${dias[agora.getDay()]}, ${agora.getDate()} ${meses[agora.getMonth()]} ${agora.getFullYear()}`;
  }
  setInterval(atualizarRelogio, 1000);
  atualizarRelogio();

  // Sequência de Boot Futurista
  let bootCompleto = false;
  function executarBoot() {
    const passos = [
      { progresso: 25, msg: "INICIALIZANDO NÚCLEO HOLOGRÁFICO 3D...", delay: 400 },
      { progresso: 50, msg: "CONECTANDO CÉREBRO OLLAMA & PIPER TTS...", delay: 900 },
      { progresso: 75, msg: "CALIBRANDO SENSORES DE ÁUDIO & WAKE WORD...", delay: 1500 },
      { progresso: 100, msg: "TODOS OS SISTEMAS ONLINE // JARVIS PRONTO", delay: 2100 }
    ];

    passos.forEach((p) => {
      setTimeout(() => {
        if (bootCompleto) return;
        bootFill.style.width = p.progresso + "%";
        bootLog.textContent = p.msg;
        playSFX("click");
      }, p.delay);
    });

    setTimeout(() => {
      finalizarBoot();
    }, 2600);
  }

  function finalizarBoot() {
    if (bootCompleto) return;
    bootCompleto = true;
    document.body.classList.remove("booting");
    playSFX("boot");
    adicionarLog("[SISTEMA] Inicialização do núcleo completada.");
  }

  window.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && !bootCompleto) {
      finalizarBoot();
    }
  });
  executarBoot();

  // Gerenciamento de Logs
  function adicionarLog(msg) {
    const d = new Date().toLocaleTimeString("pt-BR");
    const div = document.createElement("div");
    div.className = "log-entry";
    div.textContent = `[${d}] ${msg}`;
    elLog.appendChild(div);
    elLog.scrollTop = elLog.scrollHeight;
  }

  // Efeito de Digitação Suave
  function digitarTexto(elemento, texto, vel = 15) {
    elemento.innerHTML = "";
    let i = 0;
    const span = document.createElement("span");
    const cursor = document.createElement("span");
    cursor.className = "typing-cursor";
    elemento.appendChild(span);
    elemento.appendChild(cursor);

    const timer = setInterval(() => {
      span.textContent = texto.slice(0, ++i);
      if (i >= texto.length) {
        clearInterval(timer);
        setTimeout(() => cursor.remove(), 2500);
      }
    }, vel);
  }

  // Mudança de Estados com Estilo e SFX
  function definirEstado(novo) {
    if (estadoAtual === novo) return;
    const anterior = estadoAtual;
    estadoAtual = novo;

    elEstado.textContent = novo.toUpperCase();
    elEstado.className = `state-main ${novo}`;
    elHeaderStatus.textContent = `SISTEMA // ${novo.toUpperCase()}`;

    if (novo === "ouvindo") {
      playSFX("wake");
      $("badge-mic").classList.add("active");
    } else {
      $("badge-mic").classList.remove("active");
    }

    if (novo === "erro") {
      playSFX("error");
    }
    adicionarLog(`Transição de estado: ${anterior.toUpperCase()} -> ${novo.toUpperCase()}`);
  }

  // ==========================================================================
  // RENDERIZADOR 3D HOLOGRÁFICO (Three.js WebGL)
  // ==========================================================================
  let renderer3D, scene3D, camera3D;
  let coreMesh, ringGroup, rings = [], particleCloud, dialRing;
  let animTime = 0;

  function initThreeJS() {
    if (typeof THREE === "undefined") {
      ativarModoEconomia(true);
      return;
    }

    try {
      scene3D = new THREE.Scene();
      camera3D = new THREE.PerspectiveCamera(45, 1, 0.1, 1000);
      camera3D.position.z = 18;

      renderer3D = new THREE.WebGLRenderer({
        canvas: canvas3D,
        alpha: true,
        antialias: true,
        powerPreference: "high-performance"
      });
      renderer3D.setSize(480, 480);
      renderer3D.setPixelRatio(Math.min(window.devicePixelRatio, 2));

      // Iluminação
      const ambientLight = new THREE.AmbientLight(0x00E5FF, 0.8);
      scene3D.add(ambientLight);
      const pointLight = new THREE.PointLight(0x00E5FF, 2, 50);
      pointLight.position.set(0, 0, 10);
      scene3D.add(pointLight);

      // Núcleo Esférico Central
      const coreGeo = new THREE.SphereGeometry(2.4, 32, 32);
      const coreMat = new THREE.MeshBasicMaterial({
        color: 0x00E5FF,
        wireframe: true,
        transparent: true,
        opacity: 0.8
      });
      coreMesh = new THREE.Mesh(coreGeo, coreMat);
      scene3D.add(coreMesh);

      // Anéis Concêntricos 3D
      ringGroup = new THREE.Group();
      const ringConfigs = [
        { r: 3.6, thick: 0.08, seg: 64, rotSpeed: { x: 0.005, y: 0.015, z: 0.008 } },
        { r: 4.8, thick: 0.06, seg: 48, rotSpeed: { x: -0.012, y: 0.006, z: -0.01 } },
        { r: 6.0, thick: 0.05, seg: 64, rotSpeed: { x: 0.008, y: -0.014, z: 0.005 } },
        { r: 7.2, thick: 0.04, seg: 32, rotSpeed: { x: -0.005, y: 0.008, z: 0.015 } }
      ];

      rings = ringConfigs.map((cfg) => {
        const ringGeo = new THREE.TorusGeometry(cfg.r, cfg.thick, 8, cfg.seg);
        const ringMat = new THREE.MeshBasicMaterial({
          color: 0x00E5FF,
          transparent: true,
          opacity: 0.65
        });
        const mesh = new THREE.Mesh(ringGeo, ringMat);
        ringGroup.add(mesh);
        return { mesh, cfg };
      });
      scene3D.add(ringGroup);

      // Aro Externo Estilo Mostrador Graduado
      const dialGeo = new THREE.RingGeometry(7.8, 8.0, 64);
      const dialMat = new THREE.MeshBasicMaterial({
        color: 0x2979FF,
        side: THREE.DoubleSide,
        transparent: true,
        opacity: 0.4
      });
      dialRing = new THREE.Mesh(dialGeo, dialMat);
      scene3D.add(dialRing);

      // Nuvem de Partículas em Órbita
      const partCount = 180;
      const partGeo = new THREE.BufferGeometry();
      const partPos = new Float32Array(partCount * 3);
      for (let i = 0; i < partCount * 3; i += 3) {
        const rad = 4 + Math.random() * 5;
        const theta = Math.random() * Math.PI * 2;
        const phi = (Math.random() - 0.5) * Math.PI;
        partPos[i] = rad * Math.cos(theta) * Math.cos(phi);
        partPos[i + 1] = rad * Math.sin(theta) * Math.cos(phi);
        partPos[i + 2] = rad * Math.sin(phi);
      }
      partGeo.setAttribute("position", new THREE.BufferAttribute(partPos, 3));
      const partMat = new THREE.PointsMaterial({
        color: 0xA5F3FF,
        size: 0.15,
        transparent: true,
        opacity: 0.8
      });
      particleCloud = new THREE.Points(partGeo, partMat);
      scene3D.add(particleCloud);

    } catch (e) {
      console.warn("WebGL initialization failed, falling back to 2D Canvas:", e);
      ativarModoEconomia(true);
    }
  }

  function render3D(t) {
    if (!renderer3D) return;

    // Suavização do nível de áudio
    audioLevel += (targetAudioLevel - audioLevel) * 0.15;
    const pulseFactor = 1 + audioLevel * 0.8;

    // Ajuste de Cor Conforme Estado e Tema
    const isAmber = document.body.classList.contains("amber-theme");
    let hexColor = isAmber ? 0xFFB300 : 0x00E5FF;
    let secColor = isAmber ? 0xFF9100 : 0x2979FF;

    if (estadoAtual === "erro") {
      hexColor = 0xFF3D57;
      secColor = 0xFF3D57;
    } else if (estadoAtual === "pensando") {
      hexColor = isAmber ? 0xFFE082 : 0x2979FF;
    }

    coreMesh.material.color.setHex(hexColor);
    particleCloud.material.color.setHex(hexColor);

    // Animação do Núcleo
    const coreScale = (estadoAtual === "ouvindo" ? 1.2 : 1.0) * pulseFactor;
    coreMesh.scale.set(coreScale, coreScale, coreScale);
    coreMesh.rotation.y += (estadoAtual === "pensando" ? 0.05 : 0.01);
    coreMesh.rotation.x += 0.005;

    // Rotação dos Anéis 3D
    rings.forEach(({ mesh, cfg }, idx) => {
      mesh.material.color.setHex(idx % 2 === 0 ? hexColor : secColor);
      const speedMult = estadoAtual === "pensando" ? 3.5 : (estadoAtual === "ouvindo" ? 1.8 : 1.0);
      mesh.rotation.x += cfg.rotSpeed.x * speedMult;
      mesh.rotation.y += cfg.rotSpeed.y * speedMult;
      mesh.rotation.z += cfg.rotSpeed.z * speedMult;
      const ringScale = 1 + audioLevel * (0.15 + idx * 0.05);
      mesh.scale.set(ringScale, ringScale, ringScale);
    });

    // Rotação do Mostrador e Partículas
    dialRing.rotation.z -= 0.002;
    particleCloud.rotation.y += (estadoAtual === "pensando" ? 0.02 : 0.003);
    particleCloud.rotation.x += 0.002;

    renderer3D.render(scene3D, camera3D);
  }

  // ==========================================================================
  // RENDERIZADOR 2D CANVAS (Fallback e Modo Economia)
  // ==========================================================================
  const ctx2D = canvas2D.getContext("2d");

  function render2D(t) {
    const W = canvas2D.width, H = canvas2D.height;
    const cx = W / 2, cy = H / 2;
    ctx2D.clearRect(0, 0, W, H);

    audioLevel += (targetAudioLevel - audioLevel) * 0.15;
    const isAmber = document.body.classList.contains("amber-theme");
    const cor = estadoAtual === "erro" ? "#FF3D57" : (isAmber ? "#FFB300" : "#00E5FF");
    const pulso = 1 + audioLevel * 0.6;

    // Núcleo com Gradiente Radial
    const g = ctx2D.createRadialGradient(cx, cy, 5, cx, cy, 70 * pulso);
    g.addColorStop(0, cor);
    g.addColorStop(1, "transparent");
    ctx2D.globalAlpha = 0.4 + audioLevel * 0.5;
    ctx2D.fillStyle = g;
    ctx2D.beginPath();
    ctx2D.arc(cx, cy, 70 * pulso, 0, Math.PI * 2);
    ctx2D.fill();

    // 4 Anéis Concêntricos Segmentados
    for (let i = 0; i < 4; i++) {
      const r = 80 + i * 32;
      const speed = (i % 2 === 0 ? 1 : -1) * (estadoAtual === "pensando" ? 0.004 : 0.001);
      const ang0 = t * speed * (i + 1);

      ctx2D.strokeStyle = cor;
      ctx2D.lineWidth = 2;
      ctx2D.globalAlpha = 0.5 + (i * 0.1);
      ctx2D.beginPath();
      ctx2D.arc(cx, cy, r * pulso, ang0, ang0 + Math.PI * 1.4);
      ctx2D.stroke();
    }

    // Partículas em Órbita 2D
    const partCount = 36;
    ctx2D.fillStyle = cor;
    for (let i = 0; i < partCount; i++) {
      const ang = t * 0.0015 + (i * (Math.PI * 2 / partCount));
      const dist = (estadoAtual === "pensando" ? 60 : 130 + (i % 40)) * pulso;
      const px = cx + Math.cos(ang) * dist;
      const py = cy + Math.sin(ang) * dist;
      ctx2D.globalAlpha = 0.6;
      ctx2D.fillRect(px, py, 2.5, 2.5);
    }
  }

  // ==========================================================================
  // LOOP PRINCIPAL DE ANIMAÇÃO COM MONITORAMENTO DE PERFORMANCE
  // ==========================================================================
  function animate(t) {
    requestAnimationFrame(animate);

    // Pausa ou reduz animação quando a aba está em background
    if (document.hidden) return;

    const delta = t - lastFrameTime;
    lastFrameTime = t;
    const currentFPS = 1000 / Math.max(delta, 1);
    fpsHistory.push(currentFPS);
    if (fpsHistory.length > 60) fpsHistory.shift();

    // Detecção de queda de frame para alternar automaticamente para economia
    if (!modoEconomia && fpsHistory.length === 60) {
      const avgFPS = fpsHistory.reduce((a, b) => a + b, 0) / fpsHistory.length;
      if (avgFPS < 28) {
        adicionarLog("[PERF] FPS abaixo de 30. Ativando modo Economia automaticamente.");
        ativarModoEconomia(true);
      }
    }

    if (modoEconomia) {
      render2D(t);
    } else {
      render3D(t);
    }

    // Animação das barras de onda no HUD
    const waveBars = document.querySelectorAll(".wave-bar");
    waveBars.forEach((bar, idx) => {
      const h = Math.max(4, Math.min(16, (audioLevel * 20) * (1 + Math.sin(t * 0.01 + idx))));
      bar.style.height = `${h}px`;
    });
  }

  function ativarModoEconomia(eco) {
    modoEconomia = eco;
    if (eco) {
      canvas3D.classList.add("oculto");
      canvas2D.classList.remove("oculto");
      $("eco-tag").textContent = "ECO";
      $("btn-eco").classList.add("ativo");
    } else {
      canvas2D.classList.add("oculto");
      canvas3D.classList.remove("oculto");
      $("eco-tag").textContent = "3D";
      $("btn-eco").classList.remove("ativo");
    }
  }

  initThreeJS();
  requestAnimationFrame(animate);

  // ==========================================================================
  // PROTOCOLO DE RESPOSTA & HISTÓRICO (Tarefa 3)
  // ==========================================================================
  const MAX_HISTORICO = 5;
  let historicoRespostas = []; // [{ id, texto, origem }]
  let mensagemAtiva = null;    // { id, seqs: Set, texto, origem, spanElement, containerElement }
  let deltaBuffer = [];
  let rafScheduled = false;

  function renderizarHistoricoERespostaAtiva() {
    elResposta.textContent = "";
    if (historicoRespostas.length === 0 && !mensagemAtiva) {
      const span = document.createElement("span");
      span.className = "placeholder-text";
      span.textContent = "Sistemas prontos. Aguardando comando...";
      elResposta.appendChild(span);
      return;
    }

    // Renderiza as até 5 respostas anteriores
    historicoRespostas.forEach((item, idx) => {
      const div = document.createElement("div");
      div.className = "response-item past";
      const tag = document.createElement("span");
      tag.className = "response-tag";
      tag.textContent = `[#${idx + 1}] `;
      div.appendChild(tag);
      const textSpan = document.createElement("span");
      textSpan.textContent = item.texto;
      div.appendChild(textSpan);
      elResposta.appendChild(div);
    });

    // Renderiza a resposta ativa se houver
    if (mensagemAtiva) {
      const div = document.createElement("div");
      div.className = "response-item active";
      const tag = document.createElement("span");
      tag.className = "response-tag active-tag";
      tag.textContent = "▶ ";
      div.appendChild(tag);
      const textSpan = document.createElement("span");
      textSpan.textContent = mensagemAtiva.texto;
      div.appendChild(textSpan);
      mensagemAtiva.spanElement = textSpan;
      mensagemAtiva.containerElement = div;
      elResposta.appendChild(div);
    }

    elResposta.scrollTop = elResposta.scrollHeight;
  }

  function iniciarNovaResposta(id, origem = "") {
    if (mensagemAtiva && mensagemAtiva.id === id) return;
    if (mensagemAtiva && mensagemAtiva.texto.trim()) {
      historicoRespostas.push({
        id: mensagemAtiva.id,
        texto: mensagemAtiva.texto,
        origem: mensagemAtiva.origem
      });
      if (historicoRespostas.length > MAX_HISTORICO) {
        historicoRespostas.shift();
      }
    }
    mensagemAtiva = {
      id: id,
      seqs: new Set([0]),
      texto: "",
      origem: origem,
      spanElement: null,
      containerElement: null
    };
    deltaBuffer = [];
    renderizarHistoricoERespostaAtiva();
  }

  function processarDeltaBuffer() {
    if (!mensagemAtiva || deltaBuffer.length === 0) {
      rafScheduled = false;
      return;
    }
    let alterou = false;
    for (let i = 0; i < deltaBuffer.length; i++) {
      const item = deltaBuffer[i];
      if (item.id === mensagemAtiva.id) {
        mensagemAtiva.texto += item.delta;
        alterou = true;
      }
    }
    deltaBuffer = [];
    if (alterou && mensagemAtiva.spanElement) {
      mensagemAtiva.spanElement.textContent = mensagemAtiva.texto;
      elResposta.scrollTop = elResposta.scrollHeight;
    }
    rafScheduled = false;
  }

  function receberDelta(id, seq, delta) {
    if (!mensagemAtiva || mensagemAtiva.id !== id) {
      iniciarNovaResposta(id);
    }
    if (seq !== undefined && mensagemAtiva.seqs.has(seq)) {
      // Ignora duplicado (mesmo id e seq)
      return;
    }
    if (seq !== undefined) mensagemAtiva.seqs.add(seq);
    deltaBuffer.push({ id, delta });
    if (!rafScheduled) {
      rafScheduled = true;
      requestAnimationFrame(processarDeltaBuffer);
    }
  }

  function finalizarResposta(id, seq, textoFinal) {
    if (!mensagemAtiva || mensagemAtiva.id !== id) {
      iniciarNovaResposta(id);
    }
    if (seq !== undefined && mensagemAtiva.seqs.has(seq) && mensagemAtiva.texto === textoFinal) {
      return;
    }
    if (seq !== undefined) mensagemAtiva.seqs.add(seq);
    deltaBuffer = [];
    mensagemAtiva.texto = textoFinal;
    if (mensagemAtiva.spanElement) {
      mensagemAtiva.spanElement.textContent = textoFinal;
      elResposta.scrollTop = elResposta.scrollHeight;
    }
  }

  // ==========================================================================
  // WEBSOCKET & COMUNICAÇÃO EM TEMPO REAL (Singleton + Backoff Exponencial)
  // ==========================================================================
  let ws = null;
  let wsReconnectTimer = null;
  let wsBackoffMs = 1000;
  const WS_MAX_BACKOFF_MS = 16000;

  function conectarWebSocket() {
    if (wsReconnectTimer) {
      clearTimeout(wsReconnectTimer);
      wsReconnectTimer = null;
    }
    if (ws && (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING)) {
      return;
    }
    if (ws) {
      ws.onopen = null;
      ws.onmessage = null;
      ws.onerror = null;
      ws.onclose = null;
      try { ws.close(); } catch (e) {}
    }

    const proto = location.protocol === "https:" ? "wss" : "ws";
    try {
      ws = new WebSocket(`${proto}://${location.host}/ws`);
    } catch (e) {
      agendarReconexao();
      return;
    }

    ws.onopen = () => {
      wsBackoffMs = 1000;
      $("badge-ws").classList.add("active");
      adicionarLog("[LINK] Conexão WebSocket estabelecida.");
    };

    ws.onmessage = (ev) => {
      try {
        const d = JSON.parse(ev.data);
        tratarMensagemWS(d);
      } catch (e) {
        console.error("Erro ao processar mensagem do WebSocket:", e);
      }
    };

    ws.onclose = () => {
      $("badge-ws").classList.remove("active");
      agendarReconexao();
    };

    ws.onerror = () => {
      $("badge-ws").classList.remove("active");
    };
  }

  function agendarReconexao() {
    if (wsReconnectTimer) return;
    adicionarLog(`[LINK] Conexão perdida. Reconectando em ${wsBackoffMs / 1000}s...`);
    wsReconnectTimer = setTimeout(() => {
      wsReconnectTimer = null;
      wsBackoffMs = Math.min(wsBackoffMs * 2, WS_MAX_BACKOFF_MS);
      conectarWebSocket();
    }, wsBackoffMs);
  }

  function tratarMensagemWS(d) {
    if (d.tipo === "response_start") {
      iniciarNovaResposta(d.id, d.origem);
    } else if (d.tipo === "response_delta") {
      receberDelta(d.id, d.seq, d.delta);
    } else if (d.tipo === "response_end") {
      finalizarResposta(d.id, d.seq, d.texto_final);
    } else if (d.tipo === "transcricao") {
      if (d.texto) elTranscricao.textContent = d.texto;
    } else if (d.tipo === "loop_detectado") {
      adicionarLog(`[ALERTA] Loop de respostas detectado (${d.consecutivas} automáticas). Interrompido.`);
      definirEstado("erro");
    } else if (d.tipo === "confirmacao") {
      exibirModalConfirmacao(d.acao);
    }

    // Telemetria / Estado (NUNCA limpa ou altera o painel de resposta)
    if (d.estado) definirEstado(d.estado);
    if (typeof d.cpu === "number") {
      elCpuBar.style.width = `${Math.min(100, d.cpu)}%`;
      elCpuTxt.textContent = `${d.cpu.toFixed(0)}%`;
    }
    if (typeof d.ram === "number") {
      elRamBar.style.width = `${Math.min(100, d.ram)}%`;
      elRamTxt.textContent = `${d.ram.toFixed(0)}%`;
    }
    if (typeof d.nivel === "number") {
      targetAudioLevel = d.nivel;
      elAudioBar.style.width = `${Math.min(100, d.nivel * 100)}%`;
      elAudioTxt.textContent = `${(d.nivel * 100).toFixed(0)}%`;
    }
    if (d.ferramenta) {
      elFerramenta.textContent = d.ferramenta;
    }
  }

  conectarWebSocket();

  // ==========================================================================
  // ENVIO DE COMANDOS & MODAL DE CONFIRMAÇÃO
  // ==========================================================================
  async function enviarComando(texto) {
    if (!texto.trim()) return;
    definirEstado("pensando");
    elTranscricao.textContent = texto;
    $("entrada").value = "";
    playSFX("click");

    const t0 = performance.now();
    try {
      const resp = await fetch("/api/comando", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ texto, falar_pc: false })
      }).then((r) => r.json());

      const duracao = ((performance.now() - t0) / 1000).toFixed(2);
      $("tempo-resposta").textContent = `RESPOSTA GERADA EM ${resp.tempos_s ?? duracao} s`;

      if (resp.resposta) {
        finalizarResposta(resp.id || `cmd_${Date.now()}`, 999999, resp.resposta);
      }

      if (resp.precisa_confirmar) {
        exibirModalConfirmacao(resp.acao || { comando: texto });
      }

      if (resp.audio_wav_b64) {
        definirEstado("falando");
        targetAudioLevel = 0.8;
        player.src = "data:audio/wav;base64," + resp.audio_wav_b64;
        player.play().catch(() => {
          definirEstado("dormindo");
          targetAudioLevel = 0.05;
        });
        player.onended = () => {
          definirEstado("dormindo");
          targetAudioLevel = 0.05;
        };
      } else {
        definirEstado("dormindo");
      }
    } catch (err) {
      definirEstado("erro");
      finalizarResposta("erro", 0, "Erro de comunicação com o servidor.");
      adicionarLog(`[ERRO] Falha ao enviar comando: ${err}`);
    }
  }

  function exibirModalConfirmacao(acao) {
    actionPending = true;
    modalDetalhes.textContent = JSON.stringify(acao, null, 2);
    modalConfirma.classList.remove("oculto");
    playSFX("error");
  }

  function fecharModalConfirmacao() {
    actionPending = false;
    modalConfirma.classList.add("oculto");
  }

  async function confirmarAcao() {
    playSFX("confirm");
    fecharModalConfirmacao();
    definirEstado("pensando");
    const r = await fetch("/api/confirmar", { method: "POST" }).then((x) => x.json());
    if (r.resposta) {
      finalizarResposta(r.id || `conf_${Date.now()}`, 999999, r.resposta);
    }
    definirEstado("dormindo");
  }

  // ==========================================================================
  // EVENTOS DE INTERFACE & ATALHOS
  // ==========================================================================
  $("btn-enviar").onclick = () => enviarComando($("entrada").value);
  $("entrada").onkeydown = (e) => {
    if (e.key === "Enter") $("btn-enviar").onclick();
  };

  $("btn-parar").onclick = async () => {
    playSFX("click");
    player.pause();
    await fetch("/api/parar", { method: "POST" });
    definirEstado("dormindo");
    targetAudioLevel = 0.05;
    adicionarLog("[SISTEMA] Execução e fala interrompidas.");
  };

  $("btn-confirma").onclick = () => {
    if (actionPending) {
      confirmarAcao();
    } else {
      enviarComando("confirmo");
    }
  };

  $("modal-btn-confirmar").onclick = confirmarAcao;
  $("modal-btn-cancelar").onclick = fecharModalConfirmacao;

  $("btn-tema").onclick = () => {
    document.body.classList.toggle("amber-theme");
    playSFX("click");
  };

  $("btn-eco").onclick = () => {
    ativarModoEconomia(!modoEconomia);
    playSFX("click");
  };

  $("btn-audio-fx").onclick = () => {
    audioFxAtivo = !audioFxAtivo;
    $("btn-audio-fx").classList.toggle("ativo", audioFxAtivo);
    if (audioFxAtivo) playSFX("click");
  };

  // Atalho Global Ctrl+Alt+J
  window.addEventListener("keydown", (e) => {
    if (e.ctrlKey && e.altKey && (e.key === "j" || e.key === "J")) {
      e.preventDefault();
      $("entrada").focus();
      definirEstado("ouvindo");
      playSFX("wake");
    }
    if (actionPending && e.key === "Enter") {
      confirmarAcao();
    }
  });

  // Microfone do Navegador (PWA / Mobile)
  let micStream = null;
  let micAnalyser = null;
  $("btn-mic").onpointerdown = async () => {
    try {
      micStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const ctx = getAudioContext();
      const source = ctx.createMediaStreamSource(micStream);
      micAnalyser = ctx.createAnalyser();
      micAnalyser.fftSize = 256;
      source.connect(micAnalyser);
      definirEstado("ouvindo");
      targetAudioLevel = 0.6;
      $("btn-mic").classList.add("active");
    } catch (e) {
      adicionarLog("[MIC] Acesso ao microfone não concedido no navegador.");
    }
  };

  $("btn-mic").onpointerup = () => {
    if (micStream) {
      micStream.getTracks().forEach((t) => t.stop());
      micStream = null;
    }
    $("btn-mic").classList.remove("active");
    if (estadoAtual === "ouvindo") {
      definirEstado("dormindo");
      targetAudioLevel = 0.05;
    }
  };

  // Service Worker para PWA
  if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("/static/sw.js").catch(() => {});
  }
})();
