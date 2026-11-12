using System.Windows;
using System.Windows.Controls;
using Jarvis.Core;
using Jarvis.Desktop.Services;
namespace Jarvis.Desktop.Views.Pages;

public sealed class ResumoDiaPage : PaginaBase
{
    private readonly CancellationTokenSource _vida = new();
    public ResumoDiaPage() : base("HOJE", "Resumo do dia") { Unloaded += (_, _) => _vida.Cancel(); }
    protected override void Montar(Grid container)
    {
        var stack = new StackPanel();
        stack.Children.Add(DiagnosticoPage.Label("Compromissos pendentes de hoje e até cinco assuntos não lidos. Consulta sob demanda; os dados não são enviados à IA."));
        var atualizar = new Button { Content = "Gerar / atualizar resumo", HorizontalAlignment = HorizontalAlignment.Left };
        var texto = DiagnosticoPage.Label("Clique para consultar suas fontes.");
        atualizar.Click += async (_, _) => { atualizar.IsEnabled = false; try { texto.Text = await ServicosAplicacao.ResumoDia.GerarAsync(_vida.Token); } catch (OperationCanceledException) { } catch (Exception ex) { texto.Text = ex.Message; } finally { atualizar.IsEnabled = true; } };
        stack.Children.Add(atualizar); stack.Children.Add(texto);
        var ouvir = new Button { Content = "Ler resumo", HorizontalAlignment = HorizontalAlignment.Left }; ouvir.Click += (_, _) => Falar(texto.Text); stack.Children.Add(ouvir);
        container.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
}
