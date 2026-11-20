namespace Jarvis.Mobile.Views;
public sealed class MidiaCard : ContentView
{
    private readonly Image _cover = new() { HeightRequest = 64, WidthRequest = 64, Aspect = Aspect.AspectFill };
    private readonly Label _title = new() { Text = "Spotify no computador", FontSize = 15, MaxLines = 2 };
    private readonly Label _artist = new() { Text = "Conecte-se ao PC e inicie uma música", FontSize = 12 };
    private readonly Label _status = new() { FontSize = 11, TextColor = Color.FromArgb("#BBBBBB") };
    private CancellationTokenSource? _life;
    private byte[]? _coverBytes;
    private bool _wanted;
    public MidiaCard()
    {
        var root = new VerticalStackLayout { Spacing = 10 };
        var header = new Grid { ColumnSpacing = 14, ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } }; header.Add(_cover, 0); var text = new VerticalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center }; text.Add(_title); text.Add(_artist); header.Add(text, 1); root.Add(header);
        var actions = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) } };
        var index = 0; foreach (var (label, command) in new[] { ("◀", "midia_anterior"), ("Tocar / pausar", "midia_alternar"), ("▶", "midia_proxima") }) { var button = new Button { Text = label, FontSize = 12 }; button.Clicked += async (_, _) => { try { _status.Text = await App.Conexao.ComandoAsync(command); await AtualizarAsync(CancellationToken.None); } catch (Exception ex) { _status.Text = ex.Message; } }; actions.Add(button, index++); } root.Add(actions);
        var notification = new Button { Text = "Controle na notificação" }; notification.Clicked += async (_, _) => { try { await Services.MidiaNotificacao.AtivarAsync(); _status.Text = "Controles disponíveis na notificação do Android."; } catch (Exception ex) { _status.Text = ex.Message; } }; root.Add(notification);
        var stop = new Button { Text = "Ocultar controle da notificação", FontSize = 11 }; stop.Clicked += (_, _) => { Services.MidiaNotificacao.Parar(); _status.Text = "Controle da notificação encerrado."; }; root.Add(stop); root.Add(_status); Content = new Border { Content = root, Style = (Style)Application.Current.Resources["Painel"] };
        Loaded += (_, _) => App.AtividadeMudou += Atividade;
        Unloaded += (_, _) => { App.AtividadeMudou -= Atividade; Ativar(false); };
    }
    public void Ativar(bool active)
    {
        if (_wanted != active)
        {
            _wanted = active;
            if (active) App.Conexao.EstadoMudou += ConexaoMudou;
            else App.Conexao.EstadoMudou -= ConexaoMudou;
        }
        Sincronizar(App.EmPrimeiroPlano);
    }

    private void ConexaoMudou(object? sender, string mensagem)
    {
        if ((App.EmPrimeiroPlano && App.Conexao.BaseUrl is not null) != (_life is not null))
            Atividade(App.EmPrimeiroPlano);
    }

    private void Atividade(bool emPrimeiroPlano) => MainThread.BeginInvokeOnMainThread(() => Sincronizar(emPrimeiroPlano));

    private void Sincronizar(bool emPrimeiroPlano)
    {
        var active = emPrimeiroPlano && _wanted && App.Conexao.BaseUrl is not null;
        if (active && _life is null)
        {
            var cts = new CancellationTokenSource();
            _life = cts;
            _ = MonitorarAsync(cts);
        }
        else if (!active && _life is { } atual)
        {
            _life = null;
            atual.Cancel();
        }
    }
    private async Task MonitorarAsync(CancellationTokenSource cts)
    {
        try { while (!cts.IsCancellationRequested) { try { await AtualizarAsync(cts.Token); } catch (OperationCanceledException) when (cts.IsCancellationRequested) { break; } catch { _status.Text = "PC offline · capa e comandos voltam ao conectar"; } await Task.Delay(3000, cts.Token); } } catch (OperationCanceledException) { } finally { cts.Dispose(); }
    }
    private async Task AtualizarAsync(CancellationToken ct)
    {
        var state = await App.Conexao.MidiaAsync(ct); ct.ThrowIfCancellationRequested();
        _title.Text = state.Titulo; _artist.Text = state.Artista; _status.Text = state.Disponivel ? state.Tocando ? "Tocando no PC" : "Pausado no PC" : "Abra o Spotify e inicie uma música no computador";
        if (_cover.Source is null || !ReferenceEquals(state.Capa, _coverBytes))
        {
            _coverBytes = state.Capa;
            _cover.Source = state.Capa is { Length: > 0 } bytes ? ImageSource.FromStream(() => new MemoryStream(bytes, false)) : "jarvis_mark.png";
        }
    }
}
