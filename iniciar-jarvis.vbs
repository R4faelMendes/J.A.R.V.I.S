Set fso = CreateObject("Scripting.FileSystemObject")
Set WshShell = CreateObject("WScript.Shell")
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
WshShell.CurrentDirectory = scriptDir

If Not fso.FileExists(scriptDir & "\dist\index.html") Then
    WshShell.Run "cmd /c npm run build", 0, True
End If

electronExe = scriptDir & "\node_modules\electron\dist\electron.exe"
If fso.FileExists(electronExe) Then
    WshShell.Run """" & electronExe & """ """ & scriptDir & """", 1, False
Else
    WshShell.Run "cmd /c npm run electron", 0, False
End If
