# Jarvis — assistente pessoal para Windows e Android

Projeto pessoal em **C#/.NET 8**, com aplicativo desktop em **WPF** e aplicativo companheiro em **.NET MAUI**. Esta publicação corresponde ao código da versão em desenvolvimento no computador do autor.

O Jarvis recebe comandos por voz para abrir aplicativos, controlar mídia e volume, consultar informações do computador e acionar funções do assistente. A interface também reúne agenda, e-mails, rotinas, telemetria e conexão com o celular. O aplicativo Android se comunica com o PC por um servidor local e pareamento.

## Inteligência artificial e automação

- **Vosk** para detecção da palavra de ativação e **Whisper.net** para transcrição local de voz.
- **Piper** para síntese de voz em português, com modelos instalados separadamente.
- Roteamento local de intenções para comandos conhecidos, com ações implementadas pelo aplicativo.
- O módulo `Jarvis.Ai` contém conectores de **Gemini, Groq e OpenRouter**, armazenamento protegido de chaves e uma cadeia de fallback. Esses conectores são infraestrutura experimental: sua presença no código não significa que estejam ativos no fluxo atual da interface.

Os modelos de voz são de terceiros; não foram treinados neste projeto. O projeto explora a integração desses recursos com uma aplicação desktop e suas funções de automação.

## Organização do código

- `src/Jarvis.Core`: contratos, configurações e regras compartilhadas.
- `src/Jarvis.Ai`: intenções locais, serviços de e-mail e infraestrutura de provedores de IA.
- `src/Jarvis.Voice`: captura, reconhecimento e síntese de voz.
- `src/Jarvis.System`: integração com o Windows, janelas, aplicativos, mídia e telemetria.
- `src/Jarvis.Data`: persistência local e rotinas.
- `src/Jarvis.Info`: clima e notícias.
- `src/Jarvis.Server`: servidor local, comunicação e pareamento.
- `src/Jarvis.Desktop`: interface WPF.
- `src/Jarvis.Mobile`: aplicativo companheiro MAUI.
- `tests/`: testes automatizados de regras, interfaces e integrações.

## Compilar e testar

Requisitos: Windows, SDK do .NET 8 e os componentes de desenvolvimento Windows/MAUI exigidos pelos projetos. Para Android, instale também o workload MAUI Android, Android SDK e JDK compatíveis. `Directory.Build.props` contém as convenções usadas pela instalação local; ajuste os caminhos se necessário.

```powershell
dotnet restore Jarvis.sln
dotnet build Jarvis.sln -c Release
dotnet test Jarvis.sln -c Release
```

Para compilar apenas o desktop:

```powershell
dotnet build src/Jarvis.Desktop/Jarvis.Desktop.csproj -c Release
```

## Modelos e configuração local

O repositório contém o código e os recursos da interface. **Modelos de voz, executáveis externos, builds, caches, bancos pessoais e credenciais não são distribuídos nesta publicação.** A pasta `models/` permanece na instalação local e precisa ser preparada separadamente para executar as funções de voz e os testes de integração que dependem dela.

A estrutura utilizada nesta versão inclui:

- `models/vosk-model-small-pt-0.3/`;
- `models/ggml-tiny.bin` e, opcionalmente, `models/ggml-base.bin`;
- `models/piper/`, com o executável e os arquivos necessários ao Piper;
- `models/piper-voices/`, com o modelo `.onnx` e seu `.onnx.json` correspondente; a configuração padrão usa `pt_BR-faber-medium.onnx` e há suporte a vozes alternativas.

Consulte a implementação de `Jarvis.Voice` e os cartões de modelo em `docs/voices/` antes de preparar os recursos. Cada ferramenta e modelo mantém sua própria licença.

E-mail, provedores externos e pareamento dependem da configuração do usuário. As credenciais são mantidas fora do repositório e protegidas pelos mecanismos locais implementados no aplicativo. Não inclua arquivos de autorização, certificados privados ou dados pessoais em commits.

## Estado do projeto

Projeto experimental de portfólio, com funcionalidades que dependem de Windows, dispositivos de áudio, rede, autorização de contas e modelos locais. A compilação não substitui a validação dessas integrações na máquina de destino. Os documentos em `docs/` registram diferentes etapas do desenvolvimento e podem descrever versões anteriores.

Na validação de publicação de 04/11/2026, a solução compilou em Release sem erros. Dos 195 testes executados, 192 passaram e 3 falharam. Consulte os [resultados e limitações conhecidas](docs/validacao-publicacao-2026-11-04.md).
