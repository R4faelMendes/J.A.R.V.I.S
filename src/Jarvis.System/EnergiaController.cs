using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace Jarvis.Sistema;
public static class EnergiaController
{
    [DllImport("user32.dll", SetLastError = true)] private static extern bool LockWorkStation();
    [DllImport("powrprof.dll", SetLastError = true)] private static extern bool SetSuspendState(bool hibernate, bool force, bool disableWake);
    public static void Executar(string comando)
    {
        switch (comando)
        {
            case "bloquear": if (!LockWorkStation()) throw new Win32Exception(); break;
            case "suspender": if (!SetSuspendState(false, false, false)) throw new Win32Exception(); break;
            case "reiniciar": Process.Start(new ProcessStartInfo("shutdown.exe", "/r /t 30") { UseShellExecute = false, CreateNoWindow = true }); break;
            default: throw new ArgumentException("Ação de energia inválida.");
        }
    }
}
