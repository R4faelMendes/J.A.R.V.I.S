# Esfera de áudio no centro do desktop

O início usa `AudioOrbView` no lugar do reator ARC. O botão de montagem foi removido. A esfera tem volume suave, contorno ondulado, halo e pequenos picos radiais inspirados no visualizador circular da referência enviada pelo usuário.

`NivelFalaSampleProvider` mede o RMS das amostras PCM consumidas pelo player do Piper, sem modificar o som. Durante a fala, `MotorVoz.NivelAudio` retorna essa medida em vez do microfone. Durante a escuta, continua retornando o nível do microfone. A esfera interpola ataque e queda; o movimento da borda é dirigido pela amplitude da fala, sem análise FFT ou acesso ao áudio de outros aplicativos.

Em repouso, o timer da esfera para. Durante a fala/escuta, usa até 30 fps; em economia, 8 fps. Animações reduzidas mantêm o desenho estático. O fallback WPF também desenha a esfera, sem retornar ao reator.

A mudança se aplica ao centro da interface desktop. O mobile mantém o visual existente.

Verificações: `EsferaAudioTest`, `InterfaceSmokeTest`, compilação Release e teste de abertura. Prévia: `artifacts/preview/Esfera-audio-falando.png`.
