using System.Runtime.InteropServices;
namespace Jarvis.Sistema;

public interface IPlanoEnergiaJogo
{
    bool Ativar();
    bool Restaurar();
}
internal sealed class PlanoEnergiaJogo : IPlanoEnergiaJogo
{
    private Guid _anterior, _overlayAnterior;
    private bool _usouOverlay;
    [DllImport("powrprof.dll")] private static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr scheme);
    [DllImport("powrprof.dll")] private static extern uint PowerSetActiveScheme(IntPtr root, ref Guid scheme);
    [DllImport("powrprof.dll")] private static extern uint PowerGetActualOverlayScheme(out Guid scheme);
    [DllImport("powrprof.dll")] private static extern uint PowerSetActiveOverlayScheme(Guid scheme);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
    public bool Ativar()
    {
        if (PowerGetActiveScheme(IntPtr.Zero, out var ptr) != 0) return false;
        try { _anterior = Marshal.PtrToStructure<Guid>(ptr); } finally { LocalFree(ptr); }
        _usouOverlay = false;
        var desempenho = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        if (PowerSetActiveScheme(IntPtr.Zero, ref desempenho) == 0) return true;
        // Modern Standby pode oferecer desempenho como overlay do Equilibrado.
        var overlay = new Guid("ded574b5-45a0-4f42-8737-46345c09c238");
        if (PowerGetActualOverlayScheme(out _overlayAnterior) != 0 || PowerSetActiveOverlayScheme(overlay) != 0) return false;
        _usouOverlay = true; return true;
    }
    public bool Restaurar() => (_usouOverlay ? PowerSetActiveOverlayScheme(_overlayAnterior) : PowerSetActiveScheme(IntPtr.Zero, ref _anterior)) == 0;
}
