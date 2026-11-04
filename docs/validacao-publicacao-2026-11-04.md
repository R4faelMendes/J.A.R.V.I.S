# Validação da versão publicada — 04/11/2026

Esta publicação apresenta o código da versão local em desenvolvimento. Não declara que todas as integrações estejam prontas ou que todos os testes estejam aprovados.

## Compilação

```powershell
dotnet build Jarvis.sln -c Release --no-restore --nologo
```

Resultado: **0 erros e 71 avisos**. Os avisos incluem compatibilidade/versões de dependências, chamadas específicas de Windows, APIs gráficas obsoletas e avisos de código.

## Testes

```powershell
dotnet test Jarvis.sln -c Release --no-build --no-restore --nologo --logger trx --results-directory artifacts/testes-publicacao
```

- `Jarvis.Tests`: 191 testes; 189 aprovados e 2 com falha.
- `Jarvis.Mobile.Tests`: 4 testes; 3 aprovados e 1 com falha.
- Total: **195 testes; 192 aprovados e 3 com falha**.

Falhas encontradas nesta execução:

1. `Jarvis.Mobile.Tests.InicializacaoTest.ReatorMarkIvUsaArteRealEAnimaComSkiaDoMobile`: o recurso de arte não foi considerado disponível no contexto do teste.
2. `Jarvis.Tests.FluidezMemoriaAppsTest.TransmissaoValidaLimitesENaoCapturaSemPermissao`: era esperada uma `InvalidOperationException`, mas a chamada não lançou a exceção. A validação da permissão/estado de transmissão precisa ser investigada.
3. `Jarvis.Tests.EscutaCompletaV4Test.AudioPassaPorVoskSegmentacaoWhisperEAberturaLocal`: a transcrição retornou “Chervis, abra a calculadora, por favor.” e não satisfez a asserção do teste de voz.

As falhas foram registradas sem modificar a implementação para esta publicação. Os relatórios TRX e demais arquivos de execução ficam em `artifacts/`, fora do Git. Os testes de voz e outras integrações dependem dos modelos e recursos disponíveis na máquina.
