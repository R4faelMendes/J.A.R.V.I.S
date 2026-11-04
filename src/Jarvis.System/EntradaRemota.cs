using System.Runtime.InteropServices;
using System.Windows.Forms;
using Jarvis.Core;
namespace Jarvis.Sistema;

/// <summary>Gestos atômicos: nenhum botão fica pressionado entre solicitações de rede.</summary>
public sealed class EntradaRemota(ITransmissaoTela tela) : IEntradaRemota
{
    private volatile bool _permitida = true;
    private readonly SemaphoreSlim _gate = new(1, 1);
    public bool Permitida { get => _permitida && tela.Permitida; set => _permitida = value; }
    public async Task ExecutarAsync(PedidoToque p, CancellationToken ct = default)
    {
        p.Validar();
        if (!await _gate.WaitAsync(0, ct)) throw new InvalidOperationException("Outro gesto está em andamento. Aguarde terminar.");
        try { await Task.Run(async () => await AplicarAsync(p, ct), ct); }
        finally { _gate.Release(); }
    }
    private void Verificar()
    {
        if (!Permitida) throw new InvalidOperationException("Controle remoto indisponível no PC. Reinicie o Jarvis e a visualização.");
        if (tela.UltimaVisualizacao is not { } ultima || DateTimeOffset.UtcNow - ultima > TimeSpan.FromSeconds(8)) throw new InvalidOperationException("Inicie a transmissão antes de controlar o PC.");
        var desktop = OpenInputDesktop(0, false, 1);
        if (desktop == 0) throw new InvalidOperationException("Controle pausado na tela de bloqueio ou de segurança.");
        try
        {
            var name = new System.Text.StringBuilder(256);
            if (!GetUserObjectInformation(desktop, 2, name, 512, out _) || !name.ToString().Equals("Default", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Controle pausado na tela de bloqueio ou de segurança.");
        }
        finally { CloseDesktop(desktop); }
    }
    private async Task AplicarAsync(PedidoToque p, CancellationToken ct)
    {
        Verificar(); ct.ThrowIfCancellationRequested();
        RegiaoJanela monitor; RegiaoJanela area;
        var dpi = SetThreadDpiAwarenessContext(-4);
        try
        {
            var screen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == p.MonitorId) ?? throw new ArgumentException("Monitor desconectado. Atualize a transmissão.");
            var b = screen.Bounds; monitor = new(b.X, b.Y, b.Width, b.Height);
            var v = SystemInformation.VirtualScreen; area = new(v.X, v.Y, v.Width, v.Height);
        }
        finally { if (dpi != 0) SetThreadDpiAwarenessContext(dpi); }
        Input Mover(double x, double y)
        {
            var pixel = CoordenadasTela.Pixel(new(x, y), monitor);
            return Mouse(0x8000 | 0x4000 | 0x0001, (int)Math.Round((pixel.X - area.X) * 65535d / Math.Max(1, area.Largura - 1)), (int)Math.Round((pixel.Y - area.Y) * 65535d / Math.Max(1, area.Altura - 1)));
        }
        if (p.Acao == "tecla")
        {
            ushort key = p.Tecla switch { "Enter" => 0x0D, "Escape" => 0x1B, "Tab" => 0x09, "Backspace" => 0x08, "Left" => 0x25, "Up" => 0x26, "Right" => 0x27, "Down" => 0x28, _ => throw new ArgumentException("Tecla inválida.") };
            var released = false;
            try { Enviar(Key(key, false), Key(key, true)); released = true; } finally { if (!released) EnviarSemExcecao(Key(key, true)); } return;
        }
        if (p.Acao == "rolar") { Enviar(Mover(p.X, p.Y), Mouse(0x0800, data: unchecked((uint)(p.Rolagem * 120)))); return; }
        if (p.Acao == "arrastar")
        {
            try
            {
                Enviar(Mover(p.X, p.Y), Mouse(0x0002));
                var steps = Math.Max(2, p.DuracaoMs / 25);
                for (var i = 1; i <= steps; i++)
                {
                    await Task.Delay(p.DuracaoMs / steps, ct); Verificar();
                    Enviar(Mover(p.X + (p.DestinoX - p.X) * i / steps, p.Y + (p.DestinoY - p.Y) * i / steps));
                }
            }
            finally { EnviarSemExcecao(Mouse(0x0004)); }
            return;
        }
        var right = p.Acao == "direito";
        var completed = false;
        try
        {
            Enviar(Mover(p.X, p.Y), Mouse(right ? 0x0008u : 0x0002u), Mouse(right ? 0x0010u : 0x0004u));
            if (p.Acao == "duplo") Enviar(Mouse(0x0002), Mouse(0x0004));
            completed = true;
        }
        finally { if (!completed) EnviarSemExcecao(Mouse(right ? 0x0010u : 0x0004u)); }
    }
    private static Input Mouse(uint flags, int x = 0, int y = 0, uint data = 0) => new() { Type = 0, Data = new() { Mouse = new() { X = x, Y = y, Flags = flags, Wheel = data } } };
    private static Input Key(ushort code, bool up) => new() { Type = 1, Data = new() { Keyboard = new() { VirtualKey = code, Flags = (up ? 2u : 0u) | (code is >= 0x25 and <= 0x28 ? 1u : 0u) } } };
    private static void Enviar(params Input[] inputs)
    {
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
            throw new InvalidOperationException("O Windows bloqueou a entrada. Janelas de administrador e telas protegidas não aceitam este controle.");
    }
    private static void EnviarSemExcecao(params Input[] inputs) => SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputData Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputData { [FieldOffset(0)] public MouseData Mouse; [FieldOffset(0)] public KeyboardData Keyboard; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseData { public int X, Y; public uint Wheel, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardData { public ushort VirtualKey, Scan; public uint Flags, Time; public nuint Extra; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern nint SetThreadDpiAwarenessContext(nint context);
    [DllImport("user32.dll")] private static extern nint OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetUserObjectInformation(nint obj, int index, System.Text.StringBuilder value, int size, out int needed);
    [DllImport("user32.dll")] private static extern bool CloseDesktop(nint desktop);
}
