# Validação da atualização — 12/11/2026

## Alterações

- Verde esmeralda mais saturado (`#35F29A`), fundos escuros e linhas mais definidas na paleta compartilhada. O reator realça brilho/saturação na energia verde e preserva as regiões metálicas.
- Proteção contra carregar Vosk sem os arquivos mínimos de modelo: evita a falha nativa observada na primeira execução da suíte. Um teste verifica pastas ausentes e incompletas antes da DLL.
- SDK Windows de referência fixado em `10.0.19041.56`, necessário para CommunityToolkit.Mvvm 8.4.2 com o SDK .NET 8.0.404 desta máquina.
- Distribuição Windows portátil, APK Android assinado, site GitHub Pages e workflows de publicação.

## Builds e pacotes

O desktop compilou em Release com **0 erros e 66 avisos**. Persistem avisos de versões/compatibilidade de dependências, APIs gráficas obsoletas e plataforma. NAudio 2.1.4 não foi encontrado e o NuGet resolveu 2.2.0; essa dependência não foi alterada nesta atualização.

`Publicar-Desktop.ps1` gerou `Jarvis.exe` na raiz e `Jarvis-Windows-x64.exe` para a release, aproximadamente **312 MB**, com runtime .NET, bibliotecas nativas e recursos embarcados. O tamanho inclui as dependências; os modelos de voz não são distribuídos.

O teste de abertura inicial com espera fixa de 15 segundos não encontrou a janela a tempo. Uma observação com limite maior encontrou a janela em aproximadamente 25 segundos após a extração inicial. O teste foi ajustado para aguardar a janela com limite explícito e verificar os serviços/encerramento. **A execução seguinte passou, com janela em 3,6 segundos e encerramento sem erro.** Esses tempos são desta máquina e não garantem desempenho em outros computadores.

`Publicar-Android.ps1` gerou **Jarvis 2.3.1 (versionCode 21)**, aproximadamente **60 MB**. `Test-ApkAndroid.ps1` aprovou o pacote, incluindo bibliotecas Skia nas quatro arquiteturas (`armeabi-v7a`, `arm64-v8a`, `x86`, `x86_64`) e 22 recursos de ícone. `apksigner verify --print-certs` confirmou a assinatura. AOT continua desabilitado. Não houve instalação/teste em um aparelho Android físico nesta atualização.

## Testes automatizados

Após a proteção de carregamento do Vosk, a suíte Windows concluiu sem o encerramento nativo que abortou a primeira tentativa:

| Projeto | Total | Aprovados | Falhas |
| --- | ---: | ---: | ---: |
| Jarvis.Tests | 192 | 184 | 8 |
| Jarvis.Mobile.Tests | 4 | 2 | 2 |
| Total | 196 | 186 | 10 |

Falhas observadas em `Jarvis.Tests`:

1. `FluidezMemoriaAppsTest.TransmissaoValidaLimitesENaoCapturaSemPermissao`: esperava InvalidOperationException e nenhuma exceção foi lançada.
2. `EscutaNovaTest.WhisperRealEntendePcm16SemCabecalhoWav`: resultado nulo.
3. `RuntimeIntegrationTest.PiperRealESessaoSpotify`: NullReferenceException.
4. `EscutaCompletaV4Test.AudioPassaPorVoskSegmentacaoWhisperEAberturaLocal`: modelos Piper ausentes.
5. `Revisao230Test.MonitorUsaSomenteSensoresReaisComFallbackParaValoresAusentes`: timeout.
6. `Revisao200Test.VozLocalRealTranscreveAudioPiperSemChaveDeApi`: NullReferenceException.
7. `VozBootTest.Iniciar_CriaLogSemErro`: NullReferenceException.
8. `InterfaceSmokeTest.Paginas_Principais_CriamLayoutSemRecursaoOuFalhaDeRecursos`: barra de voz não coube na condição F11 do teste.

Falhas em `Jarvis.Mobile.Tests`:

1. `ReatorMarkIvUsaArteRealEAnimaComSkiaDoMobile`: recurso gráfico não disponível no assembly de testes; esse projeto ainda embute a arte PNG antiga, enquanto o aplicativo e o renderizador usam o ZIP frontal v3.
2. `TemaAtualizaCartaoBotaoEGradienteSemRecriarAzul`: UnauthorizedAccessException ao substituir o arquivo local de tema nesta execução.

As falhas permanecem registradas; não há declaração de suíte integralmente aprovada. Integrações de voz dependem de modelos que não estavam preparados neste checkout. Os relatórios TRX ficam em `artifacts/`, fora do Git.

## Site

O workflow GitHub Pages foi executado com sucesso, e o endereço público respondeu HTTP 200. A página foi inspecionada no Edge headless em **1440 × 1000** e **390 × 844**:

- Sem overflow horizontal nas duas larguras.
- Sem erros de JavaScript.
- Histórico renderizado com cinco commits e dois links de download.
- Fontes locais com licenças OFL e preferência por movimento reduzido respeitada.

O histórico é atualizado no deploy de cada push em `main`. Os botões apontam à última release publicada; novos binários dependem de tag ou execução manual do workflow de aplicativos. As credenciais de assinatura Android ficam nos secrets do GitHub e em armazenamento privado local, fora do repositório.
