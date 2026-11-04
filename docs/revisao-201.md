# Jarvis 2.0.1 — entrega de 21/10/2026

## Alterações entregues

- Paletas grafite com acentos mais suaves: azul aço, champagne, lavanda, jade, rosé e nova obsidiana/prata. PC e mobile usam o mesmo catálogo e sincronizam pelo canal autenticado existente.
- Tema no desktop cobre fundos, bordas, gradientes, texto, ícones vetoriais, gráficos, seleção e cursor dos campos. Observa cores recriadas por atualização de estado e reaplica na próxima etapa de vinculação do WPF, sem alterar propriedades dentro da notificação que ainda está em execução. Observadores são removidos ao descarregar o componente; bindings e cores herdadas são preservados.
- Tema mobile cobre gradientes, bordas, campos, seletores, pesquisa, switches, barras de progresso, sliders, indicadores, navegação e ícones dos cartões. Observa alterações posteriores de cores, incluindo componentes carregados depois de trocar o tema. Cores de erro/bateria e texto neutro mantêm seu significado e contraste.
- Configurações do desktop herdam texto claro explicitamente; seleção e lista do ComboBox usam a cor correta. Corrigida a origem real do texto preto no cabeçalho da seção de tema. Texto escuro em botões claros continua legível.
- Os três anéis giram como corpos rígidos em eixos estáveis, com velocidades distintas de 12, -9 e 7 graus/s. Retirada a rotação contínua adicional do conjunto no plano da tela. Globo animado e geometria detalhada permanecem no renderizador compartilhado entre PC e mobile.
- Saudação e respostas usam o nome salvo nas configurações (Rafael neste computador). Comandos remotos também retornam o nome para a fala do celular. Não se injeta o nome no conteúdo completo de e-mails. A pergunta sobre o próprio nome é respondida localmente.

## Instalação Android

Usuário confirmou que o bloqueio ocorre ao instalar; não forneceu o texto exato. As orientações oficiais do Google identificam Acessibilidade entre as permissões que podem provocar bloqueio de APK baixado fora da loja:
https://developers.google.com/android/play-protect/warning-dev-guidance?hl=pt-BR

O APK padrão agora é 2.0.1, código 16, sem serviço de Acessibilidade, captura desse serviço ou recursos XML correspondentes. O manifesto real do APK foi inspecionado com aapt, além do manifesto intermediário. Não declara leitura de SMS, listener de notificações ou administração do aparelho. Nenhuma proteção do Android foi desativada. Mantém microfone, captura normal autorizada pelo Android, notificações próprias de agenda/mídia e conexão com o PC.

Consequência funcional: mouse/teclado do PC controlando o celular e captura alternativa de acessibilidade não estão incluídos neste instalador. O controle do PC pelo celular e o compartilhamento normal da tela do celular permanecem. A interface e a API informam essa limitação por uma capacidade explícita, sem mandar ativar um serviço ausente. O código avançado foi preservado atrás de `JarvisControleAvancado=true`; não foi compilado nem distribuído nesta entrega.

Somente um APK universal é oferecido, incluindo armeabi-v7a para Android 32 bits, além das demais arquiteturas. Retirados o botão ARM64, o pacote ARM64 do manifesto de download, o alvo de cópia e as cópias correntes separadas. A rota ARM64 antiga é rejeitada com HTTP 400. Artefatos históricos versionados permanecem para rastreabilidade.

APK: `artifacts/entrega/Jarvis-2.0.1.apk`, também copiado como `Jarvis.apk` na entrega e no diretório mobile do desktop.
Tamanho: 43.434.400 bytes.
SHA-256: `4BE8558A6F57F159486F15E7024502295AEA4B889D2A1EBE6AC42642BA13EFE5`.
Assinatura preservada para atualização: certificado SHA-256 `d30df2c464b72377f4eddbbac3e692695d67ca55aee150940dc0b16951653957`.
Ícone adaptativo/launcher Jarvis presente (22 recursos de ícone); Release sem depuração e sem AOT; bibliotecas gráficas nativas verificadas em todas as arquiteturas.

## Validação

- Android Release e WPF Release compilados. Android sem erros; WPF sem erros e com avisos anteriores de dependências/API Skia.
- 61 testes aprovados, zero falhas, incluindo personalização, matemática do giro rígido, contraste de todas as paletas, comando de voz local com áudio real sintetizado, segurança das APIs, agenda, aparelho, toque remoto e download universal com retomada.
- Teste de interface usa recursos reais do App.xaml sem iniciar o servidor/mutex de produção. Verifica mudança de cor após atualização de estado e campos reais de configurações numa janela carregada, além de renderização das páginas, arraste e F11.
- TRX final: `tests/Jarvis.Tests/TestResults/revisao-201-final.trx`. Logs: `artifacts/test-201.log`, `artifacts/test-interface-201.log`.
- Prévia de configurações efetivamente carregadas: `artifacts/preview/Configuracoes201-ao-vivo.png` (prata). Outras prévias em `artifacts/preview`.
- Download real de um único pacote: HEAD 200, Range 206 com os 64 bytes solicitados, arquivo baixado com hash/tamanho idênticos ao APK entregue. HTTPS com pinning na LAN e no IPv6 do próprio PC: 200; API sem token: 401; API em HTTP: 403.
- Resultado: `artifacts/validacao-entrega-201.json`; verificador: `artifacts/validar-entrega-201.ps1`.
- Jarvis desktop reaberto e respondendo como 2.0.1. Instalador: http://192.168.0.64:5188/mobile (mesma rede).

## O que ainda depende do celular

Não havia aparelho conectado ao ADB. Portanto, não foi possível confirmar instalação/abertura, microfone, captura nem aparência nativa no Infinix. A retirada da permissão trata uma causa documentada do bloqueio, mas não garante aceitação pelo Play Protect sem o aviso exato e um teste no telefone. Se persistir, registrar o texto do aviso e seguir o fluxo oficial de verificação/contestação, sem desativar a proteção. Internet externa e rede móvel também não foram testadas; as verificações de IPv6 foram feitas no próprio PC.
