using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Jarvis.Core;
namespace Jarvis.Desktop.Views;

public sealed class PainelAparelhoView : Border
{
    private readonly ICentralAparelhos _central;
    private readonly ComboBox _select = new() { MinWidth = 220, HorizontalAlignment = HorizontalAlignment.Left, DisplayMemberPath = "Nome" };
    private readonly TextBlock _info = Text("Abra Meu aparelho no Jarvis do Android atualizado para conectar.", 15);
    private readonly TextBlock _batteryValue = Text("—", 36), _batteryState = Text("Aguardando leitura do Android", 12), _storage = Text("—", 21), _storageSub = Text("Espaço livre do aparelho", 12), _network = Text("—", 21), _networkSub = Text("Conexão do aparelho", 12);
    private readonly TextBlock _online = Text("AGUARDANDO CELULAR", 11);
    private readonly TextBlock _controle = Text("Para usar mouse e teclado, ative Jarvis em Acessibilidade no Android uma vez.", 12);
    private ImagemAparelho? _frame;
    private readonly TextBlock _status = Text("Aguardando dados reais do aparelho.", 12);
    private readonly TextBlock _espelho = Text("No celular: Mais → Meu aparelho → Autorizar e mostrar tela no PC.", 13);
    private readonly Image _image = new() { Height = 440, Stretch = Stretch.Uniform, Visibility = Visibility.Collapsed };
    private readonly Button _find = Button("Encontrar celular"), _mute = Button("Parar som"), _stop = Button("Parar espelhamento"), _large = Button("Ampliar tela");
    private readonly ProgressBar _battery = new() { Height = 7, Maximum = 100, Margin = new Thickness(0, 12, 0, 0), Background = new SolidColorBrush(Color.FromRgb(22, 48, 65)), Foreground = Brushes.Turquoise };
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private string _ids = "";
    private DateTimeOffset _last;
    private string? _frameDevice;
    private Window? _viewer;
    private Image? _largeImage;
    private int _ticks;
    private static TextBlock Text(string value, double size) => new() { Text = value, FontSize = size, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(194, 220, 235)), Margin = new Thickness(0, 5, 0, 5) };
    private static Button Button(string title) => new() { Content = title, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(14, 8, 14, 8) };
    public PainelAparelhoView(ICentralAparelhos central)
    {
        _central = central; CornerRadius = new(16); Padding = new(20); Margin = new(0, 0, 0, 24);
        Background = new SolidColorBrush(Color.FromRgb(7, 25, 37)); BorderBrush = new SolidColorBrush(Color.FromRgb(35, 67, 89)); BorderThickness = new(1);
        var root = new StackPanel();
        var heading = new DockPanel(); DockPanel.SetDock(_online, Dock.Right); heading.Children.Add(_online);
        var name = new StackPanel { Orientation = Orientation.Horizontal }; name.Children.Add(Icones.Criar("smartphone", 24, Brushes.DeepSkyBlue)); var title = Text("Seu celular", 24); title.Margin = new Thickness(12, 0, 0, 0); name.Children.Add(title); heading.Children.Add(name); root.Children.Add(heading);
        _select.Margin = new Thickness(0, 14, 0, 5); root.Children.Add(_select); root.Children.Add(_info);
        var cards = new Grid { Margin = new Thickness(0, 14, 0, 14) }; for (var i = 0; i < 3; i++) cards.ColumnDefinitions.Add(new ColumnDefinition());
        void Card(int column, string icon, string label, TextBlock value, TextBlock detail, ProgressBar? progress = null)
        {
            var stack = new StackPanel(); var caption = new StackPanel { Orientation = Orientation.Horizontal }; caption.Children.Add(Icones.Criar(icon, 19, Brushes.DeepSkyBlue)); var text = Text(label, 12); text.Margin = new Thickness(9, 0, 0, 8); caption.Children.Add(text); stack.Children.Add(caption); stack.Children.Add(value); stack.Children.Add(detail); if (progress is not null) stack.Children.Add(progress);
            var card = new Border { Child = stack, Background = new SolidColorBrush(Color.FromRgb(4, 17, 28)), BorderBrush = new SolidColorBrush(Color.FromRgb(27, 51, 69)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(17), Margin = new Thickness(column == 0 ? 0 : 6, 0, column == 2 ? 0 : 6, 0) }; Grid.SetColumn(card, column); cards.Children.Add(card);
        }
        Card(0, "battery", "BATERIA", _batteryValue, _batteryState, _battery); Card(1, "hard-drive", "ARMAZENAMENTO", _storage, _storageSub); Card(2, "wifi", "CONEXÃO", _network, _networkSub); root.Children.Add(cards); root.Children.Add(_status);
        var actions = new WrapPanel { Margin = new(0, 12, 0, 8) }; actions.Children.Add(_find); actions.Children.Add(_mute); actions.Children.Add(_stop); actions.Children.Add(_large); root.Children.Add(actions);
        root.Children.Add(_espelho); root.Children.Add(_controle); root.Children.Add(Navegacao()); root.Children.Add(_image); Child = root; ConectarControle(_image);
        void Do(Action<string> action) { if (_select.SelectedItem is not AparelhoPainel a) return; try { action(a.Id); Atualizar(); } catch (Exception ex) { _status.Text = ex.Message; } }
        _find.Click += (_, _) => Do(_central.Encontrar); _mute.Click += (_, _) => Do(_central.Silenciar); _stop.Click += (_, _) => Do(_central.PararEspelho);
        _large.Click += (_, _) => Ampliar(); _select.SelectionChanged += (_, _) => { _viewer?.Close(); LimparQuadro(); Atualizar(); };
        _timer.Tick += (_, _) => { if (++_ticks % 5 == 0) Atualizar(); Quadro(); };
        Loaded += (_, _) => { Atualizar(); _timer.Start(); }; Unloaded += (_, _) => { _timer.Stop(); _viewer?.Close(); _viewer = null; _largeImage = null; LimparQuadro(); };
        Atualizar();
    }
    private void LimparQuadro() { _frame = null; _image.Source = null; _image.Visibility = Visibility.Collapsed; if (_largeImage is not null) _largeImage.Source = null; _last = default; _frameDevice = null; }
    private ContextoImagemCelular? Contexto()
    {
        var device = _central.Listar().FirstOrDefault(d => d.Id == (_select.SelectedItem as AparelhoPainel)?.Id);
        return device?.Sessao is { } session && _frame is { } frame ? new(device.Id, session, frame) : null;
    }
    private void ConectarControle(Image image) => ControleImagemCelular.Conectar(image, Contexto, input => { if (Contexto() is { } ctx) _central.Controlar(ctx.Id, input); }, text => _controle.Text = text);
    private UIElement Navegacao()
    {
        var panel = new WrapPanel();
        foreach (var (name, action) in new[] { ("← Voltar", "voltar"), ("⌂ Início", "inicio"), ("▣ Recentes", "recentes") })
        {
            var button = Button(name); button.Click += (_, _) => { if (Contexto() is not { } ctx) return; try { var f = ctx.Imagem; _central.Controlar(ctx.Id, new(ctx.Sessao, action, f.Largura, f.Altura, f.Rotacao)); } catch (Exception ex) { _controle.Text = ex.Message; } }; panel.Children.Add(button);
        }
        return panel;
    }
    private void Atualizar()
    {
        var devices = _central.Listar(); var signature = string.Join("|", devices.Select(a => a.Id + ":" + a.Nome));
        if (_ids != signature)
        {
            var old = (_select.SelectedItem as AparelhoPainel)?.Id; _ids = signature; _select.ItemsSource = devices;
            _select.SelectedItem = devices.FirstOrDefault(a => a.Id == old) ?? devices.FirstOrDefault();
        }
        var device = devices.FirstOrDefault(a => a.Id == (_select.SelectedItem as AparelhoPainel)?.Id);
        _select.Visibility = devices.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _find.IsEnabled = _mute.IsEnabled = device?.Online == true; _stop.IsEnabled = device?.Espelhando == true; _large.IsEnabled = device?.Espelhando == true;
        if (device is null)
        {
            _info.Text = "Abra Mais → Meu aparelho no Jarvis do Android atualizado."; _batteryValue.Text = _storage.Text = _network.Text = "—"; _battery.Value = 0; _batteryState.Text = "Aguardando leitura do Android"; _storageSub.Text = "Espaço livre do aparelho"; _networkSub.Text = "Conexão do aparelho"; _online.Text = "AGUARDANDO CELULAR"; return;
        }
        var d = device.Dados;
        string Gb(long? n) => n is { } bytes ? (bytes / 1073741824d).ToString("F1") + " GB" : "sem leitura";
        _battery.Value = d.Bateria ?? 0;
        _info.Text = $"{d.Modelo} · {d.Sistema}";
        _batteryValue.Text = d.Bateria is { } b ? b + "%" : "—"; _batteryState.Text = d.Bateria is null ? "Android não enviou a leitura" : (d.Carregando == true ? "Conectado ao carregador" : d.Carregando == false ? "Usando a bateria" : "Estado de carga indisponível");
        _battery.Foreground = d.Bateria <= 20 ? Brushes.Orange : Brushes.Turquoise;
        _storage.Text = Gb(d.ArmazenamentoLivre); _storageSub.Text = "livres de " + Gb(d.ArmazenamentoTotal); _network.Text = d.Conexao; _networkSub.Text = "Conexão direta com o Jarvis";
        _online.Text = device.Online ? "● ONLINE" : "● OFFLINE"; _online.Foreground = device.Online ? Brushes.Turquoise : Brushes.SlateGray;
        _info.Text += device.Online ? "" : " · última leitura " + device.Visto.ToLocalTime().ToString("HH:mm:ss");
        _status.Text = device.Localizacao;
        _controle.Text = !d.ControleSuportado ? "Este instalador permite visualizar a tela. O controle do celular pelo PC não está incluído porque exige Acessibilidade." : d.ControleAtivo ? device.Controle + " Clique na imagem para usar mouse e teclado." : "No Android: Mais → Meu aparelho → Configurar mouse e teclado. Ative Jarvis em Acessibilidade uma vez.";
        _espelho.Text = device.Espelhando ? "Espelhamento iniciado · aguardando quadros do celular." : "No celular: Mais → Meu aparelho → Autorizar e mostrar tela no PC.";
    }
    private void Quadro()
    {
        if (_select.SelectedItem is not AparelhoPainel a) return;
        try
        {
            var frame = _central.Quadro(a.Id);
            if (frame is null) { LimparQuadro(); return; }
            _espelho.Text = "Tela do celular ao vivo · visualização sem áudio. Use Parar espelhamento para encerrar.";
            if (_last == frame.Instante && _frameDevice == a.Id) return;
            using var stream = new MemoryStream(frame.Jpeg, false); var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = Math.Min(frame.Largura, 1280); image.StreamSource = stream; image.EndInit(); image.Freeze();
            _frame = frame; _image.Source = image; _image.Visibility = Visibility.Visible; if (_largeImage is not null) _largeImage.Source = image; _last = frame.Instante; _frameDevice = a.Id;
        }
        catch { LimparQuadro(); _espelho.Text = "Quadro indisponível. Reinicie o compartilhamento no telefone."; }
    }
    private void Ampliar()
    {
        if (_viewer is not null) { _viewer.Activate(); return; }
        var deviceId = (_select.SelectedItem as AparelhoPainel)?.Id;
        _largeImage = new Image { Stretch = Stretch.Uniform, Source = _image.Source };
        ConectarControle(_largeImage);
        var root = new DockPanel { Background = Brushes.Black }; var close = Button("Parar espelhamento"); close.HorizontalAlignment = HorizontalAlignment.Right;
        close.Click += (_, _) => { if (_select.SelectedItem is AparelhoPainel a) _central.PararEspelho(a.Id); _viewer?.Close(); }; DockPanel.SetDock(close, Dock.Top); root.Children.Add(close); root.Children.Add(_largeImage);
        var nav = Navegacao(); DockPanel.SetDock(nav, Dock.Bottom); root.Children.Remove(_largeImage); root.Children.Add(nav); root.Children.Add(_largeImage);
        _viewer = new Window { Title = "Jarvis · tela do celular", Width = 520, Height = 820, MinWidth = 320, MinHeight = 400, Content = root, Owner = Window.GetWindow(this), Background = Brushes.Black };
        _viewer.Closed += (_, _) => { _viewer = null; _largeImage = null; if (deviceId is not null && _central.Listar().Any(a => a.Id == deviceId && a.Espelhando)) _central.PararEspelho(deviceId); }; _viewer.Show();
    }
}

