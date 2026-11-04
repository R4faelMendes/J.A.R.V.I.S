using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Jarvis.Core;
using Jarvis.Desktop.Services;
using Microsoft.Win32;

namespace Jarvis.Desktop.Views.Pages;

public sealed class EmailsPage : PaginaBase
{
    private readonly IEmailService _email = ServicosAplicacao.Email;
    private readonly CancellationTokenSource _vida = new();
    private StackPanel _lista = null!, _detalhes = null!;
    private TextBlock _status = null!;
    private Button _conectar = null!, _sync = null!, _ler = null!, _sair = null!;
    private CaixaEntrada? _caixa;
    private bool _ocupado;
    public EmailsPage() : base("CONEXÕES  /  GOOGLE", "Sua caixa de entrada")
    {
        Loaded += async (_, _) => { if (_email.Conectado) await AcaoAsync(SincronizarAsync); };
        Unloaded += (_, _) => _vida.Cancel();
    }
    private static TextBlock Texto(string t, int size = 13, string color = "#8FA9BF") => new()
    { Text = t, FontSize = size, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)new BrushConverter().ConvertFromString(color)!, Margin = new Thickness(0, 0, 0, 10) };
    private Button Botao(string t, Func<Task> acao, bool primario = false)
    {
        var b = new Button { Content = t, Margin = new Thickness(0, 0, 10, 8), Style = (Style)Application.Current.Resources[primario ? "BotaoPrimario" : "BotaoSecundario"] };
        b.Click += async (_, _) => await AcaoAsync(acao); return b;
    }
    protected override void Montar(Grid container)
    {
        container.RowDefinitions.Add(new() { Height = GridLength.Auto });
        container.RowDefinitions.Add(new() { Height = GridLength.Auto });
        container.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        var topo = new StackPanel();
        topo.Children.Add(Texto("Seus e-mails são lidos no próprio computador. Você autoriza apenas a consulta; nenhuma mensagem é enviada ou apagada."));
        var botoes = new WrapPanel();
        _conectar = Botao("Conectar Gmail sem API", ConectarImapAsync, true);
        _sync = Botao("Sincronizar", SincronizarAsync);
        _ler = Botao("Ouvir resumo", () => { if (_caixa is not null) Falar(ResumoCaixaEntrada.ParaFala(_caixa)); return Task.CompletedTask; });
        _sair = Botao("Desconectar", async () => { try { await _email.DesconectarAsync(_vida.Token); } finally { Limpar(); } _status.Text = "Conta desconectada."; });
        botoes.Children.Add(_conectar); botoes.Children.Add(_sync); botoes.Children.Add(_ler); botoes.Children.Add(_sair); topo.Children.Add(botoes);
        var auto = new CheckBox { Content = "Ler os cinco assuntos mais recentes ao abrir o Jarvis", IsChecked = ConfiguracaoApp.LerEmailsAoAbrir, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 6, 0, 14) };
        auto.Click += (_, _) => { ConfiguracaoApp.LerEmailsAoAbrir = auto.IsChecked == true; ConfiguracaoApp.NotificarAlteracao(); };
        topo.Children.Add(auto);
        _status = Texto(_email.Conectado ? "Buscando sua caixa de entrada…" : "Entre com sua conta para começar. Nenhum e-mail foi carregado."); topo.Children.Add(_status);
        container.Children.Add(topo);
        var dev = new StackPanel { Margin = new Thickness(14) };
        dev.Children.Add(Texto("Configuração única do aplicativo", 16, Theme.Gelo));
        dev.Children.Add(Texto("1. No Google Cloud, crie um projeto e habilite Gmail API.\n2. Configure a tela de consentimento e adicione sua conta como usuário de teste.\n3. Crie um cliente OAuth do tipo Aplicativo para computador e baixe o JSON.\n4. Importe esse arquivo abaixo. Depois, basta usar Entrar com Google."));
        dev.Children.Add(Texto("Em modo de teste, o Google pode expirar a autorização após sete dias. A publicação/verificação do aplicativo é uma etapa separada do desenvolvedor.", 12));
        dev.Children.Add(Botao("Importar JSON e entrar com Google", async () =>
        {
            var dialog = new OpenFileDialog { Filter = "Cliente OAuth (*.json)|*.json", Title = "Configuração do aplicativo Google" };
            if (dialog.ShowDialog() != true) return;
            await _email.ConfigurarClienteAsync(dialog.FileName); Limpar();
            await ConectarAsync();
        }));
        var expander = new Expander { Header = "Configuração do desenvolvedor · uma única vez", IsExpanded = false, Content = dev, Foreground = (Brush)Application.Current.Resources["TextoNevoa"], Margin = new Thickness(0, 0, 0, 16) };
        Grid.SetRow(expander, 1); container.Children.Add(expander);
        var colunas = new Grid(); colunas.ColumnDefinitions.Add(new() { Width = new GridLength(1.1, GridUnitType.Star) }); colunas.ColumnDefinitions.Add(new() { Width = new GridLength(16) }); colunas.ColumnDefinitions.Add(new());
        _lista = new StackPanel(); _detalhes = new StackPanel();
        var painelLista = new Border { Style = (Style)Application.Current.Resources["PainelVidro"], Child = new ScrollViewer { Content = _lista, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
        var painelDetalhes = new Border { Style = (Style)Application.Current.Resources["PainelVidro"], Child = new ScrollViewer { Content = _detalhes, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
        Grid.SetColumn(painelDetalhes, 2); colunas.Children.Add(painelLista); colunas.Children.Add(painelDetalhes);
        Grid.SetRow(colunas, 2); container.Children.Add(colunas); Limpar(); AtualizarBotoes();
    }
    private Task ConectarImapAsync()
    {
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(Texto("Conectar Gmail sem API", 24, Theme.Gelo));
        panel.Children.Add(Texto("1. Ative a verificação em duas etapas na sua conta Google.\n2. Crie uma senha de app para Jarvis.\n3. Informe o e-mail e a senha de app de 16 letras abaixo. Não use sua senha normal."));
        panel.Children.Add(Texto("A conexão usa IMAP com TLS, sem projeto no Google Cloud. A senha fica protegida no Windows com DPAPI. O Jarvis consulta a caixa em modo somente leitura.", 12));
        var link = new Button { Content = "Abrir senhas de app do Google", Margin = new Thickness(0, 0, 0, 16) };
        link.Click += (_, _) => ServicosAplicacao.AcessoExterno.AbrirPagina("https://myaccount.google.com/apppasswords");
        panel.Children.Add(link);
        panel.Children.Add(Texto("Seu e-mail")); var email = new TextBox { MaxLength = 254, Margin = new Thickness(0, 0, 0, 14) }; panel.Children.Add(email);
        panel.Children.Add(Texto("Senha de app · 16 letras")); var password = new PasswordBox { Padding = new Thickness(12), FontSize = 16, Background = (Brush)Application.Current.Resources["PainelVidroEscuroFundo"], Foreground = (Brush)Application.Current.Resources["TextoGelo"], BorderBrush = (Brush)Application.Current.Resources["BordaLinha"] }; panel.Children.Add(password);
        var status = Texto("", 12); status.Margin = new Thickness(0, 12, 0, 12); panel.Children.Add(status);
        var save = new Button { Content = "Testar e conectar", Style = (Style)Application.Current.Resources["BotaoPrimario"] }; panel.Children.Add(save);
        var dialog = new Window { Title = "Gmail · conexão direta", Owner = Window.GetWindow(this), Width = 500, Height = 650, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = new ScrollViewer { Content = panel }, Background = (Brush)Application.Current.Resources["FundoAbismo"] };
        using var vida = CancellationTokenSource.CreateLinkedTokenSource(_vida.Token); dialog.Closed += (_, _) => { vida.Cancel(); password.Clear(); };
        save.Click += async (_, _) => {
            save.IsEnabled = false; status.Text = "Testando conexão segura com o Gmail…";
            try { var secret = password.Password; password.Clear(); await _email.ConectarSenhaAppAsync(email.Text, secret, vida.Token); secret = ""; dialog.DialogResult = true; }
            catch (OperationCanceledException) { }
            catch (Exception ex) { status.Text = ex.Message; }
            finally { save.IsEnabled = true; }
        };
        if (dialog.ShowDialog() == true) return SincronizarAsync();
        return Task.CompletedTask;
    }
    private async Task ConectarAsync()
    {
        _status.Text = "Configuração salva. Conclua o login na nova aba do navegador. Aguardando autorização…";
        await _email.ConectarAsync(_vida.Token);
        await SincronizarAsync();
    }
    private void Limpar()
    {
        _caixa = null; _lista.Children.Clear(); _detalhes.Children.Clear();
        _lista.Children.Add(Texto("Sua caixa de entrada aparecerá aqui.", 16, Theme.Gelo));
        _detalhes.Children.Add(Texto("Selecione um e-mail", 22, Theme.Gelo));
        _detalhes.Children.Add(Texto("Veja o remetente, o assunto e uma prévia. A leitura não altera o estado da mensagem no Gmail."));
    }
    private void AtualizarBotoes()
    {
        _conectar.IsEnabled = !_ocupado;
        _sync.IsEnabled = _sair.IsEnabled = !_ocupado && _email.Conectado;
        _ler.IsEnabled = !_ocupado && _caixa is not null;
    }
    private async Task AcaoAsync(Func<Task> acao)
    {
        if (_ocupado || _vida.IsCancellationRequested) return;
        _ocupado = true; AtualizarBotoes();
        try { await acao(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Limpar(); _status.Text = ex.Message; }
        finally { _ocupado = false; AtualizarBotoes(); }
    }
    private async Task SincronizarAsync()
    {
        _status.Text = "Sincronizando com o Gmail…";
        var caixa = await _email.SincronizarAsync(_vida.Token);
        _vida.Token.ThrowIfCancellationRequested();
        _caixa = caixa; _lista.Children.Clear();
        _status.Text = $"{caixa.Total} mensagens · {caixa.NaoLidos} não lidas · atualizado às {DateTime.Now:HH:mm}. Exibindo as {caixa.Mensagens.Count} mais recentes.";
        if (caixa.Mensagens.Count == 0) _lista.Children.Add(Texto("Sua caixa de entrada está vazia.", 18, Theme.Gelo));
        foreach (var m in caixa.Mensagens)
        {
            var s = new StackPanel();
            s.Children.Add(Texto((m.NaoLido ? "●  " : "") + m.Remetente, 12, m.NaoLido ? Theme.CianoHolo : Theme.Nevoa));
            s.Children.Add(Texto(string.IsNullOrWhiteSpace(m.Assunto) ? "Sem assunto" : m.Assunto, 15, Theme.Gelo));
            s.Children.Add(Texto(m.Data.ToLocalTime().ToString("dd/MM · HH:mm"), 11));
            var b = new Button { Content = s, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(14) };
            b.Click += (_, _) => Selecionar(m); _lista.Children.Add(b);
        }
        if (caixa.Mensagens.FirstOrDefault() is { } primeira) Selecionar(primeira);
        else _detalhes.Children.Clear();
    }
    private void Selecionar(EmailResumo m)
    {
        _detalhes.Children.Clear();
        _detalhes.Children.Add(Texto(m.NaoLido ? "NÃO LIDO" : "MENSAGEM", 11, Theme.CianoHolo));
        _detalhes.Children.Add(Texto(m.Assunto, 24, Theme.Gelo));
        _detalhes.Children.Add(Texto(m.Remetente + "\n" + m.Data.ToLocalTime().ToString("dd/MM/yyyy HH:mm")));
        _detalhes.Children.Add(Texto("PRÉVIA DO GMAIL", 11, Theme.CianoHolo));
        _detalhes.Children.Add(Texto(m.Previa, 15, Theme.Gelo));
        _detalhes.Children.Add(Botao("Ler esta prévia", () => { Falar($"De {m.Remetente}. {m.Assunto}. {m.Previa}"); return Task.CompletedTask; }));
    }
}
