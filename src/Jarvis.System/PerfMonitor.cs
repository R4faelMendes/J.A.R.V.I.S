using System.Diagnostics;
using System.Runtime.InteropServices;
using Jarvis.Core;
using LibreHardwareMonitor.Hardware;

namespace Jarvis.Sistema;

/// <summary>Métricas reais. NaN indica que o contador ou sensor não está disponível.</summary>
public sealed class PerfMonitor : IPerfMonitor, IDisposable
{
    private readonly PerformanceCounter? _cpu;
    private readonly Dictionary<int, (TimeSpan Cpu, DateTime Hora)> _amostras = new();
    private PerformanceCounter[]? _gpu;
    private Computer? _hardware;
    private bool _tentouSensores;
    private DateTime _horaPropria = DateTime.UtcNow;
    private TimeSpan _cpuAnterior = Process.GetCurrentProcess().TotalProcessorTime;
    public PerfMonitor()
    {
        try { _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total"); _cpu.NextValue(); } catch { }
    }
    public double CpuPercent { get { try { return Math.Clamp(_cpu?.NextValue() ?? double.NaN, 0, 100); } catch { return double.NaN; } } }
    public double MemoriaPercent
    {
        get { var ram = MemoriaWindows.Ler(); return ram is null ? double.NaN : ram.Percentual; }
    }
    public ulong MemoriaTotalBytes => MemoriaWindows.Ler()?.TotalBytes ?? 0;
    public ulong MemoriaUsadaBytes => MemoriaWindows.Ler()?.UsadaBytes ?? 0;
    public double GpuPercent
    {
        get
        {
            try
            {
                if (_gpu is null)
                {
                    _gpu = new PerformanceCounterCategory("GPU Engine").GetInstanceNames().Where(n => n.Contains("engtype_3D"))
                        .Select(n => new PerformanceCounter("GPU Engine", "Utilization Percentage", n, true)).ToArray();
                    foreach (var c in _gpu) c.NextValue();
                    return double.NaN;
                }
                return _gpu.Length == 0 ? double.NaN : Math.Clamp(_gpu.Sum(c => c.NextValue()), 0, 100);
            }
            catch { return double.NaN; }
        }
    }
    public double DiscoPercent
    {
        get { try { var d = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!); return (double)(d.TotalSize - d.TotalFreeSpace) / d.TotalSize * 100; } catch { return double.NaN; } }
    }
    public double CpuPropria
    {
        get
        {
            using var p = Process.GetCurrentProcess();
            var agora = DateTime.UtcNow; var cpu = p.TotalProcessorTime;
            var delta = (agora - _horaPropria).TotalMilliseconds;
            var valor = delta > 0 ? (cpu - _cpuAnterior).TotalMilliseconds / delta / Environment.ProcessorCount * 100 : 0;
            _cpuAnterior = cpu; _horaPropria = agora; return Math.Clamp(valor, 0, 100);
        }
    }
    public double MemoriaPropriaMB { get { using var p = Process.GetCurrentProcess(); return p.WorkingSet64 / 1048576d; } }
    public double TemperaturaC
    {
        get
        {
            try
            {
                if (!_tentouSensores)
                {
                    _tentouSensores = true; _hardware = new Computer { IsCpuEnabled = true };
                    _hardware.Open();
                }
                if (_hardware is null) return double.NaN;
                foreach (var hw in _hardware.Hardware)
                {
                    hw.Update();
                    var valor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Value.HasValue)?.Value;
                    if (valor is > 0 and < 120) return valor.Value;
                }
            }
            catch { }
            return double.NaN;
        }
    }
    public IReadOnlyList<ProcessoInfo> TopProcessos()
    {
        var lista = new List<ProcessoInfo>(); var agora = DateTime.UtcNow;
        var novas = new Dictionary<int, (TimeSpan Cpu, DateTime Hora)>();
        foreach (var p in Process.GetProcesses())
        {
            using (p)
            {
                try
                {
                    if (p.Id == 0) continue;
                    var tempo = p.TotalProcessorTime; var cpu = double.NaN;
                    if (_amostras.TryGetValue(p.Id, out var antes) && agora > antes.Hora && tempo >= antes.Cpu)
                        cpu = Math.Clamp((tempo - antes.Cpu).TotalMilliseconds / (agora - antes.Hora).TotalMilliseconds / Environment.ProcessorCount * 100, 0, 100);
                    novas[p.Id] = (tempo, agora);
                    lista.Add(new(p.Id, p.ProcessName, cpu, p.WorkingSet64 / 1048576d));
                }
                catch { }
            }
        }
        _amostras.Clear(); foreach (var item in novas) _amostras[item.Key] = item.Value;
        return lista.OrderByDescending(p => p.MemoriaMB).Take(5).ToArray();
    }
    public void EncerrarProcesso(int id) { using var p = Process.GetProcessById(id); p.Kill(); }
    public void Dispose() { _cpu?.Dispose(); if (_gpu is not null) foreach (var c in _gpu) c.Dispose(); try { _hardware?.Close(); } catch { } }
}
