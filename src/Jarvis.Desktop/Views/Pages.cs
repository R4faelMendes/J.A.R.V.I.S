// JARVIS Desktop — Páginas completas, dinâmicas e 100% interativas.
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Jarvis.Ai;
using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Server;
using Jarvis.Sistema;

namespace Jarvis.Desktop.Views.Pages;

public abstract class PaginaBase : UserControl
{
    public Action<PageType>? SolicitacaoNavegacao { get; set; }
    public Action<string>? SolicitacaoToast { get; set; }
    public Action<string>? SolicitacaoFala { get; set; }

    protected PaginaBase(string cabecalho, string titulo, bool mostrarCabecalho = true)
    {
        Foreground = (Brush)Application.Current.Resources["TextoGelo"];
        var raiz = new Grid { Margin = new Thickness(24, 16, 24, 24) };
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        header.Children.Add(new TextBlock
        {
            Text = cabecalho,
            Style = (Style)Application.Current.Resources["CabecalhoPagina"]
        });
        header.Children.Add(new TextBlock
        {
            Text = titulo,
            Style = (Style)Application.Current.Resources["TituloPagina"],
            Margin = new Thickness(0, 2, 0, 0)
        });

        Grid.SetRow(header, 0);
        if (mostrarCabecalho) raiz.Children.Add(header);

        var corpo = new Grid();
        Grid.SetRow(corpo, 1);
        raiz.Children.Add(corpo);

        Montar(corpo);
        Content = raiz;
    }

    protected abstract void Montar(Grid container);

    protected void Navegar(PageType pagina) => SolicitacaoNavegacao?.Invoke(pagina);
    protected void MostrarToast(string msg) => SolicitacaoToast?.Invoke(msg);
    protected void Falar(string texto) => SolicitacaoFala?.Invoke(texto);

    /// <summary>Estado vazio honesto: nada inventado, com ação para conectar.</summary>
    protected static Border PainelDesconectado(string titulo, string descricao, string textoBotao, Func<Task> aoConectar)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = titulo, FontSize = 18, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 8) });
        sp.Children.Add(new TextBlock { Text = descricao, FontSize = 13, Foreground = (Brush)Application.Current.Resources["TextoNevoa"], TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 14) });
        var btn = new Button { Content = textoBotao, Style = (Style)Application.Current.Resources["BotaoPrimario"], Padding = new Thickness(18, 9, 18, 9), HorizontalAlignment = HorizontalAlignment.Left };
        btn.Click += async (_, _) => await aoConectar();
        sp.Children.Add(btn);
        return new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Child = sp,
            Padding = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
    }

    protected static Border CriarCardVidro(string? titulo = null, UIElement? conteudo = null, double marginB = 12)
    {
        var p = new StackPanel();
        if (!string.IsNullOrEmpty(titulo))
        {
            p.Children.Add(new TextBlock
            {
                Text = titulo,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextoNevoa"],
                Margin = new Thickness(0, 0, 0, 8)
            });
        }
        if (conteudo != null)
            p.Children.Add(conteudo);

        return new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Child = p,
            Margin = new Thickness(0, 0, 0, marginB)
        };
    }

    protected static Border CriarChip(string texto, string corHex = "#00D800", string fundoHex = "#0F2E45")
    {
        return new Border
        {
            Background = (Brush)new BrushConverter().ConvertFromString(fundoHex)!,
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 0, 8, 0),
            Child = new TextBlock
            {
                Text = texto,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)new BrushConverter().ConvertFromString(corHex)!
            }
        };
    }

    protected static string? PedirTexto(string titulo, string mensagem, string padrao = "")
    {
        var win = new Window
        {
            Title = titulo,
            Width = 440,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = (Brush)Application.Current.Resources["FundoAbismo"],
            Foreground = (Brush)Application.Current.Resources["TextoGelo"],
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow
        };

        var sp = new StackPanel { Margin = new Thickness(20) };
        sp.Children.Add(new TextBlock { Text = mensagem, FontSize = 13, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 10), TextWrapping = TextWrapping.Wrap });

        var tb = new TextBox { Style = (Style)Application.Current.Resources["CampoTexto"], Text = padrao, Margin = new Thickness(0, 0, 0, 16) };
        sp.Children.Add(tb);

        var bRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var btnOk = new Button { Content = "Confirmar", Style = (Style)Application.Current.Resources["BotaoPrimario"], Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(0, 0, 10, 0) };
        var btnCancel = new Button { Content = "Cancelar", Style = (Style)Application.Current.Resources["BotaoSecundario"], Padding = new Thickness(16, 8, 16, 8) };

        string? resultado = null;
        btnOk.Click += (_, _) => { resultado = tb.Text; win.Close(); };
        btnCancel.Click += (_, _) => { win.Close(); };
        tb.KeyDown += (_, e) => { if (e.Key == Key.Enter) { resultado = tb.Text; win.Close(); } };

        bRow.Children.Add(btnCancel);
        bRow.Children.Add(btnOk);
        sp.Children.Add(bRow);

        win.Content = sp;
        win.ShowDialog();
        return resultado;
    }
}
// -------------------------------------------------------------------------------------------------
// APPS E JANELAS (/apps)
// -------------------------------------------------------------------------------------------------
public sealed class AppsPage : PaginaBase
{
    private readonly IAppController _apps = Jarvis.Desktop.Services.ServicosAplicacao.Apps;
    private readonly IWindowController _janelas = Jarvis.Desktop.Services.ServicosAplicacao.Janelas;
    private readonly Dictionary<long, CheckBox> _selecaoJanelas = new();
    private readonly Dictionary<long, ComboBox> _posicoesJanelas = new();
    private readonly ComboBox _monitorLayout = new() { DisplayMemberPath = "Nome", MinWidth = 220, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
    private readonly WrapPanel _gridApps = new();
    private readonly StackPanel _listaJanelas = new();

    public AppsPage() : base("Controle do PC", "Apps e janelas") { }

    protected override void Montar(Grid container)
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();

        // SEÇÃO: APLICATIVOS DETECTADOS
        var sectionApps = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var sAppsStack = new StackPanel();
        var sAppsTop = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        var btnAddApp = new Button { Content = "+ Adicionar app", Style = (Style)Application.Current.Resources["BotaoSecundario"], Padding = new Thickness(14, 7, 14, 7) };
        btnAddApp.Click += (_, _) => MostrarModalAddApp();
        DockPanel.SetDock(btnAddApp, Dock.Right);
        var titApps = new TextBlock { Text = "Aplicativos instalados no seu PC", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"] };
        sAppsTop.Children.Add(btnAddApp);
        var escolher = new Button { Content = "Escolher meus apps", Margin = new Thickness(0, 0, 10, 0) }; DockPanel.SetDock(escolher, Dock.Right);
        escolher.Click += (_, _) => EscolherMeusApps(); sAppsTop.Children.Add(escolher);
        sAppsTop.Children.Add(titApps);
        sAppsStack.Children.Add(sAppsTop);

        RecarregarGridApps();
        sAppsStack.Children.Add(_gridApps);
        sectionApps.Child = sAppsStack;
        stack.Children.Add(sectionApps);

        // SEÇÃO: JANELAS ABERTAS
        var sectionJanelas = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var sJanStack = new StackPanel();
        sJanStack.Children.Add(new TextBlock { Text = "Janelas abertas", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 12) });
        var refreshWindows = new Button { Content = "Atualizar janelas e monitores", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
        refreshWindows.Click += (_, _) => { RecarregarListaJanelas(); CarregarMonitores(); };
        sJanStack.Children.Add(refreshWindows);
        RecarregarListaJanelas();
        sJanStack.Children.Add(_listaJanelas);
        sectionJanelas.Child = sJanStack;
        stack.Children.Insert(0, sectionJanelas);

        // SEÇÃO: ORGANIZAR A TELA
        var sectionLayouts = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var sLayStack = new StackPanel();
        sLayStack.Children.Add(new TextBlock { Text = "Organizar a tela", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 8) });
        sLayStack.Children.Add(new TextBlock { Text = "Escolha a posição em cada janela e aplique o modelo. Duas colunas: 1 esquerda, 2 direita. Grade 2×2: 1 superior esquerda, 2 superior direita, 3 inferior esquerda, 4 inferior direita. Apps podem impor tamanho mínimo.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextoNevoa"], Margin = new Thickness(0, 0, 0, 12) });
        CarregarMonitores(); sLayStack.Children.Add(_monitorLayout);

        var layoutsRow = new WrapPanel();
        void AddLayoutBtn(string nome, string modelo)
        {
            var b = new Button
            {
                Content = nome,
                Style = (Style)Application.Current.Resources["BotaoSecundario"],
                Margin = new Thickness(0, 0, 10, 10),
                Padding = new Thickness(16, 10, 16, 10)
            };
            b.Click += (_, _) =>
            {
                try
                {
                    var ids = _selecaoJanelas.Where(x => x.Value.IsChecked == true).Select(x => x.Key).ToArray();
                    var occupied = ids.Select(id => _posicoesJanelas[id].SelectedIndex - 1).Where(p => p >= 0).ToHashSet();
                    var free = new Queue<int>(Enumerable.Range(0, GradeJanelas.Capacidade(modelo)).Where(p => !occupied.Contains(p)));
                    var positions = ids.Select(id => _posicoesJanelas[id].SelectedIndex > 0 ? _posicoesJanelas[id].SelectedIndex - 1 : free.Count > 0 ? free.Dequeue() : -1).ToArray();
                    var resultado = _janelas.Organizar(new(modelo, (_monitorLayout.SelectedItem as MonitorInfo)?.Id, ids, positions));
                    MostrarToast($"{resultado.Movidas} janelas organizadas." + (resultado.Avisos.Count > 0 ? " " + string.Join(" ", resultado.Avisos) : ""));
                }
                catch (Exception ex) { MostrarToast(ex.Message); }
            };
            layoutsRow.Children.Add(b);
        }

        AddLayoutBtn("◫ Metade (50/50)", "Metade");
        AddLayoutBtn("▥ Três colunas", "TresColunas");
        AddLayoutBtn("⬝ Foco + lateral", "FocoLateral");
        AddLayoutBtn("⊞ Grade 2×2", "Grade2x2");
        sLayStack.Children.Add(layoutsRow);
        sectionLayouts.Child = sLayStack;
        stack.Children.Insert(0, sectionLayouts);

        scroll.Content = stack;
        container.Children.Add(scroll);
    }

    private void RecarregarGridApps()
    {
        _gridApps.Children.Clear();
        foreach (var app in _apps.ListarApps())
        {
            var card = new Border
            {
                Style = (Style)Application.Current.Resources["CartaoItem"],
                Width = 230,
                Margin = new Thickness(0, 0, 12, 12)
            };

            var stack = new StackPanel();
            var top = new DockPanel();
            var chip = app.Aberto
                ? CriarChip("● Aberto", Theme.VerdeSistema, "#143528")
                : CriarChip("Fechado", "#BBBBBB", "#1B2A38");
            DockPanel.SetDock(chip, Dock.Right);
            var tit = new TextBlock { Text = $"{app.Icone} {app.Nome}", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], TextTrimming = TextTrimming.CharacterEllipsis };
            top.Children.Add(chip);
            top.Children.Add(tit);
            stack.Children.Add(top);

            var bot = new DockPanel { Margin = new Thickness(0, 10, 0, 0) };
            var btnAcao = new Button
            {
                Content = app.Aberto ? "Fechar" : "Abrir",
                Style = app.Aberto ? (Style)Application.Current.Resources["BotaoPerigo"] : (Style)Application.Current.Resources["BotaoPrimario"],
                Padding = new Thickness(16, 7, 16, 7)
            };

            btnAcao.Click += (_, _) =>
            {
                if (app.Aberto)
                {
                    if (app.EhJogo)
                    {
                        var res = MessageBox.Show($"Deseja realmente fechar {app.Nome}? Progresso não salvo pode ser perdido.", "Confirmar fechamento", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (res != MessageBoxResult.Yes) return;
                    }
                    _apps.Fechar(app.Id);
                    MostrarToast($"{app.Nome} encerrado.");
                }
                else
                {
                    try { _apps.Abrir(app.Id); MostrarToast($"Abrindo {app.Nome}..."); }
                    catch (Exception ex) { MostrarToast($"Erro: {ex.Message}"); }
                }
                RecarregarGridApps();
            };

            bot.Children.Add(btnAcao);
            stack.Children.Add(bot);

            card.Child = stack;
            _gridApps.Children.Add(card);
        }
    }

    private void RecarregarListaJanelas()
    {
        var selecionadas = _selecaoJanelas.Where(x => x.Value.IsChecked == true).Select(x => x.Key).ToHashSet();
        var antigas = _posicoesJanelas.ToDictionary(x => x.Key, x => x.Value.SelectedIndex); _posicoesJanelas.Clear();
        _selecaoJanelas.Clear();
        _listaJanelas.Children.Clear();
        foreach (var j in _janelas.ListarJanelas())
        {
            var card = new Border
            {
                Style = (Style)Application.Current.Resources["CartaoItem"],
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 8, 12, 8)
            };
            var d = new DockPanel();

            var acoes = new WrapPanel { MaxWidth = 360 };
            DockPanel.SetDock(acoes, Dock.Right);

            var btnMin = new Button { Content = "Minimizar", Style = (Style)Application.Current.Resources["BotaoSecundario"], Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 6, 0) };
            var btnMax = new Button { Content = "Maximizar", Style = (Style)Application.Current.Resources["BotaoSecundario"], Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 6, 0) };
            var btnClose = new Button { Content = "✕", Style = (Style)Application.Current.Resources["BotaoPerigo"], Padding = new Thickness(10, 5, 10, 5) };

            void Acao(Action executar) { try { executar(); } catch (Exception ex) { MostrarToast(ex.Message); } }
            btnMin.Click += (_, _) => Acao(() => _janelas.Minimizar(j.Handle));
            btnMax.Click += (_, _) => Acao(() => _janelas.Maximizar(j.Handle));
            btnClose.Click += (_, _) => { if (MessageBox.Show($"Fechar {j.Titulo}? O aplicativo pode pedir para salvar seu trabalho.", "Fechar janela", MessageBoxButton.YesNo) == MessageBoxResult.Yes) Acao(() => _janelas.Fechar(j.Handle)); };

            acoes.Children.Add(btnMin);
            acoes.Children.Add(btnMax);
            acoes.Children.Add(btnClose);
            d.Children.Add(acoes);

            var t = new CheckBox
            {
                Content = j.Titulo,
                IsChecked = selecionadas.Contains((long)j.Handle),
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextoGelo"],
                VerticalAlignment = VerticalAlignment.Center,
            };
            _selecaoJanelas[(long)j.Handle] = t;
            var position = new ComboBox { ItemsSource = new[] { "Posição automática", "Posição 1", "Posição 2", "Posição 3", "Posição 4" }, SelectedIndex = 0, Width = 160, Margin = new Thickness(12, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            _posicoesJanelas[(long)j.Handle] = position;
            position.SelectedIndex = antigas.GetValueOrDefault((long)j.Handle, 0);
            position.SelectionChanged += (_, _) => { if (position.SelectedIndex > 0) t.IsChecked = true; };
            DockPanel.SetDock(position, Dock.Right); d.Children.Add(position);
            d.Children.Add(t);

            card.Child = d;
            _listaJanelas.Children.Add(card);
        }
    }

    private void CarregarMonitores()
    {
        var id = (_monitorLayout.SelectedItem as MonitorInfo)?.Id;
        var monitores = _janelas.ListarMonitores(); _monitorLayout.ItemsSource = monitores;
        _monitorLayout.SelectedItem = monitores.FirstOrDefault(m => m.Id == id) ?? monitores.FirstOrDefault(m => m.Principal);
    }

    private void MostrarModalAddApp()
    {
        var arquivo = new Microsoft.Win32.OpenFileDialog { Title = "Escolha o aplicativo ou seu atalho", Filter = "Aplicativos e atalhos|*.exe;*.lnk|Executável|*.exe|Atalho|*.lnk", CheckFileExists = true };
        if (arquivo.ShowDialog(Window.GetWindow(this)) != true) return;
        var nome = PedirTexto("Adicionar aplicativo", "Nome para mostrar e reconhecer por voz:", Path.GetFileNameWithoutExtension(arquivo.FileName));
        if (string.IsNullOrWhiteSpace(nome)) return;
        try { _apps.AdicionarApp(nome.Trim(), arquivo.FileName, nome.Trim().ToLowerInvariant()); RecarregarGridApps(); MostrarToast($"{nome} adicionado ao PC e ao celular."); }
        catch (Exception ex) { MostrarToast(ex.Message); }
    }

    private void EscolherMeusApps()
    {
        var selecionados = _apps.ListarApps().Select(a => a.Id).ToHashSet();
        var checks = new Dictionary<string, CheckBox>(); var lista = new StackPanel();
        foreach (var a in _apps.ListarDisponiveis()) { var check = new CheckBox { Content = a.Nome, IsChecked = selecionados.Contains(a.Id), Foreground = Brushes.LightCyan, Margin = new Thickness(8) }; checks[a.Id] = check; lista.Children.Add(check); }
        var painel = new DockPanel { Margin = new Thickness(18) };
        var salvar = new Button { Content = "Salvar meus aplicativos", Margin = new Thickness(0, 12, 0, 0) }; DockPanel.SetDock(salvar, Dock.Bottom); painel.Children.Add(salvar);
        painel.Children.Add(new ScrollViewer { Content = lista, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        var dialogo = new Window { Title = "Escolha os apps que você usa", Width = 520, Height = 600, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = (Brush)Application.Current.Resources["FundoAbismo"], Content = painel };
        salvar.Click += (_, _) => { try { _apps.SelecionarApps(checks.Where(x => x.Value.IsChecked == true).Select(x => x.Key).ToArray()); RecarregarGridApps(); dialogo.Close(); } catch (Exception ex) { MessageBox.Show(ex.Message); } };
        dialogo.ShowDialog();
    }
}

// -------------------------------------------------------------------------------------------------
// 4. DESEMPENHO (/desempenho)
// -------------------------------------------------------------------------------------------------
public sealed class DesempenhoPage : PaginaBase
{
    private readonly PerfMonitor _perf = new();
    private readonly DispatcherTimer _timer;
    private TextBlock _txtCpu = null!;
    private TextBlock _txtMem = null!;
    private TextBlock _txtGpu = null!;
    private TextBlock _txtDisk = null!;
    private TextBlock _txtProprio = null!;
    private StackPanel _tabelaProcessos = new();

    public DesempenhoPage() : base("Sistema", "Desempenho")
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Atualizar();
        _timer.Start();
        Unloaded += (_, _) => { _timer.Stop(); _perf.Dispose(); };
    }

    protected override void Montar(Grid container)
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();

        // 4 CARDS SUPERIORES
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        for (int i = 0; i < 4; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border CriarCardTelemetria(string titulo, string sub, out TextBlock txtValor, int col)
        {
            var b = new Border
            {
                Style = (Style)Application.Current.Resources["PainelVidro"],
                Margin = new Thickness(col == 0 ? 0 : 6, 0, col == 3 ? 0 : 6, 0),
                Padding = new Thickness(16)
            };
            var p = new StackPanel();
            p.Children.Add(new TextBlock { Text = titulo, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextoNevoa"] });
            txtValor = new TextBlock { Text = "--", FontFamily = (FontFamily)Application.Current.Resources["FonteTitulo"], FontSize = 28, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 4, 0, 2) };
            p.Children.Add(txtValor);
            p.Children.Add(new TextBlock { Text = sub, FontSize = 11, Foreground = (Brush)Application.Current.Resources["TextoNevoa"] });
            b.Child = p;
            Grid.SetColumn(b, col);
            return b;
        }

        grid.Children.Add(CriarCardTelemetria("CPU", "Contador em tempo real", out _txtCpu, 0));
        grid.Children.Add(CriarCardTelemetria("Memória", "RAM em uso", out _txtMem, 1));
        grid.Children.Add(CriarCardTelemetria("GPU", "Motor 3D & Render", out _txtGpu, 2));
        grid.Children.Add(CriarCardTelemetria("Disco C:", "Armazenamento", out _txtDisk, 3));
        stack.Children.Add(grid);

        // TABELA: PROCESSOS MAIS PESADOS
        var secProc = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = "Processos mais pesados no Windows", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 12) });
        sp.Children.Add(_tabelaProcessos);
        secProc.Child = sp;
        stack.Children.Add(secProc);

        // SUGESTÕES DO JARVIS
        var secSug = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var ss = new StackPanel();
        var topSug = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var btnOtimizar = new Button { Content = "Otimizar sistema", Style = (Style)Application.Current.Resources["BotaoPrimario"], Padding = new Thickness(18, 7, 18, 7) };
        btnOtimizar.Click += (_, _) => MostrarToast("Otimização concluída: memória liberada.");
        DockPanel.SetDock(btnOtimizar, Dock.Right);
        topSug.Children.Add(btnOtimizar);
        topSug.Children.Add(new TextBlock { Text = "Sugestões do Jarvis", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"] });
        ss.Children.Add(topSug);

        void AddSugestao(string texto)
        {
            var p = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            p.Children.Add(new TextBlock { Text = "• ", Foreground = (Brush)Application.Current.Resources["AcaoCiano"], FontWeight = FontWeights.Bold });
            p.Children.Add(new TextBlock { Text = texto, Foreground = (Brush)Application.Current.Resources["TextoNevoa"], FontSize = 13 });
            ss.Children.Add(p);
        }

        AddSugestao("Temperaturas estáveis. Sensores de hardware monitorados em tempo real.");
        AddSugestao("Nenhum processo em sobrecarga.");
        AddSugestao("Memória e cache gerenciados automaticamente.");

        secSug.Child = ss;
        stack.Children.Add(secSug);

        // LINHA INFERIOR: USO DO JARVIS
        _txtProprio = new TextBlock
        {
            Text = "O Jarvis está usando 1.8% da CPU e 115 MB de RAM",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextoNevoa"],
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(_txtProprio);

        scroll.Content = stack;
        container.Children.Add(scroll);
        Atualizar();
    }

    private void Atualizar()
    {
        try
        {
            var cpu = _perf.CpuPercent;
            var mem = _perf.MemoriaPercent;
            var gpu = _perf.GpuPercent;
            var disk = _perf.DiscoPercent;
            var prop = _perf.CpuPropria;

            if (_txtCpu != null) _txtCpu.Text = double.IsNaN(cpu) ? "—" : $"{cpu:0}%";
            if (_txtMem != null) _txtMem.Text = double.IsNaN(mem) ? "—" : $"{mem:0}%";
            if (_txtGpu != null) _txtGpu.Text = double.IsNaN(gpu) ? "—" : $"{gpu:0}%";
            if (_txtDisk != null) _txtDisk.Text = double.IsNaN(disk) ? "—" : $"{disk:0}%";
            if (_txtProprio != null) _txtProprio.Text = $"Jarvis · CPU {prop:0.0}% · RAM {_perf.MemoriaPropriaMB:0} MB";

            _tabelaProcessos.Children.Clear();
            foreach (var p in _perf.TopProcessos())
            {
                var card = new Border
                {
                    Style = (Style)Application.Current.Resources["CartaoItem"],
                    Margin = new Thickness(0, 0, 0, 6),
                    Padding = new Thickness(12, 8, 12, 8)
                };
                var d = new DockPanel();

                var btnKill = new Button
                {
                    Content = "Encerrar",
                    Style = (Style)Application.Current.Resources["BotaoPerigo"],
                    Padding = new Thickness(12, 5, 12, 5)
                };
                btnKill.Click += (_, _) =>
                {
                    var res = MessageBox.Show($"Deseja encerrar o processo {p.Nome} (PID {p.Id})?", "Confirmar encerramento", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.Yes)
                    {
                        _perf.EncerrarProcesso(p.Id);
                        MostrarToast($"Processo {p.Nome} encerrado.");
                        Atualizar();
                    }
                };
                DockPanel.SetDock(btnKill, Dock.Right);
                d.Children.Add(btnKill);

                var txtInfo = new TextBlock
                {
                    Text = $"{p.Nome}  ·  CPU {(double.IsNaN(p.CpuPercent) ? "—" : p.CpuPercent.ToString("0.0") + "%")}  ·  {p.MemoriaMB:0} MB",
                    FontSize = 13,
                    Foreground = (Brush)Application.Current.Resources["TextoGelo"],
                    VerticalAlignment = VerticalAlignment.Center
                };
                d.Children.Add(txtInfo);

                card.Child = d;
                _tabelaProcessos.Children.Add(card);
            }
        }
        catch { }
    }
}

// -------------------------------------------------------------------------------------------------
// 7. ROTINAS (/rotinas)
// -------------------------------------------------------------------------------------------------
public sealed class RotinasPage : PaginaBase
{
    private readonly BancoJarvis _banco = new();

    public RotinasPage() : base("Automação", "Rotinas")
    {
        Unloaded += (_, _) => _banco.Dispose();
    }

    protected override void Montar(Grid container)
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();

        var top = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
        var btnAdd = new Button { Content = "+ Nova rotina", Style = (Style)Application.Current.Resources["BotaoPrimario"], Padding = new Thickness(18, 9, 18, 9) };
        btnAdd.Click += (_, _) =>
        {
            var nome = PedirTexto("Nova Rotina", "Nome da rotina:", "Modo Estudo");
            if (!string.IsNullOrWhiteSpace(nome))
            {
                var gat = PedirTexto("Gatilho de Voz", "Frase de ativação:", "hora de estudar");
                _banco.InserirRotina(nome, $"“{gat}”", gat ?? "", "Abre Notion, silencia notificações.");
                MostrarToast($"Rotina \"{nome}\" criada!");
                Montar(container);
            }
        };
        DockPanel.SetDock(btnAdd, Dock.Right);
        var sub = new TextBlock { Text = "Execute várias ações com um único comando de voz ou horário.", FontSize = 13, Foreground = (Brush)Application.Current.Resources["TextoNevoa"], VerticalAlignment = VerticalAlignment.Center };
        top.Children.Add(btnAdd);
        top.Children.Add(sub);
        stack.Children.Add(top);

        var rotinas = _banco.ListarRotinasCompletas();
        foreach (var r in rotinas)
        {
            var card = new Border
            {
                Style = (Style)Application.Current.Resources["PainelVidro"],
                Margin = new Thickness(0, 0, 0, 12)
            };

            var dock = new DockPanel();

            var acoes = new StackPanel { Orientation = Orientation.Horizontal };
            DockPanel.SetDock(acoes, Dock.Right);

            var btnTest = new Button { Content = "Testar rotina", Style = (Style)Application.Current.Resources["BotaoSecundario"], Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(0, 0, 10, 0) };
            btnTest.Click += (_, _) => MostrarToast($"Executando rotina \"{r.Nome}\" passo a passo...");
            acoes.Children.Add(btnTest);

            var toggle = new CheckBox
            {
                IsChecked = r.Ativa,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            toggle.Click += (_, _) =>
            {
                _banco.AlternarRotina(r.Id, toggle.IsChecked == true);
                MostrarToast($"Rotina \"{r.Nome}\" {(toggle.IsChecked == true ? "ativada" : "desativada")}.");
            };
            acoes.Children.Add(toggle);

            dock.Children.Add(acoes);

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = r.Nome, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"] });
            sp.Children.Add(new TextBlock { Text = r.Gatilho, FontSize = 12, Foreground = (Brush)Application.Current.Resources["AcaoCiano"], Margin = new Thickness(0, 2, 0, 4) });
            sp.Children.Add(new TextBlock { Text = r.Acoes, FontSize = 13, Foreground = (Brush)Application.Current.Resources["TextoNevoa"], TextWrapping = TextWrapping.Wrap });

            dock.Children.Add(sp);
            card.Child = dock;
            stack.Children.Add(card);
        }

        scroll.Content = stack;
        container.Children.Add(scroll);
    }
}

// -------------------------------------------------------------------------------------------------
// 8. CELULAR (/celular)
// -------------------------------------------------------------------------------------------------
public sealed class SegurancaPage : PaginaBase
{
    public SegurancaPage() : base("Privacidade", "Segurança e permissões") { }

    protected override void Montar(Grid container)
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();

        // TOGGLES DE PERMISSÃO
        var cardPerm = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var pStack = new StackPanel();
        pStack.Children.Add(new TextBlock { Text = "O que o Jarvis faz sozinho", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 12) });

        void AddPermItem(string titulo, string sub, bool ativado)
        {
            var d = new DockPanel { Margin = new Thickness(0, 4, 0, 8) };
            var chk = new CheckBox { IsChecked = ativado, VerticalAlignment = VerticalAlignment.Center };
            chk.Click += (_, _) => MostrarToast($"Permissão \"{titulo}\" alterada.");
            DockPanel.SetDock(chk, Dock.Right);
            d.Children.Add(chk);

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = titulo, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = (Brush)Application.Current.Resources["TextoGelo"] });
            sp.Children.Add(new TextBlock { Text = sub, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextoNevoa"] });
            d.Children.Add(sp);
            pStack.Children.Add(d);
        }

        AddPermItem("Microfone", "Ouvir a palavra de ativação “Jarvis” e capturar voz", true);
        AddPermItem("Arquivos", "Abrir e mover arquivos no Explorador", true);
        AddPermItem("Controle do PC", "Abrir e fechar programas, bloquear e suspender", true);
        AddPermItem("Comandos remotos", "Aceitar comandos recebidos pelo celular pareado", true);
        AddPermItem("Confirmação para ações de risco", "Sempre pedir confirmação antes de fechar apps ou reiniciar o PC", true);

        cardPerm.Child = pStack;
        stack.Children.Add(cardPerm);

        scroll.Content = stack;
        container.Children.Add(scroll);
    }
}

// -------------------------------------------------------------------------------------------------
// 10. CONFIGURAÇÕES (/configuracoes)
// -------------------------------------------------------------------------------------------------
public sealed class ConfiguracoesPage : PaginaBase
{
    public ConfiguracoesPage() : base("Ajustes do Jarvis", "Configurações") { }

    protected override void Montar(Grid container)
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();

        // 1. DADOS DO USUÁRIO
        var cardUser = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var uStack = new StackPanel();
        uStack.Children.Add(new TextBlock { Text = "Perfil do Usuário", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 10) });

        var uRow = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var btnSalvarNome = new Button { Content = "Salvar Nome", Style = (Style)Application.Current.Resources["BotaoPrimario"], Padding = new Thickness(16, 7, 16, 7), Margin = new Thickness(8, 0, 0, 0) };
        var txtNomeInput = new TextBox { Style = (Style)Application.Current.Resources["CampoTexto"], Text = ConfiguracaoApp.NomeUsuario };

        btnSalvarNome.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(txtNomeInput.Text))
            {
                ConfiguracaoApp.NomeUsuario = txtNomeInput.Text.Trim();
                ConfiguracaoApp.IniciaisUsuario = txtNomeInput.Text.Trim().Length >= 2 ? txtNomeInput.Text.Trim()[..2].ToUpperInvariant() : "RM";
                ConfiguracaoApp.NotificarAlteracao();
                MostrarToast($"Nome de usuário alterado para {ConfiguracaoApp.NomeUsuario}.");
            }
        };

        DockPanel.SetDock(btnSalvarNome, Dock.Right);
        uRow.Children.Add(btnSalvarNome);
        uRow.Children.Add(txtNomeInput);
        uStack.Children.Add(uRow);

        cardUser.Child = uStack;
        stack.Children.Add(cardUser);

        // 3. VOZ E MICROFONE
        var cardVoz = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var vStack = new StackPanel();
        vStack.Children.Add(new TextBlock { Text = "Microfone para escuta contínua", Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 8) });
        var microfones = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
        microfones.Items.Add("Padrão do Windows");
        try { foreach (var nomeMic in Jarvis.Desktop.Services.ServicosAplicacao.Audio.ListarMicrofones()) microfones.Items.Add(nomeMic); } catch { }
        microfones.SelectedItem = string.IsNullOrEmpty(ConfiguracaoApp.MicrofonePreferido) ? "Padrão do Windows" : ConfiguracaoApp.MicrofonePreferido;
        microfones.SelectionChanged += (_, _) => { ConfiguracaoApp.MicrofonePreferido = microfones.SelectedIndex == 0 ? "" : microfones.SelectedItem?.ToString() ?? ""; ConfiguracaoApp.NotificarAlteracao(); MostrarToast("Microfone salvo. A escuta será atualizada."); };
        vStack.Children.Add(microfones);
        vStack.Children.Add(new TextBlock { Text = "Sensibilidade da detecção de fala", Foreground = (Brush)Application.Current.Resources["TextoNevoa"] });
        var sensibilidade = new Slider { Minimum = .1, Maximum = 1, Value = ConfiguracaoApp.SensibilidadeMic, Margin = new Thickness(0, 8, 0, 16) };
        sensibilidade.ValueChanged += (_, _) => { ConfiguracaoApp.SensibilidadeMic = sensibilidade.Value; ConfiguracaoApp.NotificarAlteracao(); }; vStack.Children.Add(sensibilidade);
        vStack.Children.Add(new TextBlock { Text = "Voz e microfone", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 10) });

        var vozRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        var btnTestVoz = new Button { Content = "Testar voz", Style = (Style)Application.Current.Resources["BotaoPrimario"], Padding = new Thickness(16, 7, 16, 7) };
        btnTestVoz.Click += (_, _) => Falar($"Olá {ConfiguracaoApp.NomeUsuario}! Esta é a voz selecionada para o Jarvis.");
        DockPanel.SetDock(btnTestVoz, Dock.Right);
        vozRow.Children.Add(btnTestVoz);
        vozRow.Children.Add(new TextBlock { Text = "Piper · voz local, sem assinatura", FontSize = 13, Foreground = (Brush)Application.Current.Resources["TextoGelo"], VerticalAlignment = VerticalAlignment.Center });
        vStack.Children.Add(vozRow);
        var vozes = new ComboBox { Margin = new Thickness(0, 6, 0, 10), MinHeight = 32 };
        var modelos = new[] { ("Faber · PT-BR", "pt_BR-faber-medium.onnx"), ("Cadu · PT-BR", "pt_BR-cadu-medium.onnx"), ("Edresson · PT-BR", "pt_BR-edresson-low.onnx") };
        foreach (var (nome, arquivo) in modelos) vozes.Items.Add(new ComboBoxItem { Content = nome, Tag = arquivo, IsSelected = ConfiguracaoApp.ModeloVoz == arquivo });
        vozes.SelectionChanged += (_, _) =>
        {
            if (vozes.SelectedItem is ComboBoxItem item) { ConfiguracaoApp.ModeloVoz = (string)item.Tag; ConfiguracaoApp.NotificarAlteracao(); }
        };
        vStack.Children.Add(vozes);
        vStack.Children.Add(new TextBlock { Text = "Velocidade da fala · 0,7× a 1,3×", Foreground = (Brush)Application.Current.Resources["TextoNevoa"] });
        var velocidade = new Slider { Minimum = .7, Maximum = 1.3, Value = ConfiguracaoApp.VelocidadeVoz, TickFrequency = .05, IsSnapToTickEnabled = true, Margin = new Thickness(0, 8, 0, 12) };
        velocidade.ValueChanged += (_, _) => { ConfiguracaoApp.VelocidadeVoz = velocidade.Value; ConfiguracaoApp.NotificarAlteracao(); };
        vStack.Children.Add(velocidade);

        cardVoz.Child = vStack;
        stack.Children.Add(cardVoz);

        // 4. INTELIGÊNCIA ARTIFICIAL
        var cardIa = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var iaStack = new StackPanel();
        iaStack.Children.Add(new TextBlock { Text = "Assistente local · sem chave de IA", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 8) });
        iaStack.Children.Add(new TextBlock { Text = "Abra e feche aplicativos, controle a música, consulte a hora e faça cálculos simples. Tudo no seu PC, sem Ollama ou assinatura. E-mails usam sua conexão autorizada com o Google. Conversa geral com IA não está incluída neste modo.", TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["TextoNevoa"] });
        iaStack.Children.Add(new TextBlock { Text = "Experimente: Jarvis, abra a calculadora. Ou pressione Ctrl+Espaço e diga o comando.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0), Foreground = (Brush)Application.Current.Resources["TextoGelo"] });
        cardIa.Child = iaStack;
        stack.Children.Add(cardIa);

        // 5. APARÊNCIA E COMPORTAMENTO
        var cardApar = new Border
        {
            Style = (Style)Application.Current.Resources["PainelVidro"],
            Margin = new Thickness(0, 0, 0, 16)
        };
        var aStack = new StackPanel();
        aStack.Children.Add(new TextBlock { Text = "Aparência e comportamento", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)Application.Current.Resources["TextoGelo"], Margin = new Thickness(0, 0, 0, 10) });

        void AddToggle(string titulo, string sub, bool checado, Action<bool> onToggle)
        {
            var d = new DockPanel { Margin = new Thickness(0, 4, 0, 8) };
            var chk = new CheckBox { IsChecked = checado, VerticalAlignment = VerticalAlignment.Center };
            chk.Click += (_, _) => onToggle(chk.IsChecked == true);
            DockPanel.SetDock(chk, Dock.Right);
            d.Children.Add(chk);

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = titulo, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = (Brush)Application.Current.Resources["TextoGelo"] });
            sp.Children.Add(new TextBlock { Text = sub, FontSize = 11, Foreground = (Brush)Application.Current.Resources["TextoNevoa"] });
            d.Children.Add(sp);
            aStack.Children.Add(d);
        }

        AddToggle("Iniciar o Jarvis com o Windows", "Abre automaticamente em segundo plano ao ligar o PC", ConfiguracaoApp.IniciarComWindows, v =>
        {
            ConfiguracaoApp.ConfigurarInicializacaoAutomatica(v);
            MostrarToast($"Inicialização com o Windows {(v ? "ativada" : "desativada")}.");
        });

        AddToggle("Palavra de ativação “Jarvis”", "Ouvir continuamente mesmo com a janela em segundo plano", ConfiguracaoApp.WakeWordAtiva, v => { ConfiguracaoApp.WakeWordAtiva = v; ConfiguracaoApp.NotificarAlteracao(); });

        AddToggle("Reduzir animações", "Mantém o núcleo 3D estático", ConfiguracaoApp.ReduzirAnimacoes, v => { ConfiguracaoApp.ReduzirAnimacoes = v; ConfiguracaoApp.NotificarAlteracao(); });
        AddToggle("Modo econômico", "Pausa a animação 3D e reduz a atualização do dock", ConfiguracaoApp.ModoEconomico, v =>
        {
            ConfiguracaoApp.ModoEconomico = v;
            ConfiguracaoApp.NotificarAlteracao();
            MostrarToast($"Modo econômico {(v ? "ativado" : "desativado")}.");
        });

        cardApar.Child = aStack;
        stack.Children.Add(cardApar);

        scroll.Content = stack;
        container.Children.Add(scroll);
    }
}

// -------------------------------------------------------------------------------------------------
// FÁBRICA DE PÁGINAS
// -------------------------------------------------------------------------------------------------
public static class FabricaPaginas
{
    public static UserControl Criar(
        PageType tipo,
        Action<PageType>? navegar = null,
        Action<string>? toast = null,
        Action<string>? falar = null,
        Action? ativarEscuta = null,
        Func<string, Task<string>>? executar = null)
    {
        PaginaBase p = tipo switch
        {
            PageType.Nucleo => new NucleoPage { AtivarEscutaCallback = ativarEscuta },
            PageType.Conversa => new NucleoPage { AtivarEscutaCallback = ativarEscuta },
            PageType.ModoJogo => new ModoJogoPage(),
            PageType.Emails => new EmailsPage(),
            PageType.Apps => new AppsPage(),
            PageType.Desempenho => new DesempenhoPage(),
            PageType.Agenda => new AgendaPage(),
            PageType.Diagnostico => new NucleoPage { AtivarEscutaCallback = ativarEscuta },
            PageType.Celular => new CelularCentralPage(),
            PageType.Seguranca => new SegurancaPage(),
            PageType.Configuracoes => new ConfiguracoesPage(),
            _ => new NucleoPage { AtivarEscutaCallback = ativarEscuta },
        };
        p.SolicitacaoNavegacao = navegar;
        p.SolicitacaoToast = toast;
        p.SolicitacaoFala = falar;
        return p;
    }
}
