using System.Runtime.InteropServices;
namespace Jarvis.Sistema;
public sealed record LeituraMemoria(ulong TotalBytes, ulong UsadaBytes)
{ public double Percentual => TotalBytes == 0 ? double.NaN : UsadaBytes * 100d / TotalBytes; }
public static class MemoriaWindows
{
    [StructLayout(LayoutKind.Sequential)] private struct MemoryStatus
    {
        public uint Length, Load;
        public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtendedVirtual;
    }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    public static int TamanhoEstrutura => Marshal.SizeOf<MemoryStatus>();
    public static LeituraMemoria? Ler()
    {
        var s = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        return GlobalMemoryStatusEx(ref s) && s.TotalPhys > 0 ? new(s.TotalPhys, s.TotalPhys - Math.Min(s.AvailPhys, s.TotalPhys)) : null;
    }
}
