using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Jarvis.Core;
namespace Jarvis.Sistema;

/// <summary>Captura sob demanda. Nenhuma gravação em disco, frame limitado e reutilizado por 300 ms.</summary>
public sealed class TransmissaoTela : ITransmissaoTela
{
    private readonly object _gate = new();
    private volatile bool _permitida = true;
    private QuadroTela? _cache;
    private string? _monitorCache;
    private DateTimeOffset? _ultima;
    public bool Permitida { get => _permitida; set { lock (_gate) { _permitida = value; _cache = null; } } }
    public DateTimeOffset? UltimaVisualizacao { get { lock (_gate) return _ultima; } }
    [DllImport("user32.dll")] private static extern nint SetThreadDpiAwarenessContext(nint c);
    [DllImport("user32.dll")] private static extern nint OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetUserObjectInformation(nint obj, int index, System.Text.StringBuilder value, int size, out int needed);
    [DllImport("user32.dll")] private static extern bool CloseDesktop(nint desktop);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern bool StretchBlt(nint target, int x, int y, int width, int height, nint source, int sx, int sy, int sw, int sh, uint operation);
    [DllImport("gdi32.dll")] private static extern int SetStretchBltMode(nint dc, int mode);
    public QuadroTela Capturar() => Capturar(null);
    public ISessaoTela AbrirSessao(string? monitorId, OpcoesTransmissao opcoes)
    {
        opcoes.Validar();
        VerificarPermissao();
        return new Sessao(this, monitorId, opcoes);
    }
    private void VerificarPermissao()
    {
        if (!Permitida) throw new InvalidOperationException("Transmissão indisponível no PC.");
        var desktop = OpenInputDesktop(0, false, 1);
        if (desktop == 0) throw new InvalidOperationException("Tela bloqueada ou protegida.");
        try { var nome = new System.Text.StringBuilder(256); if (!GetUserObjectInformation(desktop, 2, nome, 512, out _) || !nome.ToString().Equals("Default", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Transmissão pausada na tela de segurança."); }
        finally { CloseDesktop(desktop); }
    }
    private sealed class Sessao : ISessaoTela
    {
        private readonly TransmissaoTela _owner;
        private readonly string _id;
        private readonly Rectangle _area;
        private readonly Bitmap _scaled;
        private readonly Graphics _resize;
        private readonly MemoryStream _buffer = new();
        private readonly EncoderParameters _parameters = new(1);
        private readonly ImageCodecInfo _codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        private CapturaGpu? _gpu;
        public string Motor { get; private set; }
        public Sessao(TransmissaoTela owner, string? id, OpcoesTransmissao options)
        {
            _owner = owner;
            var old = SetThreadDpiAwarenessContext(-4);
            try
            {
                var monitor = id is null ? Screen.PrimaryScreen : Screen.AllScreens.FirstOrDefault(s => s.DeviceName == id);
                if (monitor is null) throw new InvalidOperationException("Monitor desconectado. Atualize a lista.");
                _id = monitor.DeviceName; _area = monitor.Bounds;
                var scale = Math.Min(1, (double)options.Largura / _area.Width);
                _scaled = new Bitmap(Math.Max(1, (int)(_area.Width * scale)), Math.Max(1, (int)(_area.Height * scale)), PixelFormat.Format24bppRgb);
                _resize = Graphics.FromImage(_scaled);
                _resize.InterpolationMode = InterpolationMode.Bilinear;
                _resize.CompositingMode = CompositingMode.SourceCopy;
                _parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)options.Qualidade);
                _gpu = CapturaGpu.Criar(_id, _area, out var motor); Motor = motor;
            }
            finally { if (old != 0) SetThreadDpiAwarenessContext(old); }
        }
        public QuadroTela Capturar()
        {
            _owner.VerificarPermissao();
            var old = SetThreadDpiAwarenessContext(-4);
            try
            {
                if (!Screen.AllScreens.Any(s => s.DeviceName == _id && s.Bounds == _area)) throw new InvalidOperationException("Monitor alterado. Reinicie a visualização.");
                var gpuFrame = false;
                if (_gpu is not null)
                {
                    try { gpuFrame = _gpu.Desenhar(_resize, _scaled.Width, _scaled.Height); }
                    catch (Exception ex) { Motor = $"GDI · Direct3D interrompido ({ex.HResult:X8})"; System.Diagnostics.Trace.WriteLine("Captura GPU passou para GDI: " + ex.Message); _gpu.Dispose(); _gpu = null; }
                }
                if (!gpuFrame)
                {
                var source = GetDC(0); var target = _resize.GetHdc();
                try
                {
                    SetStretchBltMode(target, 3);
                    if (!StretchBlt(target, 0, 0, _scaled.Width, _scaled.Height, source, _area.X, _area.Y, _area.Width, _area.Height, 0x00CC0020))
                        throw new InvalidOperationException("O Windows recusou a captura da tela.");
                }
                finally { _resize.ReleaseHdc(target); if (source != 0) ReleaseDC(0, source); }
                }
                _buffer.Position = 0; _buffer.SetLength(0); _scaled.Save(_buffer, _codec, _parameters);
                var instant = DateTimeOffset.UtcNow; lock (_owner._gate) _owner._ultima = instant;
                return new(_buffer.ToArray(), _scaled.Width, _scaled.Height, instant);
            }
            finally { if (old != 0) SetThreadDpiAwarenessContext(old); }
        }
        public void Dispose() { _gpu?.Dispose(); _resize.Dispose(); _scaled.Dispose(); _parameters.Dispose(); _buffer.Dispose(); }
    }
    public QuadroTela Capturar(string? monitorId)
    {
        lock (_gate)
        {
            if (!Permitida) throw new InvalidOperationException("Transmissão indisponível no PC.");
            var desktop = OpenInputDesktop(0, false, 1);
            if (desktop == 0) { _cache = null; throw new InvalidOperationException("Tela indisponível: sessão bloqueada ou área de trabalho protegida."); }
            try
            {
                var nome = new System.Text.StringBuilder(256);
                if (!GetUserObjectInformation(desktop, 2, nome, 512, out _) || !nome.ToString().Equals("Default", StringComparison.OrdinalIgnoreCase))
                { _cache = null; throw new InvalidOperationException("A transmissão é pausada na tela de bloqueio ou de segurança."); }
            }
            finally { CloseDesktop(desktop); }
            _ultima = DateTimeOffset.UtcNow;
            var antigo = SetThreadDpiAwarenessContext(-4);
            try
            {
                var monitor = monitorId is null ? Screen.PrimaryScreen : Screen.AllScreens.FirstOrDefault(s => s.DeviceName == monitorId);
                if (monitor is null) throw new InvalidOperationException("Monitor desconectado. Atualize a lista e escolha outra tela.");
                if (_cache is not null && _monitorCache == monitor.DeviceName && _ultima.Value - _cache.Instante < TimeSpan.FromMilliseconds(300)) return _cache;
                var area = monitor.Bounds; _monitorCache = monitor.DeviceName;
                using var original = new Bitmap(area.Width, area.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(original)) g.CopyFromScreen(area.Location, Point.Empty, area.Size, CopyPixelOperation.SourceCopy);
                var escala = Math.Min(1, Math.Min(1280d / area.Width, 720d / area.Height));
                var largura = Math.Max(1, (int)(area.Width * escala)); var altura = Math.Max(1, (int)(area.Height * escala));
                using var reduzida = new Bitmap(largura, altura, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(reduzida)) { g.InterpolationMode = InterpolationMode.HighQualityBilinear; g.DrawImage(original, 0, 0, largura, altura); }
                using var stream = new MemoryStream();
                using var parametros = new EncoderParameters(1);
                parametros.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 65L);
                reduzida.Save(stream, ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid), parametros);
                return _cache = new(stream.ToArray(), largura, altura, DateTimeOffset.UtcNow);
            }
            finally { if (antigo != 0) SetThreadDpiAwarenessContext(antigo); }
        }
    }
}
