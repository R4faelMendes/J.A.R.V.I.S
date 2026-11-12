using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Jarvis.Core;
using Jarvis.Desktop.Services;

namespace Jarvis.Desktop.Views.Pages;

public sealed class ConversaPage : PaginaBase
{
    public Func<string, CancellationToken, Task<string>>? Executar { get; set; }
    private readonly CancellationTokenSource _vida = new();
    public ConversaPage() : base("CENTRAL DE COMANDOS", "Digite, revise e acompanhe suas ações") { Unloaded += (_, _) => _vida.Cancel(); }
    protected override void Montar(Grid container)
    {
        var root = new DockPanel();
        var topo = new StackPanel(); DockPanel.SetDock(topo, Dock.Top); root.Children.Add(topo);
        topo.Children.Add(DiagnosticoPage.Label("Comandos digitados e transcrições de voz ficam neste computador. Escolha Editar para corrigir um reconhecimento antes de enviar novamente."));
        var entrada = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 78, MaxLength = 4000, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        System.Windows.Automation.AutomationProperties.SetName(entrada, "Comando ou pergunta"); topo.Children.Add(entrada);
        var linha = new WrapPanel { Margin = new Thickness(0, 8, 0, 8) };
        var enviar = new Button { Content = "Enviar comando", Margin = new Thickness(0, 0, 10, 0) };
        var cancelar = new Button { Content = "Cancelar espera", IsEnabled = false, Margin = new Thickness(0, 0, 10, 0) };
        var ouvir = new CheckBox { Content = "Ler resposta em voz alta", VerticalAlignment = VerticalAlignment.Center };
        linha.Children.Add(enviar); linha.Children.Add(cancelar); linha.Children.Add(ouvir); topo.Children.Add(linha);
        var status = DiagnosticoPage.Label("Enter envia; Shift + Enter cria uma linha. A IA externa começa desativada."); topo.Children.Add(status);
        var sugestoes = new WrapPanel();
        foreach (var comando in new[] { "que horas são", "abra a calculadora", "resumo do dia", "minhas rotinas" }) {
            var b = new Button { Content = comando, Margin = new Thickness(0, 0, 8, 8) }; b.Click += (_, _) => { entrada.Text = comando; entrada.Focus(); }; sugestoes.Children.Add(b);
        }
        topo.Children.Add(sugestoes);
        var lista = new StackPanel(); root.Children.Add(new ScrollViewer { Content = lista, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        var historico = ServicosAplicacao.Historico; CancellationTokenSource? atual = null;
        void Atualizar() {
            lista.Children.Clear();
            var itens = historico.Listar();
            if (itens.Count == 0) lista.Children.Add(DiagnosticoPage.Label("Nenhum comando registrado ainda."));
            foreach (var item in itens) {
                var card = new StackPanel();
                var quando = DateTimeOffset.TryParse(item.Quando, out var data) ? data.LocalDateTime.ToString("dd/MM HH:mm") : item.Quando;
                card.Children.Add(DiagnosticoPage.Label(quando + " · " + item.Origem));
                card.Children.Add(DiagnosticoPage.Label(item.Comando, Theme.Gelo)); card.Children.Add(DiagnosticoPage.Label(item.Resposta));
                var acoes = new WrapPanel();
                var editar = new Button { Content = "Editar / corrigir", Margin = new Thickness(0, 0, 8, 0) };
                editar.Click += (_, _) => { entrada.Text = item.Comando; entrada.Focus(); entrada.CaretIndex = entrada.Text.Length; };
                var repetir = new Button { Content = "Repetir" }; repetir.Click += async (_, _) => { entrada.Text = item.Comando; await Enviar(); };
                acoes.Children.Add(editar); acoes.Children.Add(repetir); card.Children.Add(acoes); lista.Children.Add(CriarCardVidro(conteudo: card));
            }
        }
        async Task Enviar() {
            if (atual is not null || string.IsNullOrWhiteSpace(entrada.Text)) return;
            var texto = entrada.Text.Trim();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_vida.Token); atual = cts;
            enviar.IsEnabled = false; cancelar.IsEnabled = true; status.Text = "Processando…";
            try {
                var resposta = await (Executar?.Invoke(texto, cts.Token) ?? Task.FromResult("O executor ainda está iniciando."));
                status.Text = resposta;
                if (ouvir.IsChecked == true) Falar(resposta);
                Atualizar();
            } catch (OperationCanceledException) { status.Text = "Espera cancelada. Ações já concluídas permanecem."; }
            catch (Exception ex) { status.Text = ex.Message; }
            finally { atual = null; enviar.IsEnabled = true; cancelar.IsEnabled = false; }
        }
        enviar.Click += async (_, _) => await Enviar(); cancelar.Click += (_, _) => atual?.Cancel();
        entrada.PreviewKeyDown += async (_, e) => { if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { e.Handled = true; await Enviar(); } };
        var limpar = new Button { Content = "Apagar histórico local", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8) };
        limpar.Click += (_, _) => { if (MessageBox.Show("Apagar o histórico de comandos deste computador?", "Histórico local", MessageBoxButton.YesNo) == MessageBoxResult.Yes) historico.Limpar(); };
        topo.Children.Add(limpar);
        EventHandler mudou = (_, _) => Dispatcher.InvokeAsync(() => { if (!_vida.IsCancellationRequested) Atualizar(); });
        Loaded += (_, _) => { historico.Alterado += mudou; Atualizar(); };
        Unloaded += (_, _) => { historico.Alterado -= mudou; atual?.Cancel(); };
        container.Children.Add(root);
    }
}
