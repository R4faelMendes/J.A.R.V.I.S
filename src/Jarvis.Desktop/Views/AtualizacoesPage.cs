using System.Windows;
using System.Windows.Controls;
using Jarvis.Core;
using Jarvis.Desktop.Services;
namespace Jarvis.Desktop.Views.Pages;

public sealed class AtualizacoesPage : PaginaBase
{
    private CancellationTokenSource? _operacao;
    public AtualizacoesPage() : base("DISTRIBUIÇÃO", "Atualizações do Jarvis") { Unloaded += (_, _) => _operacao?.Cancel(); }
    protected override void Montar(Grid container)
    {
        var servico = ServicosAplicacao.Atualizador; var stack = new StackPanel();
        stack.Children.Add(DiagnosticoPage.Label("Versão instalada: " + servico.VersaoAtual));
        stack.Children.Add(DiagnosticoPage.Label("Consulta a distribuição oficial no GitHub. O download é validado por SHA-256; a instalação e o reinício só começam quando você confirmar. Suas configurações e modelos locais são preservados."));
        var status = DiagnosticoPage.Label("Verifique quando quiser procurar uma nova versão.");
        var check = new Button { Content = "Verificar atualização", HorizontalAlignment = HorizontalAlignment.Left };
        var download = new Button { Content = "Baixar versão", IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Left };
        var instalar = new Button { Content = "Instalar e reiniciar", IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Left };
        var cancelar = new Button { Content = "Cancelar download", IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Left };
        VersaoDisponivel? versao = null; string? arquivo = null;
        check.Click += async (_, _) => {
            check.IsEnabled = false; download.IsEnabled = false; instalar.IsEnabled = false; arquivo = null; _operacao = new();
            try { versao = await servico.ConsultarAsync(_operacao.Token); status.Text = versao is null ? "Você está na versão mais recente." : "Disponível: " + versao.Versao + "\n\n" + versao.Notas; download.IsEnabled = versao is not null; }
            catch (OperationCanceledException) { } catch (Exception ex) { status.Text = "Não foi possível verificar: " + ex.Message; }
            finally { _operacao.Dispose(); _operacao = null; check.IsEnabled = true; }
        };
        download.Click += async (_, _) => {
            if (versao is null) return; _operacao = new(); check.IsEnabled = false; download.IsEnabled = false; cancelar.IsEnabled = true;
            try { arquivo = await servico.BaixarAsync(versao, new Progress<ProgressoDownload>(p => status.Text = $"{p.Etapa}: {p.Bytes / 1048576d:0.0} MB" + (p.Total is { } total ? $" / {total / 1048576d:0.0} MB" : "")), _operacao.Token); instalar.IsEnabled = true; }
            catch (OperationCanceledException) { status.Text = "Download cancelado."; } catch (Exception ex) { status.Text = ex.Message; }
            finally { _operacao.Dispose(); _operacao = null; check.IsEnabled = true; download.IsEnabled = true; cancelar.IsEnabled = false; }
        };
        cancelar.Click += (_, _) => _operacao?.Cancel();
        instalar.Click += async (_, _) => {
            if (arquivo is null || MessageBox.Show("O Jarvis será fechado para substituir o executável e abrir a nova versão. Reiniciar agora?", "Atualizar Jarvis", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            instalar.IsEnabled = false;
            try { await servico.InstalarAoSairAsync(arquivo, CancellationToken.None); Application.Current.Shutdown(); }
            catch (Exception ex) { status.Text = "Instalação não iniciada: " + ex.Message; instalar.IsEnabled = true; }
        };
        foreach (var item in new UIElement[] { check, download, cancelar, instalar, status }) stack.Children.Add(item);
        container.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }
}
