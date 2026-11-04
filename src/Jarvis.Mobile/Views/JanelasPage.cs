using Jarvis.Core;
namespace Jarvis.Mobile.Views;

public sealed class JanelasPage : ContentPage
{
    private readonly Label _status = new() { Text = "Carregando janelas…", TextColor = Color.FromArgb("#4FD6FF") };
    private readonly Label _selection = new() { Text = "Nenhuma janela selecionada" };
    private readonly VerticalStackLayout _lista = new() { Spacing = 12 };
    private readonly Picker _monitor = new() { Title = "Monitor de destino", ItemDisplayBinding = new Binding("Nome") };
    private readonly Picker _layout = new() { Title = "Grade", ItemsSource = new[] { "Duas colunas · 2 janelas", "Três colunas · 3 janelas", "Foco + lateral · 2 janelas", "Grade 2 × 2 · 4 janelas" }, SelectedIndex = 0 };
    private readonly List<long> _ordem = new();
    private readonly Dictionary<long, Label> _numbers = new();
    private readonly Dictionary<long, int> _posicoes = new();
    private static readonly string[] Modelos = { "Metade", "TresColunas", "FocoLateral", "Grade2x2" };
    private bool _busy;
    public JanelasPage()
    {
        Title = "Janelas do PC";
        _layout.SelectedIndexChanged += async (_, _) => { _posicoes.Clear(); await CarregarAsync(); };
        var stack = new VerticalStackLayout { Padding = 18, Spacing = 12 };
        stack.Add(new Label { Text = "Organize seu desktop", FontSize = 26 }); stack.Add(_status);
        stack.Add(new Label { Text = "Escolha o monitor, a grade e a posição de cada janela. Uma única janela pode ocupar a direita ou qualquer canto da grade.", FontSize = 13 });
        var top = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
        var refresh = Botao("Atualizar", async () => await CarregarAsync()); var open = Botao("Abrir aplicativo", async () => await Shell.Current.GoToAsync("//apps")); top.Add(refresh, 0); top.Add(open, 1); stack.Add(top);
        var live = Botao("Ver tela principal ao vivo", async () => await Shell.Current.GoToAsync("tela")); stack.Add(live);
        stack.Add(_monitor); stack.Add(_layout); stack.Add(_selection);
        var apply = Botao("Aplicar às janelas selecionadas", AplicarAsync); apply.Style = (Style)Application.Current!.Resources["Primario"]; stack.Add(apply);
        stack.Add(_lista); Content = new ScrollView { Content = stack };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await CarregarAsync(); }
    private static Button Botao(string texto, Func<Task> action)
    { var b = new Button { Text = texto, MinimumHeightRequest = 48, FontSize = 13, Padding = new Thickness(10, 8) }; b.Clicked += async (_, _) => await action(); return b; }
    private void Numerar()
    {
        _selection.Text = _ordem.Count == 0 ? "Nenhuma janela selecionada" : $"{_ordem.Count} selecionadas · máximo {GradeJanelas.Capacidade(Modelos[Math.Max(0, _layout.SelectedIndex)])} nesta grade";
        foreach (var item in _numbers) item.Value.Text = _ordem.Contains(item.Key) ? "Incluída na grade" : "Fora da grade";
    }
    private async Task CarregarAsync()
    {
        if (_busy) return; _busy = true;
        try { await AtualizarAsync(); _status.Text = "Lista atualizada. Selecione as janelas para organizar."; }
        catch (Exception ex) { _status.Text = ex.Message; }
        finally { _busy = false; }
    }
    private async Task AtualizarAsync()
    {
        var windows = await App.Conexao.ListarJanelasAsync();
        var monitors = await App.Conexao.ListarMonitoresAsync();
        var old = (_monitor.SelectedItem as MonitorInfo)?.Id; _monitor.ItemsSource = monitors;
        _monitor.SelectedItem = monitors.FirstOrDefault(x => x.Id == old) ?? monitors.FirstOrDefault(x => x.Principal);
        _ordem.RemoveAll(id => !windows.Any(w => w.Id == id)); _lista.Clear(); _numbers.Clear();
        foreach (var window in windows)
        {
            var row = new VerticalStackLayout { Spacing = 8, Padding = 12, BackgroundColor = Color.FromArgb("#0A1D2C") };
            var header = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
            var check = new CheckBox { IsChecked = _ordem.Contains(window.Id), Color = Color.FromArgb("#4FD6FF") };
            check.CheckedChanged += (_, e) => { if (e.Value && !_ordem.Contains(window.Id)) _ordem.Add(window.Id); if (!e.Value) _ordem.Remove(window.Id); Numerar(); };
            header.Add(check, 0); header.Add(new Label { Text = window.Titulo, MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center }, 1); row.Add(header);
            var number = new Label { FontSize = 12, TextColor = Color.FromArgb("#4FD6FF") }; _numbers[window.Id] = number; row.Add(number);
            var names = PosicoesGrade.Nomes(Modelos[Math.Max(0, _layout.SelectedIndex)]);
            var position = new Picker { Title = "Posição desta janela", ItemsSource = new[] { "Automática" }.Concat(names).ToArray(), SelectedIndex = _posicoes.GetValueOrDefault(window.Id, -1) + 1 };
            position.SelectedIndexChanged += (_, _) => { if (position.SelectedIndex > 0) { _posicoes[window.Id] = position.SelectedIndex - 1; check.IsChecked = true; } else _posicoes.Remove(window.Id); Numerar(); }; row.Add(position);
            row.Add(new Label { Text = window.App, FontSize = 12 });
            var buttons = new Grid { ColumnSpacing = 6, RowSpacing = 6, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto) } };
            buttons.Add(Botao("Focar / restaurar", () => AcaoAsync("focar_janela", window)), 0, 0);
            buttons.Add(Botao("Minimizar", () => AcaoAsync("minimizar_janela", window)), 1, 0);
            buttons.Add(Botao("Maximizar", () => AcaoAsync("maximizar_janela", window)), 0, 1);
            buttons.Add(Botao("Fechar janela", () => AcaoAsync("fechar_janela", window)), 1, 1);
            row.Add(buttons); _lista.Add(row);
        }
        if (windows.Count == 0) _lista.Add(new Label { Text = "Nenhuma janela disponível. Abra um aplicativo no PC ou use Abrir aplicativo." });
        Numerar();
    }
    private async Task AcaoAsync(string comando, JanelaRemota janela)
    {
        if (_busy) return; _busy = true;
        try
        {
            var confirmado = comando == "fechar_janela";
            if (confirmado && !await DisplayAlert("Fechar janela", $"Fechar {janela.Titulo}? O aplicativo poderá pedir para salvar seu trabalho no PC.", "Fechar", "Cancelar")) return;
            var result = await App.Conexao.ComandoAsync(comando, janela.Id.ToString(), confirmado);
            if (result.StartsWith("Confirme") && !confirmado && await DisplayAlert("Permissão do PC", "Confirmar esta ação?", "Confirmar", "Cancelar")) result = await App.Conexao.ComandoAsync(comando, janela.Id.ToString(), true);
            await Task.Delay(300); await AtualizarAsync(); _status.Text = result;
        }
        catch (Exception ex) { _status.Text = ex.Message; }
        finally { _busy = false; }
    }
    private async Task AplicarAsync()
    {
        if (_busy) return; _busy = true;
        try
        {
            var modelo = Modelos[Math.Max(0, _layout.SelectedIndex)]; var max = GradeJanelas.Capacidade(modelo);
            if (_ordem.Count == 0 || _ordem.Count > max) { _status.Text = $"Selecione de 1 a {max} janelas para esta grade."; return; }
            if (_monitor.SelectedItem is not MonitorInfo monitor) { _status.Text = "Escolha um monitor."; return; }
            var occupied = _ordem.Where(id => _posicoes.ContainsKey(id)).Select(id => _posicoes[id]).ToHashSet();
            var free = new Queue<int>(Enumerable.Range(0, max).Where(p => !occupied.Contains(p)));
            var positions = _ordem.Select(id => _posicoes.TryGetValue(id, out var p) ? p : free.Count > 0 ? free.Dequeue() : -1).ToArray();
            var pedido = new PedidoLayout(modelo, monitor.Id, _ordem.ToArray(), positions);
            ResultadoLayout result;
            try { result = await App.Conexao.OrganizarAsync(pedido, false); }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("Confirme"))
            { if (!await DisplayAlert("Organizar janelas", "Confirmar a aplicação da grade no PC?", "Aplicar", "Cancelar")) return; result = await App.Conexao.OrganizarAsync(pedido, true); }
            _status.Text = $"{result.Movidas} janelas organizadas." + (result.Avisos.Count > 0 ? " " + string.Join(" ", result.Avisos) : "");
        }
        catch (Exception ex) { _status.Text = ex.Message; }
        finally { _busy = false; }
    }
}
