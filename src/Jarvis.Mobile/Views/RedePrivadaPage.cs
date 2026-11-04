namespace Jarvis.Mobile.Views;
public sealed class RedePrivadaPage : ContentPage
{
    private readonly Label _status = new() { Text = "Verificando automaticamente…", FontSize = 13 };
    private readonly Button _check = new() { Text = "Verificar e reconectar automaticamente" };
    public RedePrivadaPage()
    {
        Title = "Conexão automática";
        var root = new VerticalStackLayout { Padding = 20, Spacing = 16 };
        root.Add(new Label { Text = "Jarvis encontra seu PC", FontSize = 25 }); root.Add(_status);
        _check.Clicked += async (_, _) => await VerificarAsync(); root.Add(_check);
        root.Add(new Label { Text = "O app usa o Wi-Fi ou se conecta diretamente ao endereço HTTPS do seu PC, com certificado pareado e token. Nenhum app de VPN é necessário.", FontSize = 13 });
        root.Add(new Label { Text = "Conecte no Wi-Fi uma vez para registrar o endereço público detectado pelo PC. Fora de casa, ambos precisam alcançar esse endereço. IPv6 requer IPv6 no celular e porta 5189 liberada no roteador; IPv4 exige IP público e redirecionamento da porta. CGNAT pode impedir o acesso direto.", FontSize = 12 });
        var manual = new VerticalStackLayout { IsVisible = false, Spacing = 10 }; var address = new Entry { Placeholder = "https://[IPv6-do-PC]:5189 ou domínio", Text = App.Conexao.EnderecoRemoto, Keyboard = Keyboard.Url }; manual.Add(address); var save = new Button { Text = "Verificar endereço direto" }; save.Clicked += async (_, _) => { try { await App.Conexao.ConectarEnderecoAsync(address.Text ?? ""); await VerificarAsync(); } catch (Exception ex) { _status.Text = ex.Message; } }; manual.Add(save);
        var advanced = new Button { Text = "Endereço manual (opcional)" }; advanced.Clicked += (_, _) => manual.IsVisible = !manual.IsVisible; root.Add(advanced); root.Add(manual);
        Content = new ScrollView { Content = root };
    }
    protected override async void OnAppearing() { base.OnAppearing(); await VerificarAsync(); }
    private async Task VerificarAsync()
    {
        _check.IsEnabled = false; _status.Text = "Procurando o PC pela rede local e pela internet direta…";
        try { await App.Conexao.CarregarSeNecessarioAsync(); var checks = await App.Conexao.VerificarAutomaticamenteAsync(); App.Conexao.IniciarMonitor(); _status.Text = App.Conexao.UltimoDiagnostico + "\n\n" + string.Join("\n", checks.Select(c => (c.Conectado ? "✓ " : "• ") + c.Endereco + " — " + c.Mensagem)); }
        catch (Exception ex) { _status.Text = ex.Message; }
        finally { _check.IsEnabled = true; }
    }
}
