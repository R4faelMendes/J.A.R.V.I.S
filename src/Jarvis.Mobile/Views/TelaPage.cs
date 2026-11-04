namespace Jarvis.Mobile.Views;

public sealed class TelaPage : ContentPage
{
    private readonly QuadroAoVivo _screen = new();
    private readonly Label _status = new() { Text = "Escolha a tela do PC pareado e inicie a visualização.", FontSize = 12, MaxLines = 3 };
    private readonly Button _toggle = new() { Text = "Iniciar visualização" };
    private readonly SemaphoreSlim _gesto = new(1, 1);
    private readonly Picker _monitor = new() { Title = "Escolher tela do PC", ItemDisplayBinding = new Binding("Nome") };
    private readonly Picker _fps = new() { Title = "FPS", ItemsSource = new[] { "15 FPS", "30 FPS", "60 FPS" }, SelectedIndex = 2 };
    private readonly Picker _resolucao = new() { Title = "Resolução", ItemsSource = new[] { "640 · conexão lenta", "960 · fluidez", "1280 · equilíbrio", "1920 · detalhes" }, SelectedIndex = 1 };
    private CancellationTokenSource? _life;
    private bool _visible;
    private bool _full;
    private readonly Grid _root;
    private readonly VerticalStackLayout _header;
    private readonly Button _exit = new() { Text = "Sair da tela cheia", IsVisible = false, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start, Margin = 12, Opacity = .85 };
    public TelaPage()
    {
        Title = "Tela ao vivo";
        var root = new Grid { Padding = 12, RowSpacing = 10, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) } };
        var header = new VerticalStackLayout { Spacing = 4 };
        _root = root; _header = header;
        var fullscreen = new Button { Text = "⛶ Tela cheia" }; fullscreen.Clicked += (_, _) => TelaCheia(true); header.Add(fullscreen);
        header.Add(new Label { Text = "Suas telas · ao vivo", FontSize = 22 });
        var select = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } }; select.Add(_monitor, 0);
        var refresh = new Button { Text = "Atualizar", FontSize = 12 }; refresh.Clicked += async (_, _) => await MonitoresAsync(); select.Add(refresh, 1); header.Add(select);
        var options = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } }; options.Add(_fps, 0); options.Add(_resolucao, 1); header.Add(options);
        var controls = new Grid { ColumnSpacing = 6, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) } };
        foreach (var (label, acao, ticks, key, column) in new[] { ("Rolar ↑", "rolar", 3, (string?)null, 0), ("Rolar ↓", "rolar", -3, (string?)null, 1), ("Enter", "tecla", 0, "Enter", 2) })
        {
            var button = new Button { Text = label, FontSize = 11, Padding = new Thickness(8, 6) };
            button.Clicked += async (_, _) => { if (_monitor.SelectedItem is Jarvis.Core.MonitorInfo m) await EnviarGestoAsync(new(m.Id, acao, Rolagem: ticks, Tecla: key)); }; controls.Add(button, column);
        }
        header.Add(controls); header.Add(new Label { Text = "Toque: clique · 2 toques: abrir · segurar: botão direito · deslizar: arrastar · 2 dedos: rolar", FontSize = 10, TextColor = Color.FromArgb("#8FA9BF") });
        _screen.Gesto += async pedido => await EnviarGestoAsync(pedido);
        root.Add(header, 0, 0); root.Add(_screen, 0, 1); root.Add(_status, 0, 2); root.Add(_toggle, 0, 3);
        root.Add(_exit, 0, 0); Grid.SetRowSpan(_exit, 4); _exit.ZIndex = 10; _exit.Clicked += (_, _) => TelaCheia(false);
        void Restart(object? sender, EventArgs e) { if (_life is not null) { Stop("Opções alteradas."); Start(); } }
        _monitor.SelectedIndexChanged += Restart; _fps.SelectedIndexChanged += Restart; _resolucao.SelectedIndexChanged += Restart;
        _toggle.Clicked += (_, _) => { if (_life is null) Start(); else Stop("Visualização pausada."); };
        Content = root;
    }
    protected override async void OnAppearing() { base.OnAppearing(); _visible = true; App.AtividadeMudou += Atividade; await MonitoresAsync(); }
    private async Task MonitoresAsync()
    {
        Stop("Buscando monitores…");
        try { var id = (_monitor.SelectedItem as Jarvis.Core.MonitorInfo)?.Id; var monitors = await App.Conexao.ListarMonitoresAsync(); _monitor.ItemsSource = monitors; _monitor.SelectedItem = monitors.FirstOrDefault(m => m.Id == id) ?? monitors.FirstOrDefault(m => m.Principal) ?? monitors.FirstOrDefault(); _status.Text = $"{monitors.Count} telas disponíveis. Escolha uma e toque em Iniciar."; }
        catch (Exception ex) { _status.Text = ex.Message; }
    }
    protected override void OnDisappearing() { TelaCheia(false); _visible = false; App.AtividadeMudou -= Atividade; Stop("Visualização pausada."); base.OnDisappearing(); }
    private void TelaCheia(bool full)
    {
        _full = full; _header.IsVisible = _status.IsVisible = _toggle.IsVisible = !full; _exit.IsVisible = full;
        _root.Padding = full ? 0 : 12; _root.RowSpacing = full ? 0 : 10; _root.BackgroundColor = full ? Colors.Black : Colors.Transparent;
        Grid.SetRow(_screen, full ? 0 : 1); Grid.SetRowSpan(_screen, full ? 4 : 1);
        Shell.SetNavBarIsVisible(this, !full); Shell.SetTabBarIsVisible(this, !full);
#if ANDROID
        Services.TelaCheiaAndroid.Definir(full);
#endif
    }
    protected override bool OnBackButtonPressed() { if (_full) { TelaCheia(false); return true; } return base.OnBackButtonPressed(); }
    private void Atividade(bool ativa) { if (!ativa) MainThread.BeginInvokeOnMainThread(() => Stop("Pausada ao sair do app.")); }
    private void Start()
    {
        if (!_visible || _life is not null) return;
        if (_monitor.SelectedItem is not Jarvis.Core.MonitorInfo monitor) { _status.Text = "Escolha um monitor."; return; }
        var fps = new[] { 15, 30, 60 }[Math.Max(0, _fps.SelectedIndex)];
        var width = new[] { 640, 960, 1280, 1920 }[Math.Max(0, _resolucao.SelectedIndex)];
        var cts = new CancellationTokenSource(); _life = cts; _toggle.Text = "Parar visualização";
        _screen.MonitorId = monitor.Id; DefinirToque(true);
        _status.Text = $"Conectando · meta {fps} FPS…";
        _ = LoopAsync(cts, monitor.Id, new(fps, width, 60));
    }
    private void Stop(string mensagem) { _life?.Cancel(); _life = null; DefinirToque(false); _screen.Limpar(); _toggle.Text = "Iniciar visualização"; _status.Text = mensagem; }
    private void DefinirToque(bool ativo) { _screen.ControleAtivo = ativo; }
    private async Task EnviarGestoAsync(Jarvis.Core.PedidoToque pedido)
    {
        var life = _life;
        if (!_screen.ControleAtivo || life is null || !await _gesto.WaitAsync(0)) return;
        try { await App.Conexao.ToqueAsync(pedido, life.Token); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (ReferenceEquals(_life, life)) _status.Text = ex.Message; }
        finally { _gesto.Release(); }
    }
    private async Task LoopAsync(CancellationTokenSource cts, string monitor, Jarvis.Core.OpcoesTransmissao options)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew(); var count = 0; long bytes = 0;
        try
        {
            await App.Conexao.TransmitirTelaAsync(monitor, options, async data =>
            {
                await _screen.ApresentarAsync(data, cts.Token); count++; bytes += data.Length;
                if (clock.Elapsed.TotalSeconds >= 1)
                {
                    var text = $"Ao vivo · {count / clock.Elapsed.TotalSeconds:F0} FPS · meta {options.Fps} · {bytes * 8 / clock.Elapsed.TotalSeconds / 1_000_000:F1} Mb/s";
                    await MainThread.InvokeOnMainThreadAsync(() => { if (ReferenceEquals(_life, cts)) _status.Text = text; });
                    clock.Restart(); count = 0; bytes = 0;
                }
            }, cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (ReferenceEquals(_life, cts)) Stop(ex.Message); }
        finally { if (ReferenceEquals(_life, cts)) { _life = null; _toggle.Text = "Iniciar visualização"; } cts.Dispose(); }
    }
}
