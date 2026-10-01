# Instalar o JARVIS (1 minuto)

1. Abra a pasta `install/`.
2. Duplo clique em **`Jarvis-Setup.cmd`**.
3. Ele prepara o .NET 8 (sem pedir administrador), baixa as vozes
   (Piper pt-BR + Vosk pt-BR) e abre o app.

Se algo falhar, o motivo está em `%LOCALAPPDATA%\Jarvis\instalacao.log`.

Opções avançadas (PowerShell):

```powershell
.\install\bootstrap.ps1 -SemModelos   # sem internet / PC fraco (sem voz)
.\install\bootstrap.ps1 -SemApp       # só prepara, não abre
.\install\bootstrap.ps1 -Forcar       # refaz tudo
```
