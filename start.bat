@echo off
chcp 65001 >nul
REM JARVIS 2.0 — Inicializador Automático (Fase 9 & Tarefa 6).
cd /d "%~dp0"
title JARVIS 2.0

if not exist ".venv\Scripts\python.exe" (
  echo [ERRO] Ambiente virtual .venv nao encontrado.
  echo Execute no PowerShell:
  echo   py -3.12 -m venv .venv
  echo   .\.venv\Scripts\pip.exe install -r requirements.txt
  pause
  exit /b 1
)

REM Executa a verificação e liberação automática de porta, Ollama e modelos
".venv\Scripts\python.exe" "scripts\preflight.py"
if %errorlevel% neq 0 (
  echo [ERRO] Falha na verificacao de pre-voo do JARVIS.
  pause
  exit /b 1
)

REM Variáveis de ambiente para performance máxima local
set OLLAMA_KEEP_ALIVE=-1
set OLLAMA_MAX_LOADED_MODELS=1
set OLLAMA_NUM_PARALLEL=1

REM HTTPS local se o certificado existir (Fase 8: microfone no celular)
set SSL_ARGS=
set PROTO=http
if exist "models\cert.pem" if exist "models\key.pem" (
  set SSL_ARGS=--ssl-keyfile "models\key.pem" --ssl-certfile "models\cert.pem"
  set PROTO=https
)

echo.
echo  =============================================================
echo   J.A.R.V.I.S 2.0 // Servidor Ativo em %PROTO%://localhost:8765
echo  =============================================================
echo   - Rede local: acesse pelo IP do computador com o PIN do config.
echo   - Pressione Ctrl+C para encerrar o servidor.
echo   - O Ollama continua rodando ao sair (seguro); apague logs\ollama_iniciado_pelo_jarvis.flag se quiser rastrear quem o iniciou.
echo.

REM Abre o HUD Holográfico no Microsoft Edge como aplicativo isolado
start "" "msedge" --app=%PROTO%://localhost:8765

REM Inicia o servidor FastAPI/Uvicorn em 0.0.0.0
".venv\Scripts\python.exe" -m uvicorn core.server:app --host 0.0.0.0 --port 8765 %SSL_ARGS%
