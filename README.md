<div align="center">

# J.A.R.V.I.S

**Seu computador. Sua voz.**

Assistente pessoal para Windows, com reconhecimento de voz local,<br>automação e um aplicativo companheiro para Android.

[![Site](https://img.shields.io/badge/Site-00D800?style=for-the-badge&logo=githubpages&logoColor=081B15)](https://R4faelMendes.github.io/J.A.R.V.I.S/)
[![Downloads](https://img.shields.io/badge/Downloads-102E25?style=for-the-badge&logo=github&logoColor=00D800)](https://github.com/R4faelMendes/J.A.R.V.I.S/releases/latest)
[![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Site](https://github.com/R4faelMendes/J.A.R.V.I.S/actions/workflows/site.yml/badge.svg)](https://github.com/R4faelMendes/J.A.R.V.I.S/actions/workflows/site.yml)

[Recursos](#o-que-o-jarvis-reúne) · [Instalar](#primeiros-passos) · [Desenvolver](#compilar-e-testar) · [Distribuição](docs/distribuicao.md)

</div>

---

## Baixar

**[Acesse o site oficial e os downloads →](https://R4faelMendes.github.io/J.A.R.V.I.S/)**

| Plataforma | Download | Requisitos |
| --- | --- | --- |
| Windows | [Jarvis-Windows-x64.exe](https://github.com/R4faelMendes/J.A.R.V.I.S/releases/latest/download/Jarvis-Windows-x64.exe) | Windows 10 build 19041 ou posterior, 64 bits |
| Android | [Jarvis-Android.apk](https://github.com/R4faelMendes/J.A.R.V.I.S/releases/latest/download/Jarvis-Android.apk) | Android 5.0 ou posterior; Jarvis no PC para funções remotas |

Os links acompanham a última release publicada. Consulte as [notas e verificações SHA-256](https://github.com/R4faelMendes/J.A.R.V.I.S/releases) antes de instalar.

**Versão experimental de desenvolvimento.** O Windows recebe um executável portátil com runtime .NET incluído; os modelos de voz são preparados separadamente. O Android é distribuído como APK fora da Play Store. O site apresenta o projeto e os downloads dos aplicativos nativos; não há versão do assistente executável no navegador.

## O que o Jarvis reúne

- **Voz em português:** Vosk para ativação, Whisper.net para transcrição e Piper para síntese local. Ativação também por `Ctrl + Espaço`.
- **Primeiro uso guiado:** instalação e reparo de Vosk, Whisper, Piper e voz em português, com progresso, cancelamento e testes locais.
- **Comandos digitados:** histórico local de voz e texto, edição da transcrição e repetição de comandos.
- **Rotinas:** editor com prévia das ações, perfis iniciais para trabalho/estudo/programação/reunião, ativação por voz e horário semanal enquanto o Jarvis estiver aberto.
- **Resumo do dia:** compromissos locais pendentes e assuntos de e-mails não lidos, com fonte e horário.
- **Atualização no app:** notas da release, download oficial com SHA-256 e instalação/reinício após confirmação.
- **Modo discreto em jogos:** economia, animações reduzidas e silêncio, com restauração dos ajustes anteriores ao sair.
- **Controle do Windows:** aplicativos, janelas e layouts, mídia, volume e bloqueio, com confirmações previstas pelo projeto.
- **Central pessoal:** conversa, aplicativos, desempenho, agenda, e-mails, rotinas, celular, segurança e configurações.
- **Telemetria:** informações do sistema e processos, com sensores dependentes de hardware e permissões.
- **PC + Android:** servidor local e SignalR, pareamento por código/QR e validação do certificado no companheiro.
- **Interface própria:** WPF com MVVM, reator SkiaSharp com fallback WPF, fontes locais e tema único preto e branco com verde `#00D800`, sem seletor de paletas.

E-mails e agenda dependem da conexão da conta; ficam vazios quando desconectados. Clima e notícias usam serviços externos. As integrações dependem de autorizações, rede e recursos da máquina.

### Inteligência artificial

Os modelos de voz são de terceiros, não treinados neste projeto. Ações conhecidas passam por um roteador local de intenções.

A conversa com Gemini, Groq e OpenRouter é **opcional e começa desativada**. Ative-a em Configurações → Conversa com IA e forneça sua própria chave, protegida por DPAPI. Há fallback entre provedores configurados e um limite local de **20 tentativas por dia**, contando falhas. A resposta identifica o provedor; o texto da IA nunca é executado como comando. E-mails, agenda e histórico não são enviados automaticamente. Serviços podem cobrar conforme sua conta.

## Primeiros passos

1. Baixe o executável Windows, coloque-o em uma pasta de sua escolha e abra o Jarvis. Ele não possui assinatura Authenticode; o Windows pode identificar o editor como desconhecido.
2. Abra **Configurações → Preparar / reparar voz** e clique em **Instalar / reparar recursos**. O assistente baixa aproximadamente 200 MB das fontes oficiais, valida os arquivos e permite testar síntese, transcrição e microfone. Reabra o Jarvis após a primeira instalação para carregar a escuta. Você também pode preparar manualmente os arquivos abaixo em `%LocalAppData%/Jarvis/models`; o checkout aceita `models/` na raiz.
3. Configure o microfone e as integrações nas páginas do aplicativo. Conecte uma conta para usar e-mails/agenda; provedores externos exigem credenciais próprias.
4. Instale o APK Android, permita a instalação pela origem utilizada e faça o pareamento com o Jarvis aberto no PC. Comece com os dois dispositivos na mesma rede local.

```text
models/
├── vosk-model-small-pt-0.3/       # extraído: final.mdl, mfcc.conf, Gr.fst, HCLr.fst, ivector/
├── ggml-tiny.bin                 # Whisper; ggml-base.bin é opcional
├── piper/
│   └── piper.exe                 # junto das bibliotecas do Piper
└── piper-voices/
    ├── pt_BR-faber-medium.onnx
    └── pt_BR-faber-medium.onnx.json
```

Consulte `Jarvis.Voice`, os [modelos Vosk](https://alphacephei.com/vosk/models) e os cartões em [docs/voices](docs/voices/). Cada ferramenta/modelo mantém sua licença. O pacote não inclui modelos, dados pessoais, bancos, tokens de contas ou certificados privados. Se um APK anterior usa outra assinatura, será necessário removê-lo antes de instalar esta distribuição.

## Usar os novos recursos

- **Comandos:** digite `que horas são`, corrija uma transcrição em Editar ou repita uma ação pelo histórico. Enter envia; Shift + Enter cria uma linha.
- **Rotinas:** escolha um perfil, ajuste as etapas e salve. Diga `executar rotina Estudar`, usando o nome salvo. A prévia lista todas as ações. Horários usam o relógio local; o PC precisa estar ligado e o Jarvis aberto. Horários perdidos não são recuperados; cada horário roda no máximo uma vez por dia. Ações que exigem confirmação devem ser executadas individualmente.
- **Resumo:** use o acesso rápido na página inicial ou diga `resumo do dia`. A agenda é a agenda local do Jarvis; não implica sincronização com o Google Calendar.
- **Atualizações:** em Configurações → Atualizações do Jarvis, consulte as notas e baixe a versão. Instalar e reiniciar pede confirmação e mantém uma cópia do executável anterior. O atualizador funciona no executável portátil publicado; em pastas protegidas, mova o aplicativo para uma pasta gravável.
- **Privacidade:** os últimos 200 comandos ficam em SQLite neste computador. Limpe o histórico pela central de comandos. A IA precisa de consentimento explícito.

Veja o [registro das melhorias implementadas e seus limites](docs/ideias-e-simplificacoes.md).

## Arquitetura

Desktop em **.NET 8 + WPF + CommunityToolkit.Mvvm**; companheiro em **.NET MAUI**. Contratos compartilhados no Core, SQLite para persistência e DPAPI para segredos no Windows.

| Projeto | Responsabilidade |
| --- | --- |
| `Jarvis.Core` | Contratos, configurações e tokens do tema único |
| `Jarvis.Desktop` | Interface WPF, composição, bandeja e overlay |
| `Jarvis.Mobile` | Companheiro MAUI, pareamento e conexão |
| `Jarvis.Voice` | Captura, ativação, reconhecimento e síntese |
| `Jarvis.System` | Windows, aplicativos, janelas, mídia e telemetria; namespace `Jarvis.Sistema` |
| `Jarvis.Ai` | Intenções locais, serviços de e-mail e conectores de IA |
| `Jarvis.Server` | Servidor embutido, comunicação e pareamento |
| `Jarvis.Data` | SQLite, rotinas e persistência |
| `Jarvis.Info` | Clima e notícias |
| `Jarvis.Rendering.Shared` | Renderização compartilhada do reator |
| `tests/` | Testes de regras, interface e integrações |

## Compilar e testar

Requisitos: Windows, SDK **.NET 8 atualizado** e Git. Para Android: workload `maui-android`, JDK 17 e Android SDK com plataforma 34 e build-tools 34.0.0. Para todos os alvos Windows do MAUI, instale também `maui-windows`.

```powershell
git clone https://github.com/R4faelMendes/J.A.R.V.I.S.git
cd J.A.R.V.I.S

# Desktop
dotnet build src/Jarvis.Desktop/Jarvis.Desktop.csproj -c Release

# Testes
dotnet test tests/Jarvis.Tests/Jarvis.Tests.csproj -c Release
dotnet test tests/Jarvis.Mobile.Tests/Jarvis.Mobile.Tests.csproj -c Release

# Solução completa (exige todos os workloads dos alvos MAUI)
dotnet build Jarvis.sln -c Release
dotnet test Jarvis.sln -c Release
```

`Directory.Build.props` tenta detectar os caminhos da instalação original e fixa uma revisão do SDK Windows compatível com o MVVM Toolkit. Em outra máquina, configure os caminhos locais:

```powershell
$env:AndroidSdkDirectory = 'C:\Android\Sdk'
$env:JavaSdkDirectory = 'C:\Program Files\Microsoft' # contém bin\java.exe
dotnet build src/Jarvis.Mobile/Jarvis.Mobile.csproj -f net8.0-android -c Release
```

Testes de voz e integrações dependem de modelos, áudio, permissões e serviços da máquina. Uma compilação bem-sucedida não garante essas integrações.

### Executável na raiz

```powershell
./scripts/Publicar-Desktop.ps1
```

Gera **`Jarvis.exe` na raiz** e uma cópia em `artifacts/release/`, com runtime e bibliotecas nativas incluídos. O binário gerado fica fora do Git; o script e o perfil de publicação são versionados. Os downloads ficam em [Releases](https://github.com/R4faelMendes/J.A.R.V.I.S/releases).

## Site e atualizações

O site fica no **GitHub Pages**, com código em `site/`. A cada push em `main`, o workflow publica a página e atualiza o histórico. Os botões usam URLs estáveis da última release.

Novos aplicativos são compilados e publicados quando uma tag `v*` é enviada. A assinatura Android usa secrets de Actions e mantém a mesma chave entre versões. Veja o [guia de distribuição](docs/distribuicao.md) para scripts, assinatura e publicação.

## Validação e limitações

Projeto em desenvolvimento. Consulte a [validação desta atualização](docs/validacao-publicacao-2026-11-12.md) e o [registro anterior](docs/validacao-publicacao-2026-11-04.md) para resultados e falhas conhecidas. Documentos em `docs/` registram etapas antigas e podem descrever comportamentos anteriores.

Não comite credenciais, certificados, chaves de assinatura ou bancos pessoais. Modelos e segredos locais ficam fora do repositório; mantenha backup privado da assinatura Android.

## Próximas melhorias

Veja a [revisão de ideias e simplificações](docs/ideias-e-simplificacoes.md), baseada nos fluxos implementados: primeiro uso da voz, comandos digitados, acesso às rotinas e remoção de ações apenas demonstrativas.

## Contribuir e relatar problemas

Leia [AGENTS.md](AGENTS.md) antes de alterar a arquitetura. [Abra uma issue](https://github.com/R4faelMendes/J.A.R.V.I.S/issues) com plataforma, versão, passos para reproduzir e logs sem dados pessoais. Em alterações de interface, inclua uma captura e registre a validação.
