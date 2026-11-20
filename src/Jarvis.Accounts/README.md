# API de conta Jarvis

O serviço ASP.NET Core roda separadamente do Kestrel local. Ele armazena a lista de PCs por conta Google. O app pode usar o pareamento local quando a API está indisponível. A lista sincronizada contém o identificador e o nome de cada PC. O PIN do certificado, o token de controle, os endereços de rede e o MAC ficam no armazenamento seguro do celular. Cada novo aparelho precisa de autorização por pareamento.

## Publicação

Compile com `dotnet publish src/Jarvis.Accounts -c Release`. Hospede a aplicação atrás de uma origem pública HTTPS com disco persistente.

- `Accounts__Origin` recebe a origem pública HTTPS da API.
- `Accounts__Database` aponta para o arquivo SQLite no disco persistente. O diretório deve existir e permitir gravação ao serviço.
- `Google__ClientId` e `Google__ClientSecret` vêm de um cliente OAuth do tipo Aplicativo da Web no projeto Google Cloud. Guarde o segredo na configuração do serviço.
- Cadastre `https://<domínio-da-api>/signin-google` como URI de redirecionamento do cliente e configure a tela de consentimento para os usuários que entrarão na conta. [Google OAuth para aplicações web](https://developers.google.com/identity/protocols/oauth2/web-server)
- Gere o aplicativo com a propriedade MSBuild `JarvisAccountsOrigin` apontando para a mesma origem. Sem essa propriedade, a tela Meus PCs permite o uso local.

Faça o teste de `/health` e complete um login real antes de publicar o APK com a origem online. No Android existente, mantenha a chave de assinatura de distribuição para que a instalação preserve o armazenamento seguro.
