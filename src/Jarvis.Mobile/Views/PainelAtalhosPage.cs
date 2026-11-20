using System.Text.Json;
using Jarvis.Mobile.Services;

namespace Jarvis.Mobile.Views;

public sealed class PainelAtalhosPage : ContentPage
{
    private const int Total = 12;
    private static readonly Atalho[] Padrao =
    [
        new("Mídia", "▶", "midia_alternar"), new("Anterior", "◀", "midia_anterior"),
        new("Próxima", "▶▶", "midia_proxima"), new("Spotify", "♫", "spotify"),
        new("Volume 25%", "♪", "volume", "25"), new("Volume 50%", "♪", "volume", "50"),
        new("Volume 75%", "♪", "volume", "75"), new("Silenciar", "×", "volume", "0"),
        new("Acordar PC", "⏻", "acordar"), new("Bloquear PC", "◆", "bloquear")
    ];

    private readonly Grid _grade = new() { RowSpacing = 8, ColumnSpacing = 8 };
    private readonly Label _status = new() { LineBreakMode = LineBreakMode.TailTruncation };
    private readonly Label _musica = new() { LineBreakMode = LineBreakMode.TailTruncation };
    private readonly Button _editar = new() { Text = "Editar" };
    private readonly Atalho?[] _slots = new Atalho?[Total];
    private readonly Button[] _botoes = new Button[Total];
    private CancellationTokenSource? _vida;
    private bool _edicao, _ocupado;
    private string _chave = "";

    public PainelAtalhosPage()
    {
        Title = "Painel de atalhos";
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false);
        var sair = new Button { Text = "Sair" };
        sair.Clicked += async (_, _) => { if (!_ocupado) await Shell.Current.GoToAsync(".."); };
        _editar.Clicked += (_, _) =>
        {
            if (_ocupado) return;
            _edicao = !_edicao;
            _editar.Text = _edicao ? "Concluir" : "Editar";
            _status.Text = _edicao ? "Toque em um espaço para personalizar." : "";
            AtualizarBotoes();
        };
        var topo = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) } };
        topo.Add(new Label { Text = "Painel de atalhos", FontSize = 20, FontFamily = "ChakraPetchSemiBold", VerticalOptions = LayoutOptions.Center });
        topo.Add(_editar, 1);
        topo.Add(sair, 2);
        for (var col = 0; col < 4; col++) _grade.ColumnDefinitions.Add(new(GridLength.Star));
        for (var row = 0; row < 3; row++) _grade.RowDefinitions.Add(new(GridLength.Star));
        for (var i = 0; i < Total; i++)
        {
            var index = i;
            var botao = new Button { Padding = new Thickness(4), FontSize = 15 };
            botao.Clicked += async (_, _) => await AcionarAsync(index);
            _botoes[i] = botao;
            _grade.Add(botao, i % 4, i / 4);
        }
        var root = new Grid
        {
            Padding = 12, RowSpacing = 6,
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }
        };
        root.Add(topo);
        root.Add(_musica, 0, 1);
        root.Add(_grade, 0, 2);
        root.Add(_status, 0, 3);
        Content = root;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _chave = "painel_atalhos_v1_" + App.Conexao.IdentificadorPareamento;
        Array.Clear(_slots);
        Atalho?[]? carregado = null;
        try { carregado = JsonSerializer.Deserialize<Atalho?[]>(Preferences.Default.Get(_chave, "null")); }
        catch (JsonException) { }
        var origem = carregado ?? Padrao;
        Array.Copy(origem, _slots, Math.Min(origem.Length, Total));
        AtualizarBotoes();
#if ANDROID
        TelaCheiaAndroid.Definir(true);
#endif
        _vida = new CancellationTokenSource();
        _ = AtualizarMidiaAsync(_vida.Token);
    }

    protected override void OnDisappearing()
    {
        _vida?.Cancel();
        _vida?.Dispose();
        _vida = null;
#if ANDROID
        TelaCheiaAndroid.Definir(false);
#endif
        base.OnDisappearing();
    }

    private void AtualizarBotoes()
    {
        for (var i = 0; i < Total; i++)
        {
            var item = _slots[i];
            _botoes[i].Text = item is null ? (_edicao ? "+ Adicionar" : "") : $"{item.Icone}\n{item.Nome}";
            _botoes[i].IsEnabled = _edicao || item is not null;
            _botoes[i].BackgroundColor = item is null ? Colors.Black : Color.FromArgb("#161616");
            _botoes[i].BorderColor = item is null ? Color.FromArgb("#333333") : Color.FromArgb("#00D800");
        }
    }

    private async Task AtualizarMidiaAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            do
            {
                if (!App.EmPrimeiroPlano) continue;
                if (!App.Conexao.Conectado)
                {
                    _musica.Text = App.Conexao.Identidade?.Nome ?? "PC desconectado";
                    continue;
                }
                try
                {
                    var midia = await App.Conexao.MidiaAsync(ct);
                    if (!ct.IsCancellationRequested)
                        _musica.Text = midia.Disponivel ? $"♫  {midia.Titulo} · {midia.Artista}" : "Spotify no PC";
                }
                catch (Exception) when (!ct.IsCancellationRequested) { _musica.Text = "Mídia indisponível"; }
            } while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    private async Task AcionarAsync(int index)
    {
        if (_ocupado) return;
        _ocupado = true;
        try
        {
            if (_edicao) { await EditarAsync(index); return; }
            if (_slots[index] is not { } item) return;
            if (item.Comando == "acordar")
            {
                await App.Conexao.AcordarAsync();
                _status.Text = "Pacote Wake-on-LAN enviado.";
                return;
            }
            if (!App.Conexao.Conectado) { _status.Text = "PC desconectado. Use Acordar PC ou verifique o pareamento."; return; }
            if (item.Comando == "spotify")
            {
                var apps = await App.Conexao.ListarAppsAsync();
                var spotify = apps.FirstOrDefault(a => a.Nome.Equals("Spotify", StringComparison.OrdinalIgnoreCase));
                if (spotify is null) { _status.Text = "Selecione o Spotify na aba Apps para poder abri-lo."; return; }
                item = item with { Comando = "abrir_app", Argumento = spotify.Id };
            }
            var confirmado = item.Comando == "bloquear" || item.Confirmar;
            if (confirmado && !await DisplayAlert(item.Nome, "Executar no PC conectado?", "Executar", "Cancelar")) return;
            var resposta = await App.Conexao.ComandoAsync(item.Comando, item.Argumento, confirmado);
            if (!confirmado && resposta.StartsWith("Confirme", StringComparison.OrdinalIgnoreCase) &&
                await DisplayAlert(item.Nome, resposta, "Executar", "Cancelar"))
                resposta = await App.Conexao.ComandoAsync(item.Comando, item.Argumento, true);
            _status.Text = resposta;
        }
        catch (Exception e) { _status.Text = e.Message; }
        finally { _ocupado = false; }
    }

    private async Task EditarAsync(int index)
    {
        var escolha = await DisplayActionSheet("Personalizar botão", "Cancelar", null,
            "Escolher comando", "Abrir aplicativo", "Definir volume", "Renomear e trocar ícone", "Mover", "Remover");
        Atalho? novo = _slots[index];
        switch (escolha)
        {
            case "Escolher comando":
                var nome = await DisplayActionSheet("Comandos", "Cancelar", null, Padrao.Select(a => a.Nome).ToArray());
                novo = Padrao.FirstOrDefault(a => a.Nome == nome);
                if (novo is null) return;
                break;
            case "Abrir aplicativo":
                var apps = await App.Conexao.ListarAppsAsync();
                if (apps.Count == 0) { _status.Text = "Selecione aplicativos na aba Apps."; return; }
                var nomes = apps.Select((a, i) => $"{i + 1}. {a.Nome}").ToArray();
                var app = Array.IndexOf(nomes, await DisplayActionSheet("Aplicativo do PC", "Cancelar", null, nomes));
                if (app < 0) return;
                novo = new(apps[app].Nome, "↗", "abrir_app", apps[app].Id, apps[app].EhJogo);
                break;
            case "Definir volume":
                var texto = await DisplayPromptAsync("Volume do PC", "Digite de 0 a 100.", "Salvar", "Cancelar", keyboard: Keyboard.Numeric);
                if (texto is null) return;
                if (!int.TryParse(texto, out var volume) || volume is < 0 or > 100)
                { _status.Text = "Digite um número de 0 a 100."; return; }
                novo = new($"Volume {volume}%", "♪", "volume", volume.ToString());
                break;
            case "Renomear e trocar ícone":
                if (novo is null) return;
                var rotulo = await DisplayPromptAsync("Nome do botão", "Digite um nome curto.", "Avançar", "Cancelar", maxLength: 28, initialValue: novo.Nome);
                if (string.IsNullOrWhiteSpace(rotulo)) return;
                var icone = await DisplayPromptAsync("Ícone do botão", "Digite um símbolo ou emoji.", "Salvar", "Cancelar", maxLength: 8, initialValue: novo.Icone);
                if (icone is null) return;
                novo = novo with { Nome = rotulo.Trim(), Icone = string.IsNullOrWhiteSpace(icone) ? novo.Icone : icone.Trim() };
                break;
            case "Mover":
                if (novo is null) return;
                var posicoes = Enumerable.Range(1, Total).Select(n => n.ToString()).ToArray();
                var destino = Array.IndexOf(posicoes, await DisplayActionSheet("Posição de destino", "Cancelar", null, posicoes));
                if (destino < 0 || destino == index) return;
                (_slots[index], _slots[destino]) = (_slots[destino], _slots[index]);
                Salvar();
                return;
            case "Remover":
                novo = null;
                break;
            default:
                return;
        }
        _slots[index] = novo;
        Salvar();
    }

    private void Salvar()
    {
        Preferences.Default.Set(_chave, JsonSerializer.Serialize(_slots));
        AtualizarBotoes();
    }

    private sealed record Atalho(string Nome, string Icone, string Comando, string? Argumento = null, bool Confirmar = false);
}
