using System.Drawing;
using System.Drawing.Imaging;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Jarvis.Sistema;

/// <summary>Desktop Duplication: lê a superfície da GPU sem o bloqueio de CopyFromScreen/GDI.</summary>
internal sealed class CapturaGpu : IDisposable
{
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private readonly IDXGIOutputDuplication _duplication;
    private ID3D11Texture2D? _staging;
    private bool _hasFrame;
    private CapturaGpu(IDXGIAdapter1 adapter, IDXGIOutput1 output)
    {
        D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport, null, out _device, out _context).CheckError();
        try { _duplication = output.DuplicateOutput(_device); }
        catch { _context.Dispose(); _device.Dispose(); throw; }
    }
    public static CapturaGpu? Criar(string monitor, Rectangle area, out string motor)
    {
        motor = "GDI · saída DXGI indisponível";
        try
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            for (uint a = 0; factory.EnumAdapters1(a, out var adapter).Success; a++)
            using (adapter)
            {
                for (uint o = 0; adapter.EnumOutputs(o, out var output).Success; o++)
                using (output)
                {
                    var description = output.Description;
                    var rect = description.DesktopCoordinates;
                    var sameBounds = rect.Left == area.Left && rect.Top == area.Top && rect.Right == area.Right && rect.Bottom == area.Bottom;
                    if ((!description.DeviceName.TrimEnd('\0').Equals(monitor, StringComparison.OrdinalIgnoreCase) && !sameBounds) || description.Rotation is not (ModeRotation.Identity or ModeRotation.Unspecified)) continue;
                    using var output1 = output.QueryInterface<IDXGIOutput1>();
                    var capture = new CapturaGpu(adapter, output1); motor = "Direct3D Desktop Duplication"; return capture;
                }
            }
        }
        catch (Exception ex) { motor = $"GDI · Direct3D indisponível ({ex.HResult:X8})"; System.Diagnostics.Trace.WriteLine("Captura GPU indisponível: " + ex.Message); }
        return null;
    }
    public bool Desenhar(Graphics target, int width, int height)
    {
        var result = _duplication.AcquireNextFrame(0, out _, out var resource);
        if (result.Success)
        {
            try
            {
                using (resource)
                using (var texture = resource.QueryInterface<ID3D11Texture2D>())
                {
                    if (_staging is null)
                    {
                        var desc = texture.Description; desc.Usage = ResourceUsage.Staging; desc.BindFlags = BindFlags.None;
                        desc.CPUAccessFlags = CpuAccessFlags.Read; desc.MiscFlags = ResourceOptionFlags.None;
                        _staging = _device.CreateTexture2D(desc);
                    }
                    _context.CopyResource(_staging, texture); _hasFrame = true;
                }
            }
            finally { _duplication.ReleaseFrame(); }
        }
        else if (result.Code != unchecked((int)0x887A0027)) result.CheckError(); // DXGI_ERROR_WAIT_TIMEOUT
        if (!_hasFrame || _staging is null) return false;
        var data = _context.Map(_staging, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            var desc = _staging.Description;
            using var bitmap = new Bitmap((int)desc.Width, (int)desc.Height, (int)data.RowPitch, PixelFormat.Format32bppRgb, data.DataPointer);
            target.DrawImage(bitmap, 0, 0, width, height);
            return true;
        }
        finally { _context.Unmap(_staging, 0); }
    }
    public void Dispose() { _staging?.Dispose(); _duplication.Dispose(); _context.Dispose(); _device.Dispose(); }
}
