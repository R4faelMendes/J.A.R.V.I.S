# J.A.R.V.I.S. — App companheiro mobile (Fase 2, item 6 — pendente de pareamento)

Status: **protocolo definido, app ainda não implementado**.

## Protocolo proposto (WebSocket local)

- Servidor: J.A.R.V.I.S. desktop escuta em `ws://<pc>:17874/jarvis`.
- Autenticação: token de pareamento exibido no desktop (QR), guardado nas
  preferências (`companionToken`).
- Mensagens desktop → mobile:
  - `{ "type": "notify", "text": "..." }` — reflexos, vigília, automações.
  - `{ "type": "approval", "id": "...", "label": "Encerrar Code.exe?" }` —
    automação sensível aguardando aprovação remota.
- Mensagens mobile → desktop:
  - `{ "type": "approve", "id": "..." }` / `{ "type": "reject", "id": "..." }`
  - `{ "type": "command", "text": "abrir agenda" }` — executa o pipeline central.

## O que falta (configuração externa / trabalho futuro)

1. Servidor WS no processo Electron (dependência `ws` + canal IPC).
2. App Expo minimalista (2 telas: notificações + aprovar/rejeitar).
3. Pareamento por QR + túnel opcional para uso fora da rede local.
4. Testes de reconexão e fila offline.

Enquanto isso, aprovações remotas continuam sendo feitas no próprio painel
(trava biométrica em `biometricService.js`).
