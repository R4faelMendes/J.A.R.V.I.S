using Jarvis.Mobile.Services;
namespace Jarvis.Mobile.Views;
public partial class DiagnosticoPage : ContentPage
{
    private async void CompartilharDiagnostico(object? s, EventArgs e) { try { await Services.DiagnosticoMobile.CompartilharAsync(); } catch (Exception ex) { Resultado.Text = ex.Message; } }
    public DiagnosticoPage() => InitializeComponent();
    protected override async void OnAppearing() { base.OnAppearing(); await ConferirAsync(); }
    private async void Verificar(object? s, EventArgs e) => await ConferirAsync();
    private async Task ConferirAsync()
    {
        Resultado.Text = "Verificando endereços diretos salvos e autenticação com o PC…";
        try
        {
            await App.Conexao.CarregarSeNecessarioAsync(); var checks = await App.Conexao.VerificarAutomaticamenteAsync();
            var lines = new List<string> { App.Conexao.UltimoDiagnostico };
            lines.AddRange(checks.Select(c => (c.Conectado ? "✓ " : "• ") + c.Endereco + "\n" + c.Mensagem));
            lines.Add(App.Conexao.Conectado ? "Certificado pareado e token aceitos pelo PC." : "O pareamento foi preservado. Se o token foi recusado, leia um novo QR no PC.");
            lines.Add("A verificação usa HTTPS autenticado na porta 5189. A porta HTTP de download não é usada para decidir se o controle está conectado."); Resultado.Text = string.Join("\n\n", lines); App.Conexao.IniciarMonitor();
        }
        catch (Exception ex) { Resultado.Text = ex.Message; }
    }
    private async void Redescobrir(object? s, EventArgs e) => await ConferirAsync();
}
