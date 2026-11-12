using System.Windows;
using System.Windows.Controls;
using Jarvis.Core;
using Jarvis.Desktop.Services;
namespace Jarvis.Desktop.Views.Pages;

public sealed class IaConfiguracaoPage : PaginaBase
{
    public IaConfiguracaoPage() : base("CONVERSA OPCIONAL", "IA para perguntas gerais") { }
    protected override void Montar(Grid container)
    {
        var servico = ServicosAplicacao.ConversaIa; var p = servico.Preferencias;
        var stack = new StackPanel();
        stack.Children.Add(DiagnosticoPage.Label("Comandos do computador continuam locais e validados. Ao ativar, perguntas não reconhecidas podem ser enviadas ao provedor configurado. Agenda, e-mails e histórico não são enviados automaticamente. Serviços podem cobrar conforme sua conta. Limite local: 20 tentativas/dia, incluindo fallback."));
        var ativa = new CheckBox { Content = "Permitir envio das minhas perguntas aos provedores configurados", IsChecked = p.Ativa, Margin = new Thickness(0, 8, 0, 16) }; stack.Children.Add(ativa);
        var modelos = new List<TextBox>();
        foreach (var (nome, valor) in new[] { ("Modelo Gemini", p.Gemini), ("Modelo Groq", p.Groq), ("Modelo OpenRouter", p.OpenRouter) }) {
            stack.Children.Add(DiagnosticoPage.Label(nome)); var box = new TextBox { Text = valor, MaxLength = 160, Margin = new Thickness(0, 0, 0, 10) }; modelos.Add(box); stack.Children.Add(box);
        }
        stack.Children.Add(DiagnosticoPage.Label("Provedor da chave a salvar / remover"));
        var provedor = new ComboBox { ItemsSource = new[] { "groq", "gemini", "openrouter" }, SelectedIndex = 0 }; stack.Children.Add(provedor);
        var senha = new PasswordBox { Margin = new Thickness(0, 8, 0, 8) }; stack.Children.Add(senha);
        stack.Children.Add(DiagnosticoPage.Label("Chave vazia preserva a chave atual. As chaves ficam protegidas por DPAPI neste Windows."));
        var remover = new CheckBox { Content = "Remover a chave do provedor selecionado" }; stack.Children.Add(remover);
        var status = DiagnosticoPage.Label(Estado());
        string Estado() => "Configurados: " + (servico.ProvedoresConfigurados.Count == 0 ? "nenhum" : string.Join(", ", servico.ProvedoresConfigurados)) + $" · tentativas hoje: {servico.UsadasHoje}/20";
        var salvar = new Button { Content = "Salvar preferências e chave", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 14, 0, 10) };
        salvar.Click += (_, _) => {
            try { servico.Configurar(new(ativa.IsChecked == true, modelos[0].Text.Trim(), modelos[1].Text.Trim(), modelos[2].Text.Trim()), (string)provedor.SelectedItem, senha.Password, remover.IsChecked == true); senha.Clear(); remover.IsChecked = false; status.Text = "Salvo. " + Estado(); }
            catch (Exception ex) { status.Text = ex.Message; }
        };
        stack.Children.Add(salvar); stack.Children.Add(status);
        var abrir = new Button { Content = "Abrir central de comandos", HorizontalAlignment = HorizontalAlignment.Left }; abrir.Click += (_, _) => Navegar(PageType.Conversa); stack.Children.Add(abrir);
        container.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
}
