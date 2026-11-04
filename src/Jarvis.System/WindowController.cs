using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Jarvis.Core;
namespace Jarvis.Sistema;

public sealed class WindowController : IWindowController
{
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, nint l);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint h);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint h);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint h, StringBuilder s, int n);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(nint h, int cmd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint h, int cmd);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(nint h, nint after, int x, int y, int w, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint h);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint first, uint second, bool attach);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool PostMessage(nint h, uint msg, nint w, nint l);
    [DllImport("user32.dll")] private static extern int GetWindowThreadProcessId(nint h, out int pid);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint h, int index);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint h, out Rect r);
    [DllImport("user32.dll")] private static extern nint SetThreadDpiAwarenessContext(nint context);
    [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] private static extern int Cloaked(nint h, int attribute, out int value, int size);
    [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] private static extern int Frame(nint h, int attribute, out Rect value, int size);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    private delegate bool EnumWindowsProc(nint h, nint l);
    private static readonly object LayoutGate = new();
    public IReadOnlyList<WindowInfo> ListarJanelas()
    {
        var lista = new List<WindowInfo>();
        EnumWindows((h, _) =>
        {
            if (!IsWindowVisible(h) || (GetWindowLong(h, -20) & 0x80) != 0) return true;
            if (Cloaked(h, 14, out var oculto, 4) == 0 && oculto != 0) return true;
            var nome = new StringBuilder(512); if (GetWindowText(h, nome, nome.Capacity) == 0) return true;
            var classe = new StringBuilder(128); GetClassName(h, classe, classe.Capacity);
            if (classe.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Windows.UI.Core.CoreWindow") return true;
            GetWindowThreadProcessId(h, out var pid);
            try
            {
                using var processo = System.Diagnostics.Process.GetProcessById(pid);
                if (processo.ProcessName is "ShellExperienceHost" or "StartMenuExperienceHost" or "SearchHost" or "TextInputHost") return true;
                lista.Add(new(h, nome.ToString(), processo.ProcessName));
            }
            catch { }
            return true;
        }, 0);
        // EnumWindows is in Z order; it is also the intentional default order for voice/automations.
        return lista;
    }
    private void Validar(nint h) { if (!IsWindow(h) || !ListarJanelas().Any(j => j.Handle == h)) throw new ArgumentException("Janela indisponível. Atualize a lista."); }
    public void Minimizar(nint h) { Validar(h); ShowWindowAsync(h, 6); }
    public void Maximizar(nint h) { Validar(h); ShowWindowAsync(h, 3); }
    public void Fechar(nint h) { Validar(h); if (!PostMessage(h, 0x0010, 0, 0)) throw new Win32Exception(Marshal.GetLastWin32Error(), "O Windows não permitiu fechar esta janela."); }
    public void TrazerParaFrente(nint h)
    {
        Validar(h); if (IsIconic(h)) ShowWindowAsync(h, 9);
        if (SetForegroundWindow(h)) return;
        var atual = GetCurrentThreadId(); var frente = (uint)GetWindowThreadProcessId(GetForegroundWindow(), out _);
        var anexou = frente != 0 && frente != atual && AttachThreadInput(atual, frente, true);
        try { if (!SetForegroundWindow(h)) throw new InvalidOperationException("O Windows recusou o foco. Se o aplicativo estiver elevado, abra-o pela barra de tarefas do PC."); }
        finally { if (anexou) AttachThreadInput(atual, frente, false); }
    }
    public void FixarTopo(nint h, bool fixar) { Validar(h); SetWindowPos(h, fixar ? -1 : -2, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010); }
    public IReadOnlyList<MonitorInfo> ListarMonitores()
    {
        var antigo = SetThreadDpiAwarenessContext(-4);
        try { return Screen.AllScreens.Select((s, i) => new MonitorInfo(s.DeviceName, $"Monitor {i + 1}" + (s.Primary ? " · principal" : ""), s.WorkingArea.X, s.WorkingArea.Y, s.WorkingArea.Width, s.WorkingArea.Height, s.Primary)).ToArray(); }
        finally { if (antigo != 0) SetThreadDpiAwarenessContext(antigo); }
    }
    public void AplicarLayout(string modelo) => Organizar(new(modelo));
    public ResultadoLayout Organizar(PedidoLayout pedido)
    {
        lock (LayoutGate)
        {
            var capacidade = GradeJanelas.Capacidade(pedido.Modelo);
            var todos = ListarJanelas();
            // Jarvis pode ser posicionado explicitamente sem entrar nos layouts automáticos por voz.
            var ids = pedido.Janelas ?? todos.Where(j => j.App != "Jarvis.Desktop" && !IsIconic(j.Handle)).Take(capacidade).Select(j => (long)j.Handle).ToArray();
            if (ids.Length == 0 || ids.Length > capacidade || ids.Distinct().Count() != ids.Length) throw new ArgumentException($"Selecione de 1 a {capacidade} janelas diferentes.");
            if (ids.Any(id => !todos.Any(j => (long)j.Handle == id))) throw new ArgumentException("Uma janela foi fechada. Atualize a lista antes de organizar.");
            var monitores = ListarMonitores();
            var monitor = pedido.MonitorId is null ? monitores.FirstOrDefault(m => m.Principal) : monitores.FirstOrDefault(m => m.Id == pedido.MonitorId);
            if (monitor is null) throw new ArgumentException("Monitor indisponível. Atualize a lista.");
            var retangulos = PosicoesGrade.Calcular(pedido.Modelo, new(monitor.X, monitor.Y, monitor.Largura, monitor.Altura), ids.Length, pedido.Posicoes);
            var avisos = new List<string>(); var movidas = 0;
            var antigo = SetThreadDpiAwarenessContext(-4);
            try
            {
                for (var i = 0; i < ids.Length; i++)
                {
                    var h = (nint)ids[i]; var alvo = retangulos[i];
                    ShowWindow(h, 9);
                    var dx = 0; var dy = 0; var dw = 0; var dh = 0;
                    if (GetWindowRect(h, out var externo) && Frame(h, 9, out var visivel, 16) == 0)
                    { dx = externo.Left - visivel.Left; dy = externo.Top - visivel.Top; dw = externo.Right - externo.Left - (visivel.Right - visivel.Left); dh = externo.Bottom - externo.Top - (visivel.Bottom - visivel.Top); }
                    if (!SetWindowPos(h, 0, alvo.X + dx, alvo.Y + dy, alvo.Largura + dw, alvo.Altura + dh, 0x0004 | 0x0010 | 0x0040 | 0x4000))
                    { avisos.Add($"{todos.First(j => (long)j.Handle == ids[i]).Titulo}: o Windows recusou a posição."); continue; }
                    movidas++;
                    if (Frame(h, 9, out var final, 16) == 0 && (Math.Abs(final.Right - final.Left - alvo.Largura) > 24 || Math.Abs(final.Bottom - final.Top - alvo.Altura) > 24)) avisos.Add($"{todos.First(j => (long)j.Handle == ids[i]).Titulo}: o aplicativo impõe um tamanho mínimo.");
                }
            }
            finally { if (antigo != 0) SetThreadDpiAwarenessContext(antigo); }
            return new(movidas, avisos);
        }
    }
}
