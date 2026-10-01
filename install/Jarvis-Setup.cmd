@echo off
rem JARVIS — primeira execução neste computador.
rem Instala o que falta (o app roda com o .NET 8 já embutido ou baixado aqui),
rem baixa os modelos de voz e abre o JARVIS.
title JARVIS - preparando
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0bootstrap.ps1" %*
if errorlevel 1 (
  echo.
  echo Nao foi possivel preparar o JARVIS. O log esta em:
  echo   %LOCALAPPDATA%\Jarvis\instalacao.log
  echo.
  pause
)
