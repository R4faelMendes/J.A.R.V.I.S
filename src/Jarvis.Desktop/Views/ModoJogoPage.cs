using System.Windows;
using System.Windows.Controls;
using Jarvis.Core;
using Jarvis.Desktop.Services;
namespace Jarvis.Desktop.Views.Pages;

public sealed class ModoJogoPage : PaginaBase
{
    private readonly System.Windows.Threading.DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly CancellationTokenSource _life = new();
    private readonly Dictionary<string, TextBlock> _metrics = new();
    private readonly HashSet<string> _fanIds = new();
    private StackPanel _fans = null!;
    private TextBlock _hardware = null!;
    private readonly TelemetryTrend _cpu = new(), _gpu = new();
    private bool _busy;
    private JogoHudWindow? _hud;
    private bool _registrando;
    private readonly List<AmostraJogo> _registro = new();
    public ModoJogoPage() : base("CENTRAL DE DESEMPENHO", "Modo de jogo · monitor e controle") {
        Loaded += async (_, _) => { _timer.Start(); await Atualizar(); }; _timer.Tick += async (_, _) => await Atualizar(); Unloaded += (_, _) => { _timer.Stop(); _life.Cancel(); };
    }
    private void Monitor(StackPanel stack)
    {
        _hardware = DiagnosticoPage.Label("Consultando sensores disponíveis…"); stack.Children.Add(_hardware);
        var grid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 3 };
        foreach (var name in new[] { "CPU", "GPU", "RAM", "Temperatura GPU", "Clock GPU", "Ventoinha" }) {
            var body = new StackPanel(); body.Children.Add(DiagnosticoPage.Label(name));
            var text = new TextBlock { Text = "—", FontSize = 30, Margin = new Thickness(0, 4, 0, 8) }; text.SetResourceReference(TextBlock.ForegroundProperty, "AcaoCiano"); body.Children.Add(text); _metrics[name] = text;
            var card = CriarCardVidro(conteudo: body); card.Margin = new Thickness(0, 0, 10, 10); card.CornerRadius = new CornerRadius(12); grid.Children.Add(card);
        }
        stack.Children.Add(grid);
        var graphs = new Grid { Height = 90, Margin = new Thickness(0, 8, 0, 20) }; graphs.ColumnDefinitions.Add(new()); graphs.ColumnDefinitions.Add(new()); _cpu.Margin = new Thickness(0, 0, 12, 0); Grid.SetColumn(_gpu, 1); graphs.Children.Add(_cpu); graphs.Children.Add(_gpu); stack.Children.Add(graphs);
        var hud = new Button { Content = "Abrir HUD de sensores", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
        hud.Click += (_, _) => { if (_hud?.IsVisible == true) { _hud.Activate(); return; } _hud = new JogoHudWindow(ServicosAplicacao.InstrumentacaoJogo); _hud.Show(); }; stack.Children.Add(hud);
        var record = new Button { Content = "Registrar sessão de desempenho", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) }; stack.Children.Add(record);
        record.Click += (_, _) => {
            if (!_registrando) { _registro.Clear(); _registrando = true; record.Content = "Parar e exportar CSV"; return; }
            _registrando = false; record.Content = "Registrar sessão de desempenho";
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Registro de desempenho (*.csv)|*.csv", FileName = "jarvis-desempenho-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".csv" };
            if (dialog.ShowDialog() != true) return;
            static string Quote(string text) => "\"" + text.Replace("\"", "\"\"") + "\"";
            try { var lines = new List<string> { "instante;sensor;valor;unidade" }; foreach (var sample in _registro) foreach (var sensor in sample.Sensores) lines.Add($"{sample.Instante:O};{Quote(sensor.Nome)};{sensor.Valor?.ToString(System.Globalization.CultureInfo.InvariantCulture)};{Quote(sensor.Unidade)}"); System.IO.File.WriteAllLines(dialog.FileName, lines, new System.Text.UTF8Encoding(true)); MostrarToast("Sessão exportada."); } catch (Exception ex) { MostrarToast(ex.Message); }
        };
        stack.Children.Add(DiagnosticoPage.Label("HUD para jogos em janela ou sem bordas. FPS e frametime exigem uma fonte de quadros compatível; valores ausentes aparecem como —. Ajustes de clock e tensão não são expostos sem suporte validado do driver."));
        _fans = new StackPanel(); stack.Children.Add(CriarCardVidro("Ventilação da GPU", _fans));
        _fans.Children.Add(DiagnosticoPage.Label("Procurando controles de ventoinha disponibilizados pelo hardware…"));
    }
    private async Task Atualizar()
    {
        if (_busy || _life.IsCancellationRequested) return; _busy = true;
        try {
            var sample = await ServicosAplicacao.InstrumentacaoJogo.LerAsync(_life.Token); if (_life.IsCancellationRequested) return;
            if (_registrando && _registro.Count < 3600) _registro.Add(sample);
            _hardware.Text = string.IsNullOrWhiteSpace(sample.Hardware) ? "Sensores de CPU/GPU não disponibilizados pelo driver. Contadores do Windows continuam disponíveis." : sample.Hardware;
            SensorJogo? Find(string name) => name switch { "Temperatura GPU" => sample.Sensores.FirstOrDefault(s => s.Nome.StartsWith("GPU · ") && s.Unidade == "°C"), "Clock GPU" => sample.Sensores.FirstOrDefault(s => s.Nome.StartsWith("GPU · ") && s.Unidade == "MHz"), "Ventoinha" => sample.Sensores.FirstOrDefault(s => s.Nome.StartsWith("GPU · ") && s.Unidade == "RPM"), _ => sample.Sensores.FirstOrDefault(s => s.Nome == name) };
            foreach (var (name, label) in _metrics) { var s = Find(name); label.Text = s?.Valor is { } value ? $"{value:0.#} {s.Unidade}" : "—"; }
            _cpu.Add(Find("CPU")?.Valor); _gpu.Add(Find("GPU")?.Valor);
            if (sample.Ventoinhas.Count == 0 && _fanIds.Count == 0) { _fans.Children.Clear(); _fans.Children.Add(DiagnosticoPage.Label("Este driver não oferece controle de ventoinha ao Jarvis. O modo automático do fabricante permanece ativo.")); }
            foreach (var fan in sample.Ventoinhas) {
                if (!_fanIds.Add(fan.Id)) continue; if (_fanIds.Count == 1) _fans.Children.Clear();
                _fans.Children.Add(DiagnosticoPage.Label(fan.Nome)); var slider = new Slider { Minimum = Math.Max(25, fan.Minimo), Maximum = fan.Maximo, Value = Math.Max(40, fan.Minimo), TickFrequency = 5, IsSnapToTickEnabled = true }; _fans.Children.Add(slider);
                var row = new WrapPanel(); var apply = new Button { Content = "Aplicar velocidade selecionada", Margin = new Thickness(0, 0, 8, 8) }; var auto = new Button { Content = "Automático", Margin = new Thickness(0, 0, 0, 8) }; row.Children.Add(apply); row.Children.Add(auto); _fans.Children.Add(row);
                apply.Click += async (_, _) => { if (!ServicosAplicacao.Jogo.Ativo) { MostrarToast("Ative o modo de jogo antes de ajustar a ventilação."); return; } try { await ServicosAplicacao.InstrumentacaoJogo.DefinirVentoinhaAsync(fan.Id, (float)slider.Value); MostrarToast($"Ventoinha em {slider.Value:0}%. Ao sair do modo de jogo, o automático será restaurado."); } catch (Exception ex) { MostrarToast(ex.Message); } };
                auto.Click += async (_, _) => { try { await ServicosAplicacao.InstrumentacaoJogo.DefinirVentoinhaAsync(fan.Id, null); MostrarToast("Controle automático restaurado."); } catch (Exception ex) { MostrarToast(ex.Message); } };
            }
        } catch (OperationCanceledException) { } catch (Exception ex) { _hardware.Text = ex.Message; } finally { _busy = false; }
    }
    protected override void Montar(Grid container)
    {
        IModoJogo jogo = ServicosAplicacao.Jogo;
        var stack = new StackPanel();
        Monitor(stack);
        stack.Children.Add(DiagnosticoPage.Label("Ativa o alto desempenho disponível no Windows e reduz o consumo do Jarvis. Selecione os apps indesejados para fechar; processos do sistema, jogos e ferramentas de trabalho ficam protegidos. Salve seu trabalho antes de ativar."));
        var status = DiagnosticoPage.Label(jogo.Ativo ? "Modo de jogo ativo." : "Modo de jogo desativado."); stack.Children.Add(status);
        var list = new StackPanel(); var selections = new List<CheckBox>();
        void Atualizar() {
            list.Children.Clear(); selections.Clear();
            foreach (var app in jogo.ListarCandidatos()) {
                var check = new CheckBox { Content = $"{app.Nome} · PID {app.Id}", Tag = app.Id, Margin = new Thickness(0, 6, 0, 6) };
                selections.Add(check); list.Children.Add(check);
            }
            if (selections.Count == 0) list.Children.Add(DiagnosticoPage.Label("Nenhum app dispensável encontrado em segundo plano."));
        }
        Atualizar(); stack.Children.Add(CriarCardVidro("Apps que você autoriza fechar", list));
        var refresh = new Button { Content = "Atualizar aplicativos", HorizontalAlignment = HorizontalAlignment.Left }; refresh.Click += (_, _) => Atualizar(); stack.Children.Add(refresh);
        var toggle = new Button { Content = jogo.Ativo ? "Sair do modo de jogo" : "Ativar modo de jogo", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 16, 0, 12), Style = (Style)Application.Current.Resources["BotaoPrimario"] }; stack.Children.Add(toggle);
        toggle.Click += async (_, _) => {
            toggle.IsEnabled = false; refresh.IsEnabled = false;
            try {
                var result = jogo.Ativo ? await jogo.DesativarAsync() : await jogo.AtivarAsync(selections.Where(c => c.IsChecked == true).Select(c => (int)c.Tag).ToArray());
                status.Text = result.Mensagem; toggle.Content = jogo.Ativo ? "Sair do modo de jogo" : "Ativar modo de jogo"; Atualizar();
            } catch (Exception ex) { status.Text = "Não foi possível concluir: " + ex.Message; }
            finally { toggle.IsEnabled = true; refresh.IsEnabled = true; }
        };
        stack.Children.Add(DiagnosticoPage.Label("Ao sair ou encerrar o Jarvis, o plano de energia anterior é restaurado. Apps fechados não são reabertos automaticamente. O modo não altera overclock nem drivers."));
        container.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
}
