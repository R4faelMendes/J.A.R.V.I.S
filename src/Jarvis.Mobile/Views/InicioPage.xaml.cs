namespace Jarvis.Mobile.Views;

public partial class InicioPage : ContentPage
{
    public InicioPage()
    {
        InitializeComponent();
        var h = DateTime.Now.Hour;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing(); Reator.Animar(true); Player.Ativar(true); App.Conexao.EstadoMudou += Estado; App.Conexao.IniciarMonitor();
        Estado(this, App.Conexao.Conectado ? "Online" : "Offline");
    }
    protected override void OnDisappearing() { Reator.Animar(false); Player.Ativar(false); App.Conexao.EstadoMudou -= Estado; base.OnDisappearing(); }
    private void Estado(object? sender, string text) => MainThread.BeginInvokeOnMainThread(() => { Status.Text = text; Status.TextColor = Color.FromArgb(App.Conexao.Conectado ? "#4DE3A1" : "#F4B544"); PcLinha.Text = App.Conexao.Identidade?.Nome ?? "Pareie seu computador"; });

    private async void Bloquear(object? s, EventArgs e)
    {
        try { if (!await DisplayAlert("Bloquear PC", "Bloquear a sessão do computador agora?", "Bloquear", "Cancelar")) return; var resposta = await App.Conexao.ComandoAsync("bloquear", confirmado: true); await DisplayAlert("Jarvis", resposta, "OK"); }
        catch (Exception ex) { await DisplayAlert("Jarvis", ex.Message, "OK"); }
    }

    private async void LigarPc(object? s, EventArgs e)
    {
        try { await App.Conexao.AcordarAsync(); App.Conexao.IniciarMonitor(); await DisplayAlert("Wake-on-LAN", "Pacote enviado. A conexão será restabelecida quando o PC e o Jarvis iniciarem. A placa de rede e a BIOS precisam permitir Wake-on-LAN.", "OK"); }
        catch (Exception ex) { await DisplayAlert("Wake-on-LAN", ex.Message, "OK"); }
    }

    private async void Falar(object? s, EventArgs e) => await Shell.Current.GoToAsync("falar");
    private async void Controles(object? s, EventArgs e) => await Shell.Current.GoToAsync("janelas");
    private async void Midia(object? s, EventArgs e) => await Shell.Current.GoToAsync("//controles");
    private async void Apps(object? s, EventArgs e) => await Shell.Current.GoToAsync("//apps");
    private async void Desempenho(object? s, EventArgs e) => await Shell.Current.GoToAsync("desempenho");
    private async void TelaAoVivo(object? s, EventArgs e) => await Shell.Current.GoToAsync("tela");
    private async void Mais(object? s, EventArgs e) => await Shell.Current.GoToAsync("//mais");
    private async void Parear(object? s, EventArgs e) => await Shell.Current.GoToAsync("parear");
}
