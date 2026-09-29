@echo off
REM JARVIS 2.0 — inicializador (Fase 9). Ativa venv, garante Ollama, inicia servidor e abre o HUD.
cd /d "%~dp0"
title JARVIS 2.0
if not exist ".venv\Scripts\python.exe" (
  echo [ERRO] .venv nao encontrada. Rode antes:
  echo   py -3.12 -m venv .venv ^&^& .\.venv\Scripts\pip.exe install -r requirements.txt
  pause
  exit /b 1
)
REM Ollama: inicia em segundo plano se ainda nao estiver ouvindo.
powershell -NoProfile -Command "try { (Invoke-WebRequest http://localhost:11434/api/tags -UseBasicParsing -TimeoutSec 3) ^| Out-Null } catch { Start-Process -FilePath \"$env:LOCALAPPDATA\Programs\Ollama\ollama.exe\" -ArgumentList 'serve' -WindowStyle Hidden }"
set OLLAMA_NUM_PARALLEL=1
REM HTTPS local se o certificado existir (Fase 8: celular precisa de HTTPS p/ microfone).
set SSL_ARGS=
set PROTO=http
if exist "models\cert.pem" if exist "models\key.pem" set SSL_ARGS=--ssl-keyfile models\key.pem --ssl-certfile models\cert.pem
if exist "models\cert.pem" if exist "models\key.pem" set PROTO=https
echo.
echo  JARVIS 2.0 iniciando em %PROTO%://localhost:8765
echo  Na primeira vez o Firewall pergunta: permita SO em "Redes privadas".
if "%PROTO%"=="https" echo  Certificado local: aceite o aviso uma vez (Avancado - Continuar).
echo.
REM Abre o HUD como app sem bordas (Edge ja existe no Windows 10).
start "" "msedge" --app=%PROTO%://localhost:8765
REM Servidor em 0.0.0.0 (rede local, PIN protege). Ctrl+C encerra.
".venv\Scripts\python.exe" -m uvicorn core.server:app --host 0.0.0.0 --port 8765 %SSL_ARGS%
