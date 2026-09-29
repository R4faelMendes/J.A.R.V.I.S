' JARVIS 2.0 — início silencioso (Fase 9): roda o start.bat sem janela de terminal.
Set sh = CreateObject("Wscript.Shell")
sh.Run """" & CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName) & "\start.bat""", 0, False
