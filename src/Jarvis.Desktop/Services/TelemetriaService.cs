using Jarvis.Core;
namespace Jarvis.Desktop.Services;
public sealed class TelemetriaService : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPerfMonitor? _monitor;
    private TelemetriaPc? _cache;
    public async Task<TelemetriaPc> ObterAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_cache is not null && DateTimeOffset.UtcNow - _cache.Instante < TimeSpan.FromSeconds(ConfiguracaoApp.ModoEconomico ? 5 : 1)) return _cache;
            return _cache = await Task.Run(() =>
            {
                _monitor ??= ServicosAplicacao.CriarMonitor();
                static double? Validar(double v) => double.IsFinite(v) && v >= 0 ? v : null;
                var ram = Jarvis.Sistema.MemoriaWindows.Ler();
                return new TelemetriaPc(Validar(_monitor.CpuPercent), ram?.Percentual ?? Validar(_monitor.MemoriaPercent), Validar(_monitor.GpuPercent), Validar(_monitor.DiscoPercent), _monitor.TemperaturaC > 0 ? Validar(_monitor.TemperaturaC) : null, DateTimeOffset.UtcNow, ram?.UsadaBytes, ram?.TotalBytes);
            });
        }
        finally { _gate.Release(); }
    }
    public void Dispose() { (_monitor as IDisposable)?.Dispose(); }
}
