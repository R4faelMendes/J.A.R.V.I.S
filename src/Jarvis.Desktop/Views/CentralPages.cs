using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Jarvis.Core;
using Jarvis.Desktop.Services;

namespace Jarvis.Desktop.Views.Pages;

public sealed class DiagnosticoPage : PaginaBase
{
    public DiagnosticoPage() : base("CENTRAL DE SAÚDE", "Diagnóstico do Jarvis") { }
    protected override void Montar(Grid container)
    {
        var stack = new StackPanel();
        var status = Label("Confira microfone, modelos de voz, conta e servidor em um só lugar."); stack.Children.Add(status);
        var button = new Button { Content = "Verificar agora", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 20), Style = (Style)Application.Current.Resources["BotaoPrimario"] }; stack.Children.Add(button);
        var results = new StackPanel(); stack.Children.Add(results);
        button.Click += async (_, _) =>
        {
            button.IsEnabled = false; status.Text = "Verificando…"; results.Children.Clear();
            try
            {
                foreach (var item in await ServicosAplicacao.Diagnostico.VerificarAsync())
                {
                    var row = new StackPanel(); row.Children.Add(Label((item.Ok ? "PRONTO  /  " : "ATENÇÃO  /  ") + item.Nome, item.Ok ? Theme.VerdeSistema : Theme.Ouro)); row.Children.Add(Label(item.Detalhe));
                    results.Children.Add(CriarCardVidro(conteudo: row));
                }
                status.Text = "Verificação concluída. Nenhuma configuração foi alterada.";
            }
            catch (Exception ex) { status.Text = ex.Message; }
            finally { button.IsEnabled = true; }
        };
        container.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
    internal static TextBlock Label(string text, string color = Theme.Nevoa) => new() { Text = text, Foreground = (Brush)new BrushConverter().ConvertFromString(color)!, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 6), FontSize = 13 };
}

public sealed class RotinasCentralPage : PaginaBase
{
    private CancellationTokenSource? _execucao;
    public RotinasCentralPage() : base("AUTOMAÇÃO", "Seu dia, em um comando") { Unloaded += (_, _) => _execucao?.Cancel(); }
    protected override void Montar(Grid container)
    {
        container.Children.Clear(); var stack = new StackPanel();
        stack.Children.Add(DiagnosticoPage.Label("Crie sequências com aplicativos, música, volume, janelas e pausas. Cancelar interrompe as próximas etapas; ações já executadas permanecem."));
        var name = new TextBox { Text = "Modo trabalho", Margin = new Thickness(0, 8, 0, 8), MaxLength = 80 };
        stack.Children.Add(DiagnosticoPage.Label("Nome e frase de ativação")); stack.Children.Add(name);
        var actions = new List<PassoRotina>(); var list = new ListBox { Height = 100, Margin = new Thickness(0, 10, 0, 10) };
        var presets = new WrapPanel { Margin = new Thickness(0, 4, 0, 14) };
        foreach (var preset in new[] { "Modo trabalho", "Modo jogo", "Encerrar o dia" })
        {
            var button = new Button { Content = preset, Margin = new Thickness(0, 0, 8, 0) };
            button.Click += (_, _) =>
            {
                name.Text = preset; actions.Clear(); list.Items.Clear();
                actions.Add(new("midia_pausar")); actions.Add(new("volume", preset == "Encerrar o dia" ? "0" : preset == "Modo jogo" ? "60" : "20"));
                if (preset == "Modo trabalho") actions.Add(new("layout", "Metade"));
                foreach (var step in actions) list.Items.Add(PlanoRotina.Descrever(step));
            }; presets.Children.Add(button);
        }
        stack.Children.Add(presets);
        var kind = new ComboBox { ItemsSource = new[] { "Abrir aplicativo", "Ajustar volume", "Pausar música", "Continuar música", "Organizar janelas", "Esperar" }, SelectedIndex = 0, MinWidth = 170 };
        var arg = new ComboBox { MinWidth = 210, Margin = new Thickness(10, 0, 10, 0), DisplayMemberPath = "Nome", SelectedValuePath = "Id" };
        var apps = ServicosAplicacao.Apps.ListarApps().Where(a => !a.EhJogo).ToArray();
        void Options()
        {
            arg.ItemsSource = kind.SelectedIndex switch
            {
                0 => apps.Select(a => new Option(a.Id, a.Nome)).ToArray(),
                1 => new[] { new Option("20", "20%"), new("40", "40%"), new("60", "60%"), new("80", "80%") },
                4 => new[] { new Option("Metade", "Duas colunas"), new("TresColunas", "Três colunas"), new("FocoLateral", "Foco lateral"), new("Grade2x2", "Grade 2 × 2") },
                5 => new[] { new Option("2", "2 segundos"), new("5", "5 segundos"), new("10", "10 segundos") },
                _ => Array.Empty<Option>()
            }; arg.SelectedIndex = 0;
        }
        kind.SelectionChanged += (_, _) => Options(); Options();
        var add = new Button { Content = "Adicionar etapa" }; var controls = new WrapPanel(); controls.Children.Add(kind); controls.Children.Add(arg); controls.Children.Add(add); stack.Children.Add(controls); stack.Children.Add(list);
        var status = DiagnosticoPage.Label("Selecione as etapas na ordem desejada.");
        add.Click += (_, _) =>
        {
            if (actions.Count >= 20) { status.Text = "Limite de 20 etapas."; return; }
            var cmd = new[] { "abrir_app", "volume", "midia_pausar", "midia_continuar", "layout", "esperar" }[kind.SelectedIndex];
            if (kind.SelectedIndex is 0 or 1 or 4 or 5 && arg.SelectedValue is null) return;
            actions.Add(new(cmd, arg.SelectedValue as string)); list.Items.Add($"{actions.Count:00}  {kind.SelectedItem}  {(arg.SelectedItem as Option)?.Nome}");
        };
        var removeStep = new Button { Content = "Remover etapa selecionada", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
        removeStep.Click += (_, _) => { var i = list.SelectedIndex; if (i >= 0) { actions.RemoveAt(i); list.Items.RemoveAt(i); } }; stack.Children.Add(removeStep);
        var save = new Button { Content = "Salvar rotina", HorizontalAlignment = HorizontalAlignment.Left, Style = (Style)Application.Current.Resources["BotaoPrimario"] };
        save.Click += (_, _) =>
        {
            try
            {
                var json = JsonSerializer.Serialize(actions); PlanoRotina.Validar(json);
                if (string.IsNullOrWhiteSpace(name.Text)) throw new ArgumentException("Dê um nome à rotina.");
                ServicosAplicacao.CatalogoRotinas.Adicionar(name.Text.Trim(), name.Text.Trim(), json); Montar(container);
            }
            catch (Exception ex) { status.Text = ex.Message; }
        }; stack.Children.Add(save); stack.Children.Add(status);
        foreach (var r in ServicosAplicacao.CatalogoRotinas.Listar())
        {
            var body = new StackPanel(); body.Children.Add(DiagnosticoPage.Label(r.Nome, Theme.Gelo));
            var toggle = new CheckBox { Content = "Ativa para voz", IsChecked = r.Ativa, Foreground = Brushes.White, Margin = new Thickness(0, 6, 0, 10) };
            toggle.Click += (_, _) => ServicosAplicacao.CatalogoRotinas.Alternar(r.Id, toggle.IsChecked == true); body.Children.Add(toggle);
            var row = new WrapPanel(); var run = new Button { Content = "Executar etapas", Margin = new Thickness(0, 0, 8, 0) }; var cancel = new Button { Content = "Cancelar", IsEnabled = false };
            var progress = DiagnosticoPage.Label(Descricao(r.Acoes));
            run.Click += async (_, _) =>
            {
                if (_execucao is not null) { status.Text = "Já existe uma rotina em execução."; return; }
                var cts = new CancellationTokenSource(); _execucao = cts; run.IsEnabled = false; cancel.IsEnabled = true;
                try { await ServicosAplicacao.Rotinas.ExecutarAsync(r.Acoes, new Progress<ProgressoRotina>(p => progress.Text = $"{p.Atual}/{p.Total}  {p.Mensagem}"), cts.Token); }
                catch (OperationCanceledException) { progress.Text = "Cancelada. Próximas etapas não serão executadas."; }
                catch (Exception ex) { progress.Text = "Interrompida: " + ex.Message; }
                finally { _execucao = null; cts.Dispose(); run.IsEnabled = true; cancel.IsEnabled = false; }
            };
            cancel.Click += (_, _) => _execucao?.Cancel(); row.Children.Add(run); row.Children.Add(cancel); body.Children.Add(row); body.Children.Add(progress); stack.Children.Add(CriarCardVidro(conteudo: body));
        }
        container.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
    private sealed record Option(string Id, string Nome) { public override string ToString() => Nome; }
    private static string Descricao(string json) { try { var p = PlanoRotina.Validar(json); return $"{p.Length} etapas executáveis"; } catch { return "Rotina antiga: crie uma nova sequência para executar ações reais."; } }
}
