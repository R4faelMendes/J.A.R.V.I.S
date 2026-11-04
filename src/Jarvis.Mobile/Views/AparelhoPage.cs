using Jarvis.Mobile.Services;
namespace Jarvis.Mobile.Views;
public sealed class AparelhoPage : ContentPage
{
    private readonly Label _dados = new() { FontSize = 15, LineHeight = 1.4 };
    private readonly Label _bateria = new() { FontSize = 38, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#4DE3A1") }, _carga = new() { FontSize = 12 }, _armazenamento = new() { FontSize = 20 }, _rede = new() { FontSize = 20 };
    private readonly ProgressBar _progresso = new() { ProgressColor = Color.FromArgb("#4DE3A1"), BackgroundColor = Color.FromArgb("#1B3A57") };
    private readonly Label _estado = new() { FontSize = 12, TextColor = Color.FromArgb("#4FD6FF") };
    private readonly Label _espelho = new() { FontSize = 13 };
    private readonly Button _vinculo = new();
    private readonly Button _transmitir = new();
    private readonly IDispatcherTimer _timer;
    public AparelhoPage()
    {
        Title = "Meu aparelho";
        var root = new VerticalStackLayout { Padding = 20, Spacing = 16 };
        root.Add(new Label { Text = "Seu celular, no computador", FontSize = 26, FontAttributes = FontAttributes.Bold });
        root.Add(new Label { Text = "Bateria, carregamento, espaço livre e conexão aparecem em Dispositivos no Jarvis do PC.", FontSize = 13 });
        root.Add(_dados);
        Border Card(string icon, string name, params View[] contents)
        {
            var stack = new VerticalStackLayout { Spacing = 9 }; var heading = new HorizontalStackLayout { Spacing = 10 }; heading.Add(new Image { Source = icon, WidthRequest = 23, HeightRequest = 23 }); heading.Add(new Label { Text = name, FontSize = 12, VerticalOptions = LayoutOptions.Center }); stack.Add(heading); foreach (var view in contents) stack.Add(view);
            return new Border { Content = stack, Padding = 18, BackgroundColor = Color.FromArgb("#081B29"), Stroke = Color.FromArgb("#1B3A57"), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 5 } };
        }
        root.Add(Card("battery_icon.png", "BATERIA DO APARELHO", _bateria, _carga, _progresso));
        var info = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } }; info.Add(Card("storage_icon.png", "ESPAÇO LIVRE", _armazenamento), 0); info.Add(Card("wifi_icon.png", "CONEXÃO", _rede), 1); root.Add(info); root.Add(_estado); root.Add(_vinculo);
        _vinculo.Clicked += async (_, _) => { try { if (DispositivoCompanheiro.Ativo) { EspelhoCelular.Parar(); DispositivoCompanheiro.Parar(); } else await DispositivoCompanheiro.AtivarAsync(); Atualizar(); } catch (Exception ex) { await DisplayAlert("Conexão", ex.Message, "OK"); } };
        root.Add(new Label { Text = "Encontrar celular", FontSize = 21 });
        root.Add(new Label { Text = "No PC, toque em Encontrar celular. Este aparelho tocará por até 20 segundos. Use Parar som na notificação ou no PC. O pedido funciona com o app em segundo plano enquanto a conexão estiver ativa.", FontSize = 13 });
        root.Add(new Label { Text = "Mostrar minha tela no PC", FontSize = 21 }); root.Add(_espelho); root.Add(_transmitir);
        if (DispositivoCompanheiro.ControleSuportado)
        {
        var controle = new Label { Text = "Mouse e teclado: ative Jarvis em Acessibilidade do Android uma vez. O controle funciona somente enquanto a tela estiver sendo compartilhada e desbloqueada.", FontSize = 13 }; root.Add(controle);
        var configurar = new Button { Text = "Configurar mouse e teclado no Android" }; configurar.Clicked += (_, _) => DispositivoCompanheiro.ConfigurarControle(); root.Add(configurar);
        var modo = new Switch { IsToggled = EspelhoCelular.Compatibilidade }; modo.Toggled += (_, e) => EspelhoCelular.Compatibilidade = e.Value;
        root.Add(new Label { Text = "Modo compatível para tela preta: usa a captura do serviço de acessibilidade enquanto você compartilha a tela. Ative Jarvis em Acessibilidade. A imagem atualiza mais devagar neste modo.", FontSize = 12 }); root.Add(modo);
        }
        else root.Add(new Label { Text = "Este instalador não solicita Acessibilidade. Você pode compartilhar a tela normalmente e controlar o PC pelo celular. Mouse e teclado do PC no celular não estão incluídos.", FontSize = 13 });
        _transmitir.Clicked += async (_, _) =>
        {
            _transmitir.IsEnabled = false;
            try { if (EspelhoCelular.Ativo) EspelhoCelular.Parar(); else await EspelhoCelular.IniciarAsync(); }
            catch (Exception ex) { await DisplayAlert("Tela do celular", ex.Message, "OK"); }
            finally { _transmitir.IsEnabled = true; Atualizar(); }
        };
        root.Add(new Label { Text = "No aviso do Android, escolha Tela inteira se essa opção aparecer. Abra Dispositivos no PC para visualizar. O sistema pede autorização a cada sessão. A notificação de compartilhamento permite parar a qualquer momento.", FontSize = 12, TextColor = Color.FromArgb("#8FA9BF") });
        root.Add(new Label { Text = "Se a imagem ficar preta, teste primeiro a tela inicial do celular. Alguns apps impedem a captura do próprio conteúdo. Ao girar o aparelho, aguarde a imagem atualizar antes de clicar.", FontSize = 12, TextColor = Color.FromArgb("#8FA9BF") });
        Content = new ScrollView { Content = root };
        _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromSeconds(1); _timer.Tick += (_, _) => Atualizar();
    }
    protected override void OnAppearing() { base.OnAppearing(); DispositivoCompanheiro.Restaurar(); Atualizar(); _timer.Start(); }
    protected override void OnDisappearing() { _timer.Stop(); base.OnDisappearing(); }
    private void Atualizar()
    {
        var d = DispositivoCompanheiro.Coletar();
        _dados.Text = $"{d.Modelo}\n{d.Sistema}"; _bateria.Text = d.Bateria is { } b ? b + "%" : "—"; _progresso.Progress = (d.Bateria ?? 0) / 100d;
        _carga.Text = d.Bateria is null ? "Leitura do Android indisponível" : d.Carregando == true ? "Conectado ao carregador" : "Usando a bateria";
        _armazenamento.Text = d.ArmazenamentoLivre is { } f ? (f / 1073741824d).ToString("F1") + " GB" : "—"; _rede.Text = d.Conexao;
        _estado.Text = DispositivoCompanheiro.Estado; _vinculo.Text = DispositivoCompanheiro.Ativo ? "Desativar painel e localizar" : "Ativar painel e localizar";
        _espelho.Text = EspelhoCelular.Estado; _transmitir.Text = EspelhoCelular.Ativo ? "Parar compartilhamento" : "Autorizar e mostrar tela no PC";
    }
}
