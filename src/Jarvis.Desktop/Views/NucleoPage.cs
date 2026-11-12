using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Jarvis.Core;
using Jarvis.Desktop.Services;
namespace Jarvis.Desktop.Views.Pages;
public sealed class NucleoPage : PaginaBase
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _busy, _saiu; private TextBlock _song = null!, _artist = null!, _device = null!, _connection = null!, _tempo = null!; private TelemetryGauge _cpuGauge = null!, _ramGauge = null!; private TelemetryTrend _cpuTrend = null!, _ramTrend = null!; private Image _cover = null!; private ProgressBar _posicao = null!; private StackPanel _emailRows = null!; private Button _play = null!; private bool _tocando; private DateTime _emailSync = DateTime.MinValue;
    public Action? AtivarEscutaCallback { get; set; }
    public NucleoPage() : base("", "", false) { _timer.Tick += async (_, _) => await AtualizarAsync(); Loaded += async (_, _) => { _saiu = false; _timer.Start(); await AtualizarAsync(); }; Unloaded += (_, _) => { _saiu = true; _timer.Stop(); }; }
    private static Brush B(string cor) => (Brush)new BrushConverter().ConvertFromString(cor)!;
    private static TextBlock T(string text, double size = 12, string cor = Theme.Nevoa) => new() { Text = text, FontSize = size, Foreground = B(cor), TextWrapping = TextWrapping.Wrap, FontFamily = (FontFamily)Application.Current.Resources["FonteTexto"] };
    private static Border Card(UIElement content) {
        var card = new Border { Child = content, Padding = new Thickness(16), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5) };
        card.SetResourceReference(Border.BackgroundProperty, "PainelVidroEscuroFundo"); card.SetResourceReference(Border.BorderBrushProperty, "BordaLinha"); return card;
    }
    private Button Go(string title, PageType page) { var b = new Button { Content = title, HorizontalContentAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0), Padding = new Thickness(12, 12, 10, 12), Background = B("#181818") }; b.Click += (_, _) => Navegar(page); return b; }
    protected override void Montar(Grid container)
    {
        var root = new Grid { MinHeight = 560 };
        root.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 220 });
        root.ColumnDefinitions.Add(new() { Width = new GridLength(1.65, GridUnitType.Star), MinWidth = 340 });
        root.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 220 });
        var left = new StackPanel { Margin = new Thickness(0, 0, 18, 0) };
        TextBlock Caption(string text) { var t = T(text, 10, Theme.Nevoa); t.FontFamily = new FontFamily("Consolas"); t.Margin = new Thickness(0, 0, 0, 12); return t; }
        left.Children.Add(Caption("01 / CANAL DE COMUNICAÇÃO"));
        var channel = new StackPanel(); channel.Children.Add(Caption("JARVIS  /  ASSISTENTE PESSOAL"));
        var greet = T($"{(DateTime.Now.Hour < 12 ? "Bom dia" : DateTime.Now.Hour < 18 ? "Boa tarde" : "Boa noite")}, {ConfiguracaoApp.NomeUsuario}.", 23, Theme.Gelo); greet.FontWeight = FontWeights.Light; channel.Children.Add(greet);
        var hint = T("Às suas ordens. O que vamos fazer?", 13); hint.Margin = new Thickness(0, 15, 0, 14); channel.Children.Add(hint);
        var talk = new Button { Content = "◉  Ativar microfone", HorizontalContentAlignment = HorizontalAlignment.Center, Padding = new Thickness(12), Margin = new Thickness(0, 6, 0, 0) }; talk.Click += (_, _) => AtivarEscutaCallback?.Invoke(); channel.Children.Add(talk); left.Children.Add(Card(channel));
        var mail = new StackPanel();
        var inbox = Go("", PageType.Emails);
        var mailHeader = new Grid(); mailHeader.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) }); mailHeader.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        mailHeader.Children.Add(T("E-MAILS", 11, Theme.Gelo)); var openInbox = T("VER CAIXA ↗", 10, Theme.CianoHolo); Grid.SetColumn(openInbox, 1); mailHeader.Children.Add(openInbox);
        inbox.Content = mailHeader; inbox.HorizontalContentAlignment = HorizontalAlignment.Stretch; inbox.VerticalContentAlignment = VerticalAlignment.Center; inbox.Background = Brushes.Transparent; inbox.BorderThickness = new Thickness(0); inbox.Margin = new Thickness(0, 0, 0, 10); inbox.Padding = new Thickness(0, 6, 0, 6); mail.Children.Add(inbox);
        _emailRows = new StackPanel(); _emailRows.Children.Add(T(ServicosAplicacao.Email.Conectado ? "Consultando sua caixa de entrada…" : "Conecte sua conta para ver seus e-mails.", 12)); mail.Children.Add(_emailRows); var ec = Card(mail); ec.Margin = new Thickness(0, 16, 0, 0); ec.HorizontalAlignment = HorizontalAlignment.Stretch; left.Children.Add(ec);
        var quick = new StackPanel { Margin = new Thickness(0, 14, 0, 0) }; quick.Children.Add(Caption("ACESSO RÁPIDO")); quick.Children.Add(Go("Aplicativos autorizados    ›", PageType.Apps)); quick.Children.Add(Go("Janelas e grades    ›", PageType.Apps)); quick.Children.Add(Go("Minha agenda    ›", PageType.Agenda)); quick.Children.Add(Go("Resumo do dia    ›", PageType.ResumoDia)); left.Children.Add(quick);
        quick.Children.Add(Go("Modo de jogo    ›", PageType.ModoJogo)); root.Children.Add(left);
        var center = new Grid { Margin = new Thickness(4, 0, 4, 0) }; center.RowDefinitions.Add(new() { Height = GridLength.Auto }); center.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star), MinHeight = 300 }); center.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var coreHead = new Grid(); coreHead.ColumnDefinitions.Add(new()); coreHead.ColumnDefinitions.Add(new()); coreHead.Children.Add(Caption("ÁUDIO / JARVIS")); var coreLabel = Caption("VOZ LOCAL · PT-BR"); coreLabel.HorizontalAlignment = HorizontalAlignment.Right; Grid.SetColumn(coreLabel, 1); coreHead.Children.Add(coreLabel); center.Children.Add(coreHead);
        var reactor = new AudioOrbView { MinHeight = 310, MaxHeight = 470, Margin = new Thickness(-6, 0, -6, 0) }; reactor.SetBinding(AudioOrbView.EstadoProperty, new Binding("EstadoReator")); reactor.SetBinding(AudioOrbView.NivelAudioProperty, new Binding("NivelAudio")); Grid.SetRow(reactor, 1); center.Children.Add(reactor);
        var coreFooter = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 20) }; var caption = Caption("SISTEMAS INTEGRADOS  /  JARVIS"); caption.HorizontalAlignment = HorizontalAlignment.Center; coreFooter.Children.Add(caption); var state = T("Pronto para o próximo comando.", 23, Theme.Gelo); state.FontWeight = FontWeights.Light; state.HorizontalAlignment = HorizontalAlignment.Center; coreFooter.Children.Add(state);
        var voice = new Button { Content = "◉   Diga Jarvis", Style = (Style)Application.Current.Resources["BotaoPrimario"], Margin = new Thickness(0, 17, 0, 0), Padding = new Thickness(24, 11, 24, 11) }; voice.Click += (_, _) => AtivarEscutaCallback?.Invoke(); coreFooter.Children.Add(voice); Grid.SetRow(coreFooter, 2); center.Children.Add(coreFooter); Grid.SetColumn(center, 1); root.Children.Add(center);
        var side = new StackPanel { Margin = new Thickness(18, 0, 0, 0) }; Grid.SetColumn(side, 2); side.Children.Add(Caption("02 / ESTADO DO SISTEMA"));
        var sys = new StackPanel(); sys.Children.Add(T("▱   Seu computador", 13, Theme.Gelo));
        Grid Metric(string label, TelemetryGauge gauge, TelemetryTrend trend) { var row = new Grid { Margin = new Thickness(0, 18, 0, 0) }; row.ColumnDefinitions.Add(new() { Width = new GridLength(42) }); row.ColumnDefinitions.Add(new() { Width = new GridLength(100) }); row.ColumnDefinitions.Add(new()); var text = T(label, 11, Theme.Gelo); text.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(text); gauge.Width = 96; gauge.Height = 42; Grid.SetColumn(gauge, 1); row.Children.Add(gauge); trend.Height = 32; trend.Margin = new Thickness(12, 0, 0, 0); Grid.SetColumn(trend, 2); row.Children.Add(trend); return row; }
        _cpuGauge = new TelemetryGauge(); _ramGauge = new TelemetryGauge { Accent = B(Theme.Gelo) }; _cpuTrend = new TelemetryTrend(); _ramTrend = new TelemetryTrend { Accent = B(Theme.Gelo) }; sys.Children.Add(Metric("CPU", _cpuGauge, _cpuTrend)); sys.Children.Add(Metric("RAM", _ramGauge, _ramTrend)); var detail = Go("Desempenho  ›", PageType.Desempenho); detail.FontSize = 10; detail.Margin = new Thickness(0, 12, 0, 0); sys.Children.Add(detail); side.Children.Add(Card(sys));
        var phone = new StackPanel(); phone.Children.Add(T("▯   Seu celular", 13, Theme.Gelo)); _device = T("Nenhum celular conectado", 13, Theme.Gelo); _device.Margin = new Thickness(0, 16, 0, 4); phone.Children.Add(_device); _connection = T("Conecte o app pelo QR code", 10); phone.Children.Add(_connection); phone.Children.Add(Go("Parear aplicativo  ›", PageType.Celular)); var phoneCard = Card(phone); phoneCard.Margin = new Thickness(0, 12, 0, 0); side.Children.Add(phoneCard); root.Children.Add(side);
        var music = new StackPanel(); music.Children.Add(T("♫   Música", 13, Theme.Gelo)); var track = new Grid { Margin = new Thickness(0, 16, 0, 12) }; track.ColumnDefinitions.Add(new() { Width = new GridLength(64) }); track.ColumnDefinitions.Add(new()); _cover = new Image { Stretch = Stretch.UniformToFill }; var album = new Grid(); album.Children.Add(new Border { Background = B("#0D3348"), CornerRadius = new CornerRadius(8) }); album.Children.Add(new TextBlock { Text = "♫", FontSize = 26, Foreground = B(Theme.CianoHolo), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }); album.Children.Add(_cover); album.Width = 54; album.Height = 54; track.Children.Add(album); var names = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; _song = T("Spotify", 15, Theme.Gelo); _artist = T("Conecte seu player para ouvir.", 11); names.Children.Add(_song); names.Children.Add(_artist); Grid.SetColumn(names, 1); track.Children.Add(names); music.Children.Add(track); _posicao = new ProgressBar { Height = 3, Maximum = 100, Margin = new Thickness(0, 0, 0, 5) }; _tempo = T("", 10); music.Children.Add(_posicao); music.Children.Add(_tempo); var controls = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        foreach (var (label, cmd) in new[] { ("|‹", ComandoMidia.Anterior), ("▶", ComandoMidia.Alternar), ("›|", ComandoMidia.Proxima) }) { var b = new Button { Content = T(label,16,Theme.CianoHolo), FontSize = 16, Width = 48, Height = 40, Padding = new Thickness(0), Margin = new Thickness(0, 0, 12, 0), Foreground = B(Theme.CianoHolo), Background = Brushes.Transparent }; b.Click += async (_, _) => await MidiaAsync(cmd); controls.Children.Add(b); if (cmd == ComandoMidia.Alternar) _play = b; }
        music.Children.Add(controls); var mc = Card(music); mc.Margin = new Thickness(0, 16, 0, 0); side.Children.Add(mc);
        void Layout(double width) {
            var narrow = width < 940;
            root.RowDefinitions.Clear();
            for (var i = 0; i < 3; i++) root.ColumnDefinitions[i].MinWidth = narrow ? 0 : i == 1 ? 340 : 220;
            if (narrow) {
                root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new() { Height = GridLength.Auto });
                Grid.SetColumn(center, 0); Grid.SetRow(center, 0); Grid.SetColumnSpan(center, 3);
                Grid.SetColumn(left, 0); Grid.SetRow(left, 1); Grid.SetColumnSpan(left, 3);
                Grid.SetColumn(side, 0); Grid.SetRow(side, 2); Grid.SetColumnSpan(side, 3);
                left.Margin = new Thickness(0, 12, 0, 0); side.Margin = new Thickness(0, 12, 0, 0);
            } else {
                Grid.SetColumn(center, 1); Grid.SetRow(center, 0); Grid.SetColumnSpan(center, 1);
                Grid.SetColumn(left, 0); Grid.SetRow(left, 0); Grid.SetColumnSpan(left, 1);
                Grid.SetColumn(side, 2); Grid.SetRow(side, 0); Grid.SetColumnSpan(side, 1);
                left.Margin = new Thickness(0, 0, 18, 0); side.Margin = new Thickness(18, 0, 0, 0);
            }
        }
        bool? wasNarrow = null;
        container.SizeChanged += (_, e) => { var narrow = e.NewSize.Width < 940; if (wasNarrow != narrow) { wasNarrow = narrow; Layout(e.NewSize.Width); } };
        container.Children.Add(new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
    }
    private async Task MidiaAsync(ComandoMidia cmd) { try { if (!await ServicosAplicacao.Midia.ExecutarAsync(cmd)) MostrarToast("Abra o Spotify e inicie uma música no PC."); await AtualizarAsync(); } catch (Exception ex) { MostrarToast(ex.Message); } }
    private async Task AtualizarAsync()
    {
        if (_busy || _saiu) return; _busy = true; try
        {
            var t = await ServicosAplicacao.Telemetria.ObterAsync(); if (_saiu) return; _cpuGauge.Value = t.Cpu ?? double.NaN; _ramGauge.Value = t.Memoria ?? double.NaN; _cpuTrend.Add(t.Cpu); _ramTrend.Add(t.Memoria);
            var device = ServicosAplicacao.EstadoCelular; _device.Text = device?.Nome ?? "Nenhum celular conectado"; _connection.Text = device is null ? "Conecte o app pelo QR code" : device.Conexao + (device.Bateria.HasValue ? $" · {device.Bateria}% de bateria" : " · app autenticado");
            var m = await ServicosAplicacao.Midia.ObterEstadoAsync(); if (_saiu) return; _song.Text = m.Disponivel ? m.Titulo : "Spotify"; _artist.Text = m.Disponivel ? m.Artista : "Abra o player e inicie uma música."; _tocando = m.Tocando; _play.Content = T(_tocando ? "Ⅱ" : "▶",16,Theme.CianoHolo);
            if (m.Capa is { Length: > 0 }) { using var stream = new System.IO.MemoryStream(m.Capa); var image = new System.Windows.Media.Imaging.BitmapImage(); image.BeginInit(); image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze(); _cover.Source = image; } else _cover.Source = null;
            _posicao.Value = m.DuracaoSegundos > 0 ? Math.Clamp(m.PosicaoSegundos / m.DuracaoSegundos * 100, 0, 100) : 0; _tempo.Text = m.DuracaoSegundos > 0 ? $"{TimeSpan.FromSeconds(m.PosicaoSegundos):m\\:ss}   /   {TimeSpan.FromSeconds(m.DuracaoSegundos):m\\:ss}" : "";
            if (ServicosAplicacao.Email.Conectado && DateTime.UtcNow - _emailSync > TimeSpan.FromMinutes(1)) { _emailSync = DateTime.UtcNow; var inbox = await ServicosAplicacao.Email.SincronizarAsync(); if (_saiu) return; _emailRows.Children.Clear(); foreach (var email in inbox.Mensagens.Take(3)) { var row = new StackPanel { Margin = new Thickness(0, 7, 0, 7) }; var sender = T(email.Remetente, 11, Theme.CianoHolo); sender.TextTrimming = TextTrimming.CharacterEllipsis; sender.TextWrapping = TextWrapping.NoWrap; row.Children.Add(sender); var subject = T(email.Assunto, 11); subject.TextWrapping = TextWrapping.NoWrap; subject.TextTrimming = TextTrimming.CharacterEllipsis; row.Children.Add(subject); _emailRows.Children.Add(row); } if (inbox.Mensagens.Count == 0) _emailRows.Children.Add(T("Sua caixa de entrada está vazia.")); }
        }
        catch { }
        finally { _busy = false; }
    }
}

