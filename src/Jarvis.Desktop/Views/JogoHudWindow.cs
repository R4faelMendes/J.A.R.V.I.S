using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Jarvis.Core;
namespace Jarvis.Desktop.Views;

public sealed class JogoHudWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly TextBlock _text = new() { FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 12, Margin = new Thickness(14), TextWrapping = TextWrapping.Wrap };
    private bool _busy;
    private readonly CancellationTokenSource _life = new();
    public JogoHudWindow(IInstrumentacaoJogo monitor)
    {
        Title = "Jarvis · HUD de desempenho"; Width = 290; Height = 210; ResizeMode = ResizeMode.NoResize; Topmost = true; ShowInTaskbar = false; WindowStyle = WindowStyle.ToolWindow;
        SetResourceReference(BackgroundProperty, "FundoAbismo"); _text.SetResourceReference(TextBlock.ForegroundProperty, "AcaoCiano"); Content = _text;
        Left = SystemParameters.WorkArea.Right - Width - 20; Top = SystemParameters.WorkArea.Top + 24;
        async Task Update() {
            if (_busy || _life.IsCancellationRequested) return; _busy = true;
            try { var value = await monitor.LerAsync(_life.Token); if (_life.IsCancellationRequested) return; var selected = value.Sensores.Where(s => s.Nome is "CPU" or "GPU" or "RAM" || s.Unidade is "°C" or "MHz" or "RPM").Take(9);
                _text.Text = "JARVIS / DESEMPENHO\n\n" + string.Join("\n", selected.Select(s => $"{s.Nome,-20} {(s.Valor is { } v ? v.ToString("0.#") : "—")} {s.Unidade}"));
            } catch (OperationCanceledException) { } catch { _text.Text = "Sensores indisponíveis."; } finally { _busy = false; }
        }
        Loaded += async (_, _) => { _timer.Start(); await Update(); }; _timer.Tick += async (_, _) => await Update(); Closed += (_, _) => { _timer.Stop(); _life.Cancel(); };
    }
}
