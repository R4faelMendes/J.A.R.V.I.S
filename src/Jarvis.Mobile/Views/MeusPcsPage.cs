using Jarvis.Mobile.Services;

namespace Jarvis.Mobile.Views;

public sealed class MeusPcsPage : ContentPage
{
    private const string PrimeiraImportacao = "jarvis_importacao_local_concluida";
    private readonly VerticalStackLayout _lista = new() { Spacing = 12 };
    private readonly Label _sessao = new();
    private readonly Label _status = new() { TextColor = Colors.White };
    private readonly Button _google = new();
    private readonly Button _publicar = new() { Text = "Salvar PCs deste aparelho na conta" };
    private readonly Button _cancelar = new() { Text = "Cancelar login", IsVisible = false };
    private readonly VerticalStackLayout _acoes = new() { Spacing = 8 };
    private CancellationTokenSource? _login;
    private bool _ocupado;

    public MeusPcsPage()
    {
        Title = "Meus PCs";
        Shell.SetNavBarIsVisible(this, false);
        _google.Clicked += async (_, _) => await ExecutarAsync(AlternarContaAsync);
        _publicar.Clicked += async (_, _) => await ExecutarAsync(PublicarAsync);
        _cancelar.Clicked += (_, _) => _login?.Cancel();
        var parear = Botao("Adicionar computador", () => Shell.Current.GoToAsync("parear"));
        parear.Style = (Style)Application.Current.Resources["Primario"];
        _acoes.Add(parear);
        _acoes.Add(_google);
        _acoes.Add(_publicar);
        _acoes.Add(Botao("Atualizar lista", () => ExecutarAsync(CarregarAsync)));
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20, 16, 20, 28),
                Spacing = 16,
                Children = { new Label { Text = "Computadores", Style = (Style)Application.Current.Resources["Titulo"] },
                    _sessao, _acoes, _cancelar, _status, _lista }
            }
        };
        SemanticProperties.SetDescription(_status, "Status da conta e dos computadores");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ExecutarAsync(CarregarAsync);
    }

    private async Task ExecutarAsync(Func<Task> acao)
    {
        if (_ocupado) return;
        _ocupado = true;
        _acoes.IsEnabled = _lista.IsEnabled = false;
        _status.Text = "";
        try { await acao(); }
        catch (OperationCanceledException) { _status.Text = "Login encerrado. Você pode tentar novamente."; }
        catch (Exception e) { _status.Text = e.Message; }
        finally { _ocupado = false; _acoes.IsEnabled = _lista.IsEnabled = true; }
    }

    private async Task CarregarAsync()
    {
        var pcsLocais = await App.Conexao.ListarPcsAsync();
        var sessao = await App.Conta.SessaoAsync();
        _sessao.Text = sessao is not null ? sessao.Email ?? "Conta Google conectada" :
            ContaJarvis.Disponivel ? "Entre com o Google para sincronizar sua lista." : "Pareamento local disponível. Conta online ainda indisponível.";
        _google.IsVisible = ContaJarvis.Disponivel;
        _google.Text = sessao is not null ? "Sair da conta" : "Entrar com Google";
        _publicar.IsVisible = sessao is not null && pcsLocais.Length > 0;
        IReadOnlyList<PcConta> nuvem = [];
        if (sessao is not null)
        {
            try { nuvem = await App.Conta.ListarAsync(default); }
            catch (Exception e) { _status.Text = e.Message; }
        }

        _lista.Clear();
        foreach (var pc in pcsLocais)
            _lista.Add(Cartao(pc.Id, nuvem.FirstOrDefault(n => n.Id == pc.Id)?.Name ?? pc.Nome, pc, nuvem.Any(n => n.Id == pc.Id)));
        foreach (var pc in nuvem.Where(p => pcsLocais.All(l => l.Id != p.Id)))
            _lista.Add(Cartao(pc.Id, pc.Name, null, true));
        if (_lista.Count == 0) _lista.Add(new Label { Text = "Adicione um computador por QR ou PIN para começar." });
    }

    private Border Cartao(string id, string nome, PcSalvo? local, bool naConta)
    {
        var ativo = App.Conexao.PcAtivo == id;
        var favorito = App.Conexao.Favorito == id;
        var detalhes = local is null ? "Salvo na conta · pareamento necessário neste aparelho" :
            ativo && App.Conexao.Conectado ? "Conectado" :
            local.UltimaResposta is { } ultima ? $"Última resposta em {ultima.LocalDateTime:g}" : "Sem resposta registrada";
        var estado = new Label { Text = favorito ? detalhes + " · favorito" : detalhes };
        var corpo = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = nome, FontFamily = "ChakraPetchSemiBold", FontSize = 20 },
                estado
            }
        };
        if (local is null)
            corpo.Add(Botao("Parear neste aparelho", () => Shell.Current.GoToAsync("parear")));
        else
        {
            if (!ativo || !App.Conexao.Conectado)
                corpo.Add(Botao(ativo ? "Reconectar" : "Usar este PC", () => ExecutarAsync(async () =>
                {
                    if (ativo) await App.Conexao.VerificarAutomaticamenteAsync();
                    else await App.Conexao.SelecionarPcAsync(id);
                    await CarregarAsync();
                })));
            if (!ativo)
                corpo.Add(Botao("Verificar conexão", () => ExecutarAsync(async () =>
                {
                    var online = await App.Conexao.VerificarPcSalvoAsync(id);
                    var situacao = online ? "Acessível agora" : "Sem resposta agora";
                    estado.Text = favorito ? situacao + " · favorito" : situacao;
                })));
            if (local.PodeAcordar && !(ativo && App.Conexao.Conectado))
                corpo.Add(Botao("Acordar PC", () => ExecutarAsync(async () =>
                {
                    await App.Conexao.AcordarSalvoAsync(id);
                    _status.Text = "Pacote Wake-on-LAN enviado.";
                })));
            corpo.Add(Botao(favorito ? "Desmarcar favorito" : "Definir como favorito", () => ExecutarAsync(async () =>
            {
                App.Conexao.DefinirFavorito(favorito ? null : id);
                await CarregarAsync();
            })));
        }
        corpo.Add(Botao("Renomear", () => ExecutarAsync(async () =>
        {
            var novo = (await DisplayPromptAsync("Renomear computador", "Digite um nome para este PC.", "Salvar", "Cancelar", maxLength: 100, initialValue: nome))?.Trim();
            if (string.IsNullOrWhiteSpace(novo)) return;
            if (naConta) await App.Conta.SalvarAsync(new(id, novo));
            await App.Conexao.RenomearPcAsync(id, novo);
            await CarregarAsync();
        })));
        corpo.Add(Botao("Remover", () => ExecutarAsync(async () =>
        {
            if (!await DisplayAlert("Remover computador", $"Remover {nome} da lista e apagar o pareamento salvo neste aparelho?", "Remover", "Cancelar")) return;
            if (naConta) await App.Conta.RemoverAsync(id, default);
            await App.Conexao.RemoverPcAsync(id);
            await CarregarAsync();
        })));
        return new Border { Content = corpo, Style = (Style)Application.Current.Resources["Painel"] };
    }

    private async Task AlternarContaAsync()
    {
        if (await App.Conta.SessaoAsync() is not null) await App.Conta.SairAsync(default);
        else
        {
            using var login = new CancellationTokenSource();
            _login = login;
            _cancelar.IsVisible = true;
            _status.Text = "Conclua o login no navegador e volte ao Jarvis.";
            try { await App.Conta.EntrarAsync(login.Token); }
            finally { _login = null; _cancelar.IsVisible = false; }

            if (!Preferences.Default.Get(PrimeiraImportacao, false))
            {
                // Uma troca de conta não republica os pareamentos locais automaticamente.
                Preferences.Default.Set(PrimeiraImportacao, true);
                try { await EnviarLocaisAsync(); }
                catch (Exception)
                {
                    await CarregarAsync();
                    _status.Text = "Use Salvar PCs deste aparelho na conta para tentar sincronizar novamente.";
                    return;
                }
            }
        }
        _status.Text = "";
        await CarregarAsync();
    }

    private async Task PublicarAsync()
    {
        await EnviarLocaisAsync();
        await CarregarAsync();
    }

    private async Task EnviarLocaisAsync()
    {
        var remotos = await App.Conta.ListarAsync(default);
        foreach (var pc in await App.Conexao.ListarPcsAsync())
            if (remotos.All(p => p.Id != pc.Id)) await App.Conta.SalvarAsync(new(pc.Id, pc.Nome));
    }

    private static Button Botao(string texto, Func<Task> executar)
    {
        var button = new Button { Text = texto };
        button.Clicked += async (_, _) => await executar();
        return button;
    }
}
