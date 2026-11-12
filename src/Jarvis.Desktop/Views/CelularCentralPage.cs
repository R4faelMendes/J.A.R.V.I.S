using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Jarvis.Desktop.Services;
namespace Jarvis.Desktop.Views.Pages;
public sealed class CelularCentralPage : PaginaBase
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly CancellationTokenSource _life = new();
    private Image _qr = null!;
    private TextBlock _pin = null!, _state = null!, _address = null!, _expiry = null!, _connected = null!;
    private DateTimeOffset _expires;
    private DateTimeOffset _retry;
    private bool _busy;
    public CelularCentralPage() : base("APLICATIVO ANDROID", "Seu Jarvis, também no celular")
    {
        Loaded += async (_, _) => { _timer.Start(); await GerarAsync(); };
        Unloaded += (_, _) => { _timer.Stop(); _life.Cancel(); };
        _timer.Tick += async (_, _) =>
        {
            _connected.Text = ServicosAplicacao.ConexaoCelular.CelularOnline is { } name ? name + " conectado" : "Aguardando o aplicativo do celular";
            var left = _expires - DateTimeOffset.UtcNow;
            _expiry.Text = left > TimeSpan.Zero ? $"Código válido por {left.Minutes:00}:{left.Seconds:00}. Renovação automática." : "Aguardando código de pareamento.";
            if (left < TimeSpan.FromSeconds(25) && DateTimeOffset.UtcNow >= _retry) await GerarAsync();
        };
    }
    protected override void Montar(Grid container)
    {
        var stack = new StackPanel();
        stack.Children.Add(new Jarvis.Desktop.Views.PainelAparelhoView(ServicosAplicacao.Aparelhos));
        stack.Children.Add(DiagnosticoPage.Label("1 · INSTALAR NO CELULAR SEM USB", "#00D800"));
        var install = ServicosAplicacao.ConexaoCelular.ObterInstalacao();
        var downloadQr = new Image { Width = 180, Height = 180, HorizontalAlignment = HorizontalAlignment.Left };
        var chooseNetwork = new ComboBox { ItemsSource = install.Alternativa is null ? new[] { "Mesmo Wi-Fi do PC" } : new[] { "Mesmo Wi-Fi do PC", "Conexão direta" }, SelectedIndex = 0, Width = 250, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 10) };
        var downloadUrl = DiagnosticoPage.Label(install.Url);
        string CurrentDownload() => chooseNetwork.SelectedIndex == 1 ? install.Alternativa! : install.Url;
        void UpdateDownload() { downloadUrl.Text = CurrentDownload(); var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; using var png = new MemoryStream(ServicosAplicacao.ConexaoCelular.ObterQrInstalacao(CurrentDownload())); bitmap.StreamSource = png; bitmap.EndInit(); bitmap.Freeze(); downloadQr.Source = bitmap; }
        chooseNetwork.SelectionChanged += (_, _) => UpdateDownload(); UpdateDownload();
        stack.Children.Add(DiagnosticoPage.Label("Leia este QR com a câmera do celular, toque em Baixar Jarvis e abra o APK. Depois, use o QR de pareamento abaixo dentro do app."));
        stack.Children.Add(chooseNetwork); stack.Children.Add(downloadQr); stack.Children.Add(downloadUrl);
        var copyLink = new Button { Content = "Copiar link para baixar o app", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 20) }; copyLink.Click += (_, _) => { Clipboard.SetText(CurrentDownload()); MostrarToast("Link copiado. Pode abrir no celular sem cabo USB."); }; stack.Children.Add(copyLink);
        stack.Children.Add(DiagnosticoPage.Label("ACESSO DIRETO PELA INTERNET", "#00D800"));
        var remote = DiagnosticoPage.Label(ServicosAplicacao.AcessoExterno.Endereco ?? "Nenhum IPv6 público detectado. Você pode informar um domínio/IP público e configurar a porta HTTPS no roteador."); stack.Children.Add(remote);
        var direct = new TextBox { Text = ServicosAplicacao.AcessoExterno.Endereco ?? "", MaxLength = 250, Margin = new Thickness(0, 8, 0, 8) }; stack.Children.Add(direct);
        var saveRemote = new Button { Content = "Salvar endereço direto", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 10) }; saveRemote.Click += (_, _) => { try { ServicosAplicacao.AcessoExterno.Salvar(direct.Text); remote.Text = "Endereço salvo. O app pareado aprende este endereço ao conectar no Wi-Fi. Teste depois pelos dados móveis."; } catch (Exception ex) { MostrarToast(ex.Message); } }; stack.Children.Add(saveRemote);
        var copyRemote = new Button { Content = "Copiar endereço direto", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) }; copyRemote.Click += (_, _) => { if (ServicosAplicacao.AcessoExterno.Endereco is { } endpoint) { Clipboard.SetText(endpoint); MostrarToast("Endereço copiado para Conexão direta no celular."); } }; stack.Children.Add(copyRemote);
        stack.Children.Add(DiagnosticoPage.Label("Sem aplicativo de VPN. IPv6 exige IPv6 também na rede do celular e liberação da porta HTTPS 5189 no roteador/firewall. IPv4 exige IP público e redirecionamento dessa porta; CGNAT bloqueia conexões de entrada. O endereço pode mudar conforme o provedor."));
        var telaEstado = DiagnosticoPage.Label("Celulares pareados podem visualizar e controlar o PC ao iniciar Tela ao vivo. Feche a visualização no app para parar."); stack.Children.Add(telaEstado);
        _timer.Tick += (_, _) => { var ultima = ServicosAplicacao.Tela.UltimaVisualizacao; telaEstado.Text = ultima.HasValue && DateTimeOffset.UtcNow - ultima.Value < TimeSpan.FromSeconds(3) ? "TRANSMITINDO · celular pareado conectado à tela do PC." : "Abra Tela ao vivo no app pareado. Visualização e toque já estão disponíveis."; };
        _connected = DiagnosticoPage.Label("Aguardando o aplicativo do celular", "#00D800"); stack.Children.Add(_connected);
        stack.Children.Add(DiagnosticoPage.Label("2 · CONECTAR O APP AO PC", "#00D800"));
        stack.Children.Add(DiagnosticoPage.Label("Após instalar pelo link acima, abra o Jarvis no Android e toque em Fotografar QR code para ler o código abaixo. O pareamento registra os endereços Wi-Fi e internet direta para as próximas conexões automáticas."));
        var pair = new Grid(); pair.ColumnDefinitions.Add(new() { Width = new GridLength(300) }); pair.ColumnDefinitions.Add(new());
        _qr = new Image { Width = 280, Height = 280, Stretch = Stretch.Uniform }; RenderOptions.SetBitmapScalingMode(_qr, BitmapScalingMode.NearestNeighbor);
        pair.Children.Add(new Border { Background = Brushes.White, CornerRadius = new CornerRadius(5), Padding = new Thickness(10), Child = _qr });
        var details = new StackPanel { Margin = new Thickness(28, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        details.Children.Add(DiagnosticoPage.Label("PAREAMENTO DIRETO NO APP")); _pin = DiagnosticoPage.Label("••• •••", "#00D800"); _pin.FontSize = 42; details.Children.Add(_pin);
        _address = DiagnosticoPage.Label("Conectando ao servidor local…"); details.Children.Add(_address); _expiry = DiagnosticoPage.Label("", "#F4B544"); details.Children.Add(_expiry);
        var renew = new Button { Content = "Gerar novo código", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 0), Style = (Style)Application.Current.Resources["BotaoPrimario"] }; renew.Click += async (_, _) => await GerarAsync(); details.Children.Add(renew);
        Grid.SetColumn(details, 1); pair.Children.Add(details); stack.Children.Add(CriarCardVidro(conteudo: pair));
        _state = DiagnosticoPage.Label("Abra o Jarvis no telefone para conectar."); stack.Children.Add(_state);
        stack.Children.Add(DiagnosticoPage.Label("No mesmo Wi-Fi, o app encontra este computador automaticamente. Confirme o PIN uma vez; o pareamento salvo permite reconectar nas próximas vezes. Fora dessa rede, use o endereço HTTPS direto em Conexão direta no app. Redes de convidados podem bloquear a conexão local."));
        var test = new Button { Content = "Verificar servidor e conexão", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 16, 0, 10) };
        test.Click += async (_, _) => { test.IsEnabled = false; try { var items = await ServicosAplicacao.Diagnostico.VerificarAsync(_life.Token); _state.Text = string.Join("\n", items.Where(i => i.Nome is "Servidor local" or "Dados protegidos").Select(i => (i.Ok ? "✓ " : "Atenção: ") + i.Nome + " — " + i.Detalhe)); } catch (OperationCanceledException) { } finally { test.IsEnabled = true; } }; stack.Children.Add(test);
        stack.Children.Add(DiagnosticoPage.Label("Ligar PC usa Wake-on-LAN. O app salva automaticamente MAC e broadcast no pareamento; a BIOS, a placa de rede e a alimentação do PC precisam permitir o despertar."));
        var wake = new Button { Content = "Verificar suporte para ligar meu PC", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 10) }; var wakeStatus = DiagnosticoPage.Label("");
        wake.Click += async (_, _) => { wake.IsEnabled = false; try { var items = await ServicosAplicacao.Diagnostico.VerificarAsync(_life.Token); wakeStatus.Text = string.Join("\n\n", items.Where(i => i.Nome.StartsWith("Wake-on-LAN")).Select(i => (i.Ok ? "✓ " : "• ") + i.Nome + "\n" + i.Detalhe)); } catch (OperationCanceledException) { } finally { wake.IsEnabled = true; } }; stack.Children.Add(wake); stack.Children.Add(wakeStatus);
        container.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
    private async Task GerarAsync()
    {
        if (_busy || _life.IsCancellationRequested) return; _busy = true; _retry = DateTimeOffset.UtcNow.AddSeconds(15);
        try
        {
            var pair = await ServicosAplicacao.ConexaoCelular.CriarPareamentoAsync(_life.Token);
            if (_life.IsCancellationRequested) return;
            using var stream = new MemoryStream(pair.Png); var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze(); _qr.Source = image;
            _pin.Text = pair.Codigo[..3] + " " + pair.Codigo[3..]; _address.Text = $"IP: {pair.Ip}  ·  Conexão segura: {pair.Porta}"; _expires = pair.Expira; _state.Text = "QR pronto. Leia no app Jarvis do Android.";
        }
        catch (OperationCanceledException) { }
        catch (Exception) { _state.Text = "Servidor ainda não respondeu. Tentarei novamente automaticamente; confira se o Jarvis está iniciado e as portas 5188/5189 estão livres."; }
        finally { _busy = false; }
    }
}


