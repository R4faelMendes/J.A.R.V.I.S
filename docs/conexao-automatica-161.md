# Correção 1.6.1 — conexão automática e instalação sem USB

## Diagnóstico observado

- Tailscale do PC em Running, com endereço 100.110.47.61.
- Um celular Android online na mesma rede privada, 100.109.252.22; ping pelo Tailscale respondeu em aproximadamente 65 ms. Isso comprova a rota VPN PC → celular, não a conexão ponta a ponta do aplicativo no aparelho.
- Jarvis do PC estava aberto e /ping local respondeu.
- O cliente 1.6.0 dependia de configurar o endereço remoto manualmente. Endereços privados não eram aprendidos automaticamente ao parear.
- O diagnóstico do Android usava a porta HTTP 5188 para avaliar a conexão VPN, mas somente a porta HTTPS 5189 estava autorizada pela regra de VPN. O aplicativo podia mostrar falha mesmo quando o canal de controle estivesse disponível.

## Mudanças

- O PC anuncia seus endereços locais e Tailscale no pareamento e em /api/identidade. O app salva esses dados de modo privado e verifica alternativas automaticamente ao mudar de rede ou perder conexão.
- Sondagens em até três endereços simultâneos, com cancelamento das restantes quando o mesmo PC responde. HTTPS, token e impressão do certificado continuam obrigatórios. Não há aceite automático de certificado diferente.
- Pareamentos antigos continuam compatíveis: resposta de pareamento mantém valores textuais, com campos adicionais de endereços; clientes antigos podem continuar usando o token.
- Reconexão não apaga o pareamento ao falhar. O diagnóstico distingue servidor sem resposta, token recusado e certificado diferente.
- Troca de endereço só ocorre após resposta autenticada do PC. Tentativa de pareamento que falha não substitui o endereço em uso. Clientes HTTP antigos têm descarte adiado para não interromper comandos em andamento.
- No Android, o Jarvis detecta a VPN e solicita conexão pelo intent oficial `com.tailscale.ipn.CONNECT_VPN`, sem abrir a tela do Tailscale. Login e autorização prévia de VPN continuam sendo exigidos pelo Android. Configuração de VPN sempre ativa disponível pelo próprio menu de diagnóstico do Jarvis.
- No Windows, ativado `tailscale set --unattended=true`; serviço já configurado com inicialização automática. Não é necessário manter a janela Tailscale aberta.
- Corrigida prioridade de IPs: interfaces físicas com gateway precedem interfaces virtuais também quando o PC usa Wi-Fi.
- Interface Dispositivos tem QR de INSTALAÇÃO separado do QR de PAREAMENTO, opção Wi-Fi/Tailscale e cópia de link. O download é direto do PC, sem USB ou hospedagem pública.
- Nova página /mobile com instruções e botão para baixar o APK 1.6.1. Download HTTP da aplicação assinada; controle e dados pessoais continuam exclusivamente HTTPS autenticado.
- Nova regra de firewall de download permite TCP 5188 somente entre endereços da faixa privada Tailscale e para Jarvis.Desktop.exe. Sem encaminhamento de porta no roteador.

## Como usar

1. No celular conectado ao Tailscale, abrir `http://100.110.47.61:5188/mobile`, baixar e instalar a atualização por cima da versão atual.
2. No PC → Dispositivos: QR de instalação serve para baixar; QR de pareamento deve ser lido dentro do Jarvis uma vez para registrar todos os endereços.
3. No Android → Mais → Conexão automática / Tailscale. O app também verifica automaticamente ao abrir e ao mudar de rede.
4. Após login e permissão inicial, não é necessário abrir o Tailscale a cada uso. Se o Android revogar a permissão, forçar parada ou bloquear a VPN pelo modo de bateria, será necessário reautorizar no sistema. O Jarvis não contorna essa restrição.

## Validação

- Compilação Release do desktop e Android concluída sem erros. Android mantém um aviso de API obsoleta (`GetAllNetworks`, utilizada para compatibilidade com aparelhos antigos).
- 18 testes aprovados, zero falhas: reconexão por endpoint alternativo, certificado errado, token recusado, cancelamento, compatibilidade de pareamento, download com suporte a retomada, renderização da interface e regressões de agenda, grade e controle remoto. Resultado em `tests/Jarvis.Tests/TestResults/conexao-161-final.trx`.
- Após simplificar as instruções da tela Dispositivos e retirar o antigo botão de transferência USB, repetidos os 10 testes de conexão e interface: todos aprovados (`conexao-161-interface-final.trx`).
- APK 1.6.1, código de versão 10, 43.293.953 bytes. Verificação do pacote confirmou bibliotecas nativas, ícones de instalação e serviços Android. Assinatura igual à versão anterior, permitindo atualizar por cima.
- SHA-256 do APK: `1C0ADDCFBA161781C709A6AFFA634A86B6B0EF20C490A8CDD9C68E0AF7CD5BCF`.
- APK de entrega: `artifacts/entrega/Jarvis-1.6.1.apk`; cópia servida pelo desktop em `src/Jarvis.Desktop/bin/Release/net8.0-windows10.0.19041.0/mobile/Jarvis.apk`.
- Jarvis reiniciado na versão Release e janela respondendo. `/ping` local e `/mobile` no endereço Tailscale retornaram HTTP 200. Download parcial retornou HTTP 206, nome `Jarvis-1.6.1.apk` e `Cache-Control: no-store`.
- Download completo realizado pelo próprio PC usando `http://100.110.47.61:5188/jarvis.apk`: hash idêntico ao APK de entrega. Isso valida o servidor e o arquivo pelo endereço VPN, sem substituir a verificação a partir do telefone.

Ainda sem aparelho conectado por ADB; solicitação do intent, mudança real Wi-Fi/dados móveis e instalação no telefone precisam de teste no aparelho. Link entregue ao usuário para essa verificação. Não houve confirmação de conexão completa no celular nesta validação.

Referências primárias:
- https://tailscale.com/docs/how-to/run-unattended
- https://github.com/tailscale/tailscale-android/blob/main/android/src/main/AndroidManifest.xml
- https://github.com/tailscale/tailscale-android/blob/main/android/src/main/java/com/tailscale/ipn/IPNReceiver.java
