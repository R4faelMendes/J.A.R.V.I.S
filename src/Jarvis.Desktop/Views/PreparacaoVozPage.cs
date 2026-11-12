using System.Windows;
using System.Windows.Controls;
using Jarvis.Core;
using Jarvis.Desktop.Services;

namespace Jarvis.Desktop.Views.Pages;

public sealed class PreparacaoVozPage : PaginaBase
{
    private readonly CancellationTokenSource _vida = new();
    private CancellationTokenSource? _operacao;
    public PreparacaoVozPage() : base("PRIMEIROS PASSOS", "Prepare e teste sua voz local")
    {
        Unloaded += (_, _) => { _operacao?.Cancel(); _vida.Cancel(); };
    }
    protected override void Montar(Grid container)
    {
        var servico = ServicosAplicacao.PreparacaoVoz;
        var stack = new StackPanel();
        stack.Children.Add(DiagnosticoPage.Label("1. Escolha o microfone em Configurações.  2. Instale os recursos.  3. Teste a voz e a transcrição. Downloads oficiais de aproximadamente 200 MB; os modelos funcionam localmente depois da instalação."));
        var config = new Button { Content = "Escolher microfone", HorizontalAlignment = HorizontalAlignment.Left };
        config.Click += (_, _) => Navegar(PageType.Configuracoes); stack.Children.Add(config);
        var lista = new StackPanel { Margin = new Thickness(0, 16, 0, 12) }; stack.Children.Add(lista);
        void Atualizar() {
            lista.Children.Clear();
            foreach (var recurso in servico.Verificar()) lista.Children.Add(CriarCardVidro((recurso.Pronto ? "Pronto · " : "Pendente · ") + recurso.Nome, DiagnosticoPage.Label(recurso.Detalhe)));
        }
        Atualizar();
        stack.Children.Add(DiagnosticoPage.Label("Pasta de instalação: " + servico.Pasta));
        var status = DiagnosticoPage.Label("A instalação começa somente quando você clicar em Instalar / reparar.");
        var barra = new ProgressBar { Height = 6, Margin = new Thickness(0, 10, 0, 12) };
        var instalar = new Button { Content = "Instalar / reparar recursos", Margin = new Thickness(0, 0, 8, 0) };
        var testar = new Button { Content = "Testar e ouvir voz", Margin = new Thickness(0, 0, 8, 0) };
        var mic = new Button { Content = "Gravar 5 segundos para testar microfone", Margin = new Thickness(0, 0, 8, 0) };
        var cancelar = new Button { Content = "Cancelar", IsEnabled = false };
        var botoes = new WrapPanel(); foreach (var b in new[] { instalar, testar, mic, cancelar }) botoes.Children.Add(b);
        async Task Executar(Func<CancellationToken, Task<string>> acao) {
            if (_operacao is not null) return;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_vida.Token); _operacao = cts;
            instalar.IsEnabled = testar.IsEnabled = mic.IsEnabled = false; cancelar.IsEnabled = true; barra.IsIndeterminate = true;
            try { status.Text = await acao(cts.Token); }
            catch (OperationCanceledException) { status.Text = "Cancelado. Os recursos concluídos foram preservados."; }
            catch (Exception ex) { status.Text = "Não foi possível concluir: " + ex.Message; }
            finally { _operacao = null; instalar.IsEnabled = testar.IsEnabled = mic.IsEnabled = true; cancelar.IsEnabled = false; barra.IsIndeterminate = false; Atualizar(); }
        }
        instalar.Click += async (_, _) => await Executar(async ct => {
            await servico.InstalarAsync(new Progress<ProgressoDownload>(p => status.Text = p.Etapa + (p.Bytes > 0 ? $" · {p.Bytes / 1048576d:0.0} MB" : "")), ct);
            return "Recursos validados. Teste a voz; se a escuta já estava aberta, reabra o Jarvis depois de instalar.";
        });
        testar.Click += async (_, _) => await Executar(servico.TestarAsync);
        mic.Click += async (_, _) => { status.Text = "Fale agora durante 5 segundos. O áudio permanece local e nenhum comando será executado."; await Executar(servico.TestarMicrofoneAsync); };
        cancelar.Click += (_, _) => _operacao?.Cancel();
        stack.Children.Add(status); stack.Children.Add(barra); stack.Children.Add(botoes);
        container.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
}
