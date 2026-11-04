# Correção de abertura — Android 1.4.1 (código 7)

## Falha encontrada no pacote 1.4

A inspeção do APK anterior confirmou ausência de `libSkiaSharp.so` em Android. A tela inicial cria um canvas Skia para o reator; sem essa biblioteca, sua inicialização pode derrubar o aplicativo.

O grafo restaurado misturava `SkiaSharp.Views.Maui.Controls/2.88.9` com `SkiaSharp/4.151.1`, introduzido por `ZXing.Net.Bindings.SkiaSharp/0.16.24`. Não havia `SkiaSharp.NativeAssets.Android` no grafo. Compilar com êxito não detectou esse problema de distribuição.

## Correção

- Mobile fixa SkiaSharp e native assets Android em 2.88.9, compatível com os controles MAUI usados pelo projeto.
- Removido o adaptador ZXing que puxava SkiaSharp 4. O leitor QR usa ZXing.Net puro com conversão explícita para pixels BGRA; o mesmo arquivo do adaptador é compilado nos testes.
- O pareamento inicial aguarda `MainPage.Loaded`, e a navegação por deep link tem tratamento de exceção. Uma falha de restauração da conexão não deve fechar a interface.
- AOT permanece desabilitado no Release, conforme a decisão existente do projeto.
- O ícone Jarvis pertence aos recursos `mipmap` do aplicativo, incluindo ícone redondo/adaptativo, e é configurado no manifesto de instalação. Nenhuma imagem precisa ser instalada separadamente.

## Entrega e teste

Instale `artifacts/entrega/Jarvis-1.4.1.apk` sobre a versão anterior. O pacote conserva o identificador `com.companyname.jarvis.mobile` e a assinatura é conferida antes da entrega. O nome exibido continua Jarvis.

`scripts/Test-ApkAndroid.ps1` rejeita pacotes sem `libSkiaSharp.so` para qualquer ABI incluída, sem os ícones esperados ou com versões gráficas incompatíveis. O empacotador chama essa checagem antes de copiar a entrega.

A checagem reproduziu a falha no APK antigo e aprovou o pacote recompilado para arm64-v8a, armeabi-v7a, x86 e x86_64, com 22 recursos de ícone. A validação estrutural do APK não substitui teste de abertura no Android.

Não havia telefone conectado ao ADB nem emulador instalado. A abertura no aparelho do usuário ainda precisa ser conferida. Não se afirma teste físico apenas pelo sucesso da compilação.

Logs: `artifacts/mobile-fix-restore.log`, `artifacts/mobile-fix-final-build.log`, `artifacts/mobile-fix-tests.log`, `artifacts/mobile-fix-package.json`. A assinatura anterior/nova é comparada em arquivos de diagnóstico na mesma pasta.

## Resultado final

- Android Release 1.4.1: zero erros e zero avisos.
- Pacote final aprovado em `artifacts/mobile-fix-package-final.json`: quatro ABIs com Skia nativo e 22 recursos de ícone.
- Assinatura igual à instalação anterior e cópia do instalador no desktop idêntica: `artifacts/mobile-fix-entrega.json`.
- 107 testes aprovados na execução final isolada, incluindo o decodificador QR compartilhado. Foram observadas falhas intermitentes de áudio/janelas em execuções amplas anteriores; os testes de integração com estado global passaram a executar sem paralelismo. Log final: `artifacts/mobile-fix-tests-amplo-detalhes.log`; resultado TRX em `tests/Jarvis.Tests/TestResults/mobile-fix-amplo.trx`.
- Ícone extraído do APK real e inspecionado: `docs/design/Jarvis-icone-apk-141.png`.
- APK: 43.199.237 bytes, SHA256 `967A2D8B8728E8EF5B60AF5A5C02CC2ADC6E4A2099F5DCBFCD4B341813D83645`.

O APK versionado é `artifacts/entrega/Jarvis-1.4.1.apk`. O caminho padrão `Jarvis.apk` e a pasta mobile do desktop também foram atualizados. O ícone já faz parte de todos esses arquivos de instalação.
