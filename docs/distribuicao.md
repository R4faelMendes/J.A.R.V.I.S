# Publicação e downloads

O site público fica em https://R4faelMendes.github.io/J.A.R.V.I.S/ e seu código em `site/`.
O workflow `site.yml` publica a página a cada push em `main`, publicação de release ou execução manual. O histórico é gerado a partir dos cinco commits mais recentes; os downloads usam URLs estáveis da última release. A consulta de versão pela API é opcional: os links funcionam mesmo quando a API limita requisições.

## Windows portátil

```powershell
./scripts/Publicar-Desktop.ps1
```

Gera `Jarvis.exe` na raiz e `artifacts/release/Jarvis-Windows-x64.exe`, com runtime .NET e bibliotecas nativas incluídos. Não requer instalação do .NET no computador de destino. O primeiro início extrai bibliotecas para o diretório temporário do usuário. Modelos de voz continuam separados, preferencialmente em `%LocalAppData%/Jarvis/models`. O executável não tem assinatura Authenticode; o Windows pode exibir um aviso de editor desconhecido.

O executável gerado fica fora do Git. O script e o perfil `Portable.pubxml` são versionados; os binários são distribuídos por Releases, evitando manter builds grandes no histórico do código.

## Android assinado

Defina `AndroidSdkDirectory` e `JavaSdkDirectory` se os caminhos automáticos não se aplicarem. Instale a plataforma Android 34 e build-tools 34.0.0 e o workload `maui-android`.

```powershell
$env:JARVIS_ANDROID_KEYSTORE = 'C:\caminho-privado\jarvis.keystore'
# Carregue JARVIS_ANDROID_PASSWORD por um gerenciador de segredos; nunca a comite.
./scripts/Publicar-Android.ps1
```

A chave deve conter o alias `jarvis`. O script lê a senha pela variável de ambiente, valida o APK com `Test-ApkAndroid.ps1` e gera `artifacts/release/Jarvis-Android.apk`. Preserve a mesma chave para permitir atualizações no Android. Se uma instalação anterior usa outra assinatura, será necessário removê-la antes de instalar este APK, com possível perda dos dados locais.

## Releases automáticas

Configure no GitHub os secrets de Actions `ANDROID_KEYSTORE_BASE64` (conteúdo da chave em base64) e `ANDROID_KEYSTORE_PASSWORD`. A chave não deve entrar no repositório nem nos logs. Uma cópia local de distribuição pode ficar em `%LocalAppData%/Jarvis/distribution`, com a senha protegida por DPAPI; guarde backup privado da chave e da senha.

Depois de testar as alterações e atualizar `ApplicationDisplayVersion`/`ApplicationVersion` no mobile:

```powershell
git tag v2.4.0
git push origin main
git push origin v2.4.0
```

O workflow `release.yml` compila os dois aplicativos, valida o pacote Android e publica os arquivos e `SHA256SUMS.txt`. Também pode ser executado manualmente para uma tag já existente. Os botões do site passam a usar a última release sem edição manual de URLs. Um push de código atualiza o site e o histórico; novos binários só são publicados por uma tag ou execução manual do workflow de release.

Para preparar o site localmente, execute `./scripts/Preparar-Site.ps1` e sirva `artifacts/site` com um servidor HTTP. As fontes são copiadas dos recursos já versionados do aplicativo; as licenças OFL acompanham a publicação.

As duas automações usam GitHub Actions. O Pages deve estar habilitado com a origem **GitHub Actions** nas configurações do repositório.
