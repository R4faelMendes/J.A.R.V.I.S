using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
namespace Jarvis.Desktop.Services;
public sealed class WindowFrameService
{
    private readonly Window _window;
    private bool _full;
    private Rect _restore;
    private WindowState _state;
    private double _minHeight, _minWidth;
    public WindowFrameService(Window window)
    {
        _window = window;
        window.SourceInitialized += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook(Hook);
        window.PreviewKeyDown += (_, e) => { if (e.Key == Key.F11 || (_full && e.Key == Key.Escape)) { Alternar(); e.Handled = true; } };
    }
    public void Alternar()
    {
        if (!_full)
        {
            _state = _window.WindowState; _restore = _window.RestoreBounds; _minWidth = _window.MinWidth; _minHeight = _window.MinHeight;
            _window.MinHeight = 0; _window.MinWidth = 0; _full = true; _window.WindowState = WindowState.Normal; _window.WindowState = WindowState.Maximized;
        }
        else
        {
            _full = false; _window.WindowState = WindowState.Normal; _window.MinWidth = _minWidth; _window.MinHeight = _minHeight;
            if (!_restore.IsEmpty) { _window.Left = _restore.Left; _window.Top = _restore.Top; _window.Width = _restore.Width; _window.Height = _restore.Height; }
            _window.WindowState = _state;
        }
    }
    private nint Hook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != 0x0024) return 0;
        var monitor = MonitorFromWindow(hwnd, 2); var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return 0;
        var limits = Marshal.PtrToStructure<MinMax>(lParam); var bounds = _full ? info.Bounds : info.Work;
        limits.MaxPosition = new(bounds.Left - info.Bounds.Left, bounds.Top - info.Bounds.Top); limits.MaxSize = new(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
        Marshal.StructureToPtr(limits, lParam, false); handled = true; return 0;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Point(int x, int y) { public int X = x, Y = y; }
    [StructLayout(LayoutKind.Sequential)] private struct RectNative { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public RectNative Bounds, Work; public int Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMax { public Point Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
}
