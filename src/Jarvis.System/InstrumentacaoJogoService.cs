using Jarvis.Core;
using LibreHardwareMonitor.Hardware;
namespace Jarvis.Sistema;

public sealed class InstrumentacaoJogoService : IInstrumentacaoJogo
{
    private readonly object _gate = new();
    private Computer? _computer;
    private PerfMonitor? _perf;
    private readonly Dictionary<string, IControl> _controls = new();
    private readonly HashSet<string> _alteradas = new();
    private DateTimeOffset _ultimo;
    private AmostraJogo? _cache;
    public Task<AmostraJogo> LerAsync(CancellationToken ct = default) => Task.Run(() => {
        lock (_gate) {
            ct.ThrowIfCancellationRequested();
            if (_cache is not null && DateTimeOffset.UtcNow - _ultimo < TimeSpan.FromMilliseconds(900)) return _cache;
            _perf ??= new PerfMonitor();
            var readings = new List<SensorJogo>(); var controls = new List<ControleVentoinha>(); var hardware = new List<string>();
            static double? V(double value) => double.IsFinite(value) ? value : null;
            readings.Add(new("CPU", "%", V(_perf.CpuPercent))); readings.Add(new("RAM", "%", V(_perf.MemoriaPercent))); readings.Add(new("GPU", "%", V(_perf.GpuPercent)));
            try {
                if (_computer is null) { _computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true }; _computer.Open(); }
                foreach (var hw in _computer.Hardware) {
                    hw.Update(); hardware.Add(hw.Name);
                    var gpu = hw.HardwareType is HardwareType.GpuAmd or HardwareType.GpuNvidia or HardwareType.GpuIntel;
                    var prefix = gpu ? "GPU · " : "CPU · ";
                    foreach (var s in hw.Sensors) {
                        var unit = s.SensorType switch { SensorType.Temperature => "°C", SensorType.Clock => "MHz", SensorType.Power => "W", SensorType.Fan => "RPM", SensorType.Load => "%", SensorType.SmallData => "MB", SensorType.Data => "GB", SensorType.Control => "%", _ => "" };
                        if (unit.Length > 0) readings.Add(new(prefix + s.Name, unit, s.Value is { } v && float.IsFinite(v) ? v : null));
                        if (gpu && s.SensorType == SensorType.Control && s.Name.Contains("Fan", StringComparison.OrdinalIgnoreCase) && s.Control is { } control && control.MaxSoftwareValue >= 25 && control.MinSoftwareValue < control.MaxSoftwareValue) {
                            var id = s.Identifier.ToString(); _controls[id] = control; controls.Add(new(id, hw.Name + " · " + s.Name, control.MinSoftwareValue, control.MaxSoftwareValue));
                        }
                    }
                }
            } catch { /* Apenas valores efetivamente lidos são apresentados. */ }
            if (readings[2].Valor is null) {
                var loads = readings.Where(s => s.Nome.StartsWith("GPU · ") && s.Unidade == "%" && s.Valor.HasValue && (s.Nome.Contains("D3D 3D") || s.Nome.EndsWith("GPU Core"))).Select(s => s.Valor!.Value).ToArray();
                if (loads.Length > 0) readings[2] = new("GPU", "%", Math.Clamp(loads.Max(), 0, 100));
            }
            _ultimo = DateTimeOffset.UtcNow; return _cache = new(_ultimo, readings, controls, string.Join(" · ", hardware));
        }
    }, ct);
    public Task DefinirVentoinhaAsync(string id, float? porcentagem) => Task.Run(() => {
        lock (_gate) {
            if (!_controls.TryGetValue(id, out var control)) throw new InvalidOperationException("O driver não disponibilizou esse controle de ventoinha.");
            if (porcentagem is null) { control.SetDefault(); _alteradas.Remove(id); return; }
            if (!float.IsFinite(porcentagem.Value) || porcentagem < Math.Max(25, control.MinSoftwareValue) || porcentagem > control.MaxSoftwareValue) throw new ArgumentOutOfRangeException(nameof(porcentagem));
            control.SetSoftware(porcentagem.Value); _alteradas.Add(id);
        }
    });
    public void Restaurar() { lock (_gate) { foreach (var id in _alteradas.ToArray()) { if (_controls.TryGetValue(id, out var c)) { try { c.SetDefault(); _alteradas.Remove(id); } catch { } } } } }
    public void Dispose() { lock (_gate) { Restaurar(); _computer?.Close(); _computer = null; _perf?.Dispose(); _perf = null; } }
}
